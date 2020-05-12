using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Diagnostics;

using RUDP.Interfaces;
using RUDP.Enumerations;

namespace RUDP
{
	/// <summary>
	/// Reliable User Datagram Protocol listener/server implementation.
	/// </summary>
	public class RudpListener
	{
		/// <summary>
		/// Application Identifier.
		/// </summary>
		public ushort AppId { get; private set; }
		/// <summary>
		/// Indicates if the listener is active.
		/// </summary>
		public bool Active { get; private set; }
		/// <summary>
		/// Number of packets sent per second.
		/// </summary>
		public int SendRate { get; private set; }
		/// <summary>
		/// Gets the local endpoint.
		/// </summary>
		public EndPoint LocalEndpoint { get; private set; }

		/// <summary>
		/// Contains the bound socket.
		/// </summary>
		private IRudpSocket _socket;
		/// <summary>
		/// Communication handling thread.
		/// </summary>
		private Thread _listenerThread;
		/// <summary>
		/// Handles timing between sent packets.
		/// </summary>
		private Stopwatch _sendStopwatch;
		/// <summary>
		/// Contains all connected remote host through this listener.
		/// </summary>
		private Dictionary<IPEndPoint, RudpClient> _connectedClients = new Dictionary<IPEndPoint, RudpClient>();
		/// <summary>
		/// Remote host connection request list.
		/// </summary>
		private Queue<IPEndPoint> _pendingRequests;
		/// <summary>
		/// List of accepted remote host connections to be replied.
		/// </summary>
		private List<(IPEndPoint, Packet)> _acceptReplyList = new List<(IPEndPoint, Packet)>();
		
		/// <summary>
		/// Creates a new RudpListener instance.
		/// </summary>
		/// <param name="appId"></param>
		/// <param name="port">socket port.</param>
		/// <param name="sendRate">packet send rate per second.</param>
		public RudpListener(ushort appId, int port, int sendRate) : this(appId, new IPEndPoint(IPAddress.Any, port), sendRate) { }

		/// <summary>
		/// Creates a new RudpListener instance.
		/// </summary>
		/// <param name="appId"></param>
		/// <param name="address">IP address to bind to.</param>
		/// <param name="port">socket port.</param>
		/// <param name="sendRate">packet send rate per second.</param>
		public RudpListener(ushort appId, IPAddress address, int port, int sendRate) : this(appId, new IPEndPoint(address, port), sendRate) { }

		/// <summary>
		/// Creates a new RudpListener instance.
		/// </summary>
		/// <param name="appId"></param>
		/// <param name="endPoint">endpoint to bind to.</param>
		/// <param name="sendRate">packet send rate per second.</param>
		public RudpListener(ushort appId, IPEndPoint endPoint, int sendRate)
		{
			AppId = appId;
			LocalEndpoint = endPoint;
			SendRate = sendRate;
		}

		/// <summary>
		/// Accepts the first connection request from the pending connection requests.
		/// </summary>
		/// <returns>New instance of RudpClient representing the remote host.</returns>
		public RudpClient AcceptClient()
		{
			IPEndPoint endPoint = null;
			lock (_pendingRequests)
				if (_pendingRequests.Count > 0)
					endPoint = _pendingRequests.Dequeue();
				else
					return null;

			byte updateRate = (byte)SendRate;
			byte version = 0;

			Packet acceptPacket = new Packet()
			{
				AppId = AppId,
				SequenceNumber = 0,
				AckSequenceNumber = 0,
				AckBitfield = new Bitfield(4),
				Type = PacketType.ConnectionAccept,
				Data = new byte[] { updateRate, version }
			};

			RudpClient client = new RudpClient(this, endPoint, 1);
			_connectedClients.Add(endPoint, client);
			_acceptReplyList.Add((endPoint, acceptPacket));

			return client;
		}

		public void AllowNatTraversal(bool allowed)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Indicates if there are connection requests pending.
		/// </summary>
		/// <returns>true if there is at leat one connection request pending.</returns>
		public bool Pending()
		{
			if (!Active)
				throw new InvalidOperationException();

			lock(_pendingRequests)
			{
				return _pendingRequests.Count > 0;
			}			
		}

		/// <summary>
		/// Start listening for connection requests.
		/// </summary>
		public void Start()
		{
			Start(0);
		}

		/// <summary>
		/// Start listening for connection requests.
		/// </summary>
		/// <param name="backlog">limit of pending connection requests.</param>
		public void Start(int backlog)
		{
			if (backlog != 0)
				_pendingRequests = new Queue<IPEndPoint>(backlog);
			else
				_pendingRequests = new Queue<IPEndPoint>();

			Active = true;

			_listenerThread = new Thread(new ThreadStart(ListenerThread));
			_listenerThread.Start();
		}

		/// <summary>
		/// Stops listener and drops connected clients.
		/// </summary>
		public void Stop()
		{
			Active = false;
			lock (_sendStopwatch)
				_sendStopwatch.Stop();

			lock(_pendingRequests)
			{
				_pendingRequests.Clear();
				_pendingRequests = null;
			}
		}

		/// <summary>
		/// Implements communication logic in for server mode and listener. 
		/// </summary>
		private void ListenerThread()
		{
			_socket = new RudpInternalSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_socket.Bind(LocalEndpoint);

			_sendStopwatch = new Stopwatch();
			_sendStopwatch.Start();

			byte[] receiveBuffer = new byte[4096];

			while(Active)
			{
				if(_socket.Available > 0)
				{
					EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
					int receiveCount = _socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEndpoint);

					Packet packet = new Packet(receiveBuffer, 0, receiveCount);
					if(!_connectedClients.ContainsKey((IPEndPoint)remoteEndpoint))
					{
						switch(packet.Type)
						{
							case PacketType.ConnectionRequest:
								{
									if (packet.Validate(AppId, ushort.MaxValue) && 
										!_pendingRequests.Contains((IPEndPoint)remoteEndpoint))
										lock (_pendingRequests)
											_pendingRequests.Enqueue((IPEndPoint)remoteEndpoint);
									else
										_socket.SendTo(
											new Packet()
											{
												AppId = AppId,
												SequenceNumber = 0,
												AckSequenceNumber = 0,
												AckBitfield = new Bitfield(4),
												Type = PacketType.ConnectionRefuse
											}.ToBytes(),
											SocketFlags.None,
											remoteEndpoint
											);
								}
								break;
							case PacketType.DisconnectionNotify:
								break;
							default:
								break;
						}
					}
					else
						_connectedClients[(IPEndPoint)remoteEndpoint].ReceiveUpdate(packet);
					
				}

				lock (_sendStopwatch)
					if (_sendStopwatch.ElapsedMilliseconds >= 1000/SendRate)
					{
						_sendStopwatch.Restart();

						foreach ((IPEndPoint endpoint, Packet packet) reply in _acceptReplyList)
							_socket.SendTo(reply.packet.ToBytes(), reply.endpoint);

						List<IPEndPoint> disconnectedClients = new List<IPEndPoint>();
						foreach (var pair in _connectedClients)
						{
							(Packet packet, RudpEvent rudpEvent) = pair.Value.SendUpdate();

							if (!pair.Value.Connected)
								disconnectedClients.Add(pair.Key);

							_socket.SendTo(packet.ToBytes(), pair.Key);
						}
						foreach (IPEndPoint endPoint in disconnectedClients)
							_connectedClients.Remove(endPoint);

						_acceptReplyList.Clear();
					}

				Thread.Yield();
			}
		}
	}
}

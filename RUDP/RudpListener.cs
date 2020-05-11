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
	public class RudpListener
	{
		public ushort AppId { get; private set; }
		public bool Active { get; private set; }
		public EndPoint LocalEndpoint { get; private set; }
		public IRudpSocket Server { get; private set; }
		public int SendRate { get; private set; }

		private Thread _listenerThread;
		private Stopwatch _sendStopwatch;
		private Dictionary<IPEndPoint, RudpClient> _connectedClients = new Dictionary<IPEndPoint, RudpClient>();
		private Queue<IPEndPoint> _pendingRequests;
		private List<(IPEndPoint, Packet)> _acceptReplyList = new List<(IPEndPoint, Packet)>();
		
		public RudpListener(ushort appId, int port, int sendRate) : this(appId, new IPEndPoint(IPAddress.Any, port), sendRate) { }
		
		public RudpListener(ushort appId, IPAddress address, int port, int sendRate) : this(appId, new IPEndPoint(address, port), sendRate) { }

		public RudpListener(ushort appId, IPEndPoint endPoint, int sendRate)
		{
			AppId = appId;
			LocalEndpoint = endPoint;
			SendRate = sendRate;
		}

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
			};

			using(MemoryStream memoryStream = new MemoryStream())
			{
				memoryStream.WriteByte(updateRate);
				memoryStream.WriteByte(version);
				acceptPacket.Data = memoryStream.ToArray();
			}

			RudpClient client = new RudpClient(this, endPoint, 1);
			_connectedClients.Add(endPoint, client);
			_acceptReplyList.Add((endPoint, acceptPacket));

			return client;
		}

		public void AllowNatTraversal(bool allowed)
		{
			throw new NotImplementedException();
		}

		public bool Pending()
		{
			if (!Active)
				throw new InvalidOperationException();

			lock(_pendingRequests)
			{
				return _pendingRequests.Count > 0;
			}			
		}

		public void Start()
		{
			Start(0);
		}

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

		private void ListenerThread()
		{
			Server = new RudpInternalSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Server.Bind(LocalEndpoint);

			_sendStopwatch = new Stopwatch();
			_sendStopwatch.Start();

			byte[] receiveBuffer = new byte[4096];

			while(Active)
			{
				if(Server.Available > 0)
				{
					EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
					int receiveCount = Server.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEndpoint);

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
										Server.SendTo(
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
							Server.SendTo(reply.packet.ToBytes(), reply.endpoint);

						List<IPEndPoint> disconnectedClients = new List<IPEndPoint>();
						foreach (var pair in _connectedClients)
						{
							(Packet packet, RudpEvent rudpEvent) = pair.Value.SendUpdate();

							if (!pair.Value.Connected)
								disconnectedClients.Add(pair.Key);

							Server.SendTo(packet.ToBytes(), pair.Key);
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

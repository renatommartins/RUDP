namespace RUDP
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Threading;
	using RUDP.Enumerations;
	using RUDP.Interfaces;
	using RUDP.Utils;

	/// <summary>
	/// Reliable User Datagram Protocol listener/server implementation.
	/// </summary>
	public class RudpListener : RudpBase
	{
		/// <summary>
		/// Contains all connected remote host through this listener.
		/// </summary>
		private readonly List<RudpClient> _connectedClients = new List<RudpClient>();

		/// <summary>
		/// Remote host connection request list.
		/// </summary>
		private readonly Queue<IPEndPoint> _pendingRequests;

		/// <summary>
		/// List of accepted remote host connections to be replied.
		/// </summary>
		private readonly List<(IPEndPoint endPoint, Packet packet)> _acceptReplyList = new List<(IPEndPoint, Packet)>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RudpListener"/> class.
		/// </summary>
		/// <param name="appId">Application identifier to help avoid mismatched and/or unwanted data.</param>
		/// <param name="endPoint">Endpoint to bind to.</param>
		/// <param name="sendRate">Packet send rate per second.</param>
		/// <param name="socket">Socket instance to inject.</param>
		public RudpListener(ushort appId, EndPoint endPoint, int sendRate, ISocket socket = null)
			: base(appId, null, endPoint, UpdateMode.Thread)
		{
			_pendingRequests = new Queue<IPEndPoint>();
			AppId = appId;
			LocalEndpoint = endPoint;
			SendRate = sendRate;
			Socket = socket;
		}

		/// <summary>
		/// Gets a value indicating whether it is active.
		/// </summary>
		public new bool IsActive => base.IsActive;

		/// <summary>
		/// Accepts the first connection request from the pending connection requests.
		/// </summary>
		/// <returns>New instance of RudpClient representing the remote host.</returns>
		public RudpClient AcceptClient()
		{
			// Checks if there is a connections request pending.
			IPEndPoint endPoint;
			lock (_pendingRequests)
			{
				if (_pendingRequests?.Count <= 0)
				{
					return null;
				}

				endPoint = _pendingRequests.Dequeue();
			}

			// Creates connection accept packet.
			var acceptPacket = new Packet()
			{
				AppId = AppId,
				SequenceNumber = 0,
				AckSequenceNumber = 0,
				AckBitfield = new Bitfield(4),
				Type = PacketType.ConnectionAccept,
				Data = new ArraySegment<byte>(new[] { (byte)SendRate, RudpConstants.Version }),
			};

			// Create new RudpClient instance to represent remote host.
			var client = new RudpClient(this, endPoint, 1);
			_connectedClients.Add(client);

			// Enqueue connection accept reply to send on next send window.
			_acceptReplyList.Add((endPoint, acceptPacket));

			return client;
		}

		/// <summary>
		/// Indicates if there are connection requests pending.
		/// </summary>
		/// <returns>true if there is at least one connection request pending.</returns>
		public bool Pending()
		{
			if (!IsActive)
			{
				throw new InvalidOperationException();
			}

			lock (_pendingRequests)
			{
				return _pendingRequests.Count > 0;
			}
		}

		/// <summary>
		/// Start listening for connection requests.
		/// </summary>
		/// <param name="backlog">limit of pending connection requests.</param>
		public void Start(int backlog = 0)
		{
			base.IsActive = true;

			UpdateThread = new Thread(ListenerThread);
			UpdateThread.Start();
		}

		/// <summary>
		/// Stops listener and drops connected clients.
		/// </summary>
		public void Stop()
		{
			base.IsActive = false;
			lock (SendStopwatch)
			{
				SendStopwatch.Stop();
			}

			lock (_pendingRequests)
			{
				_pendingRequests.Clear();
			}
		}

		/// <summary>
		/// Implements communication logic in for server mode and listener.
		/// </summary>
		private void ListenerThread()
		{
			// Instantiates RudpInternalSocket if null and binds the actual socket
			if (Socket == null)
			{
				Socket = new RudpInternalSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			}

			Socket.Bind(LocalEndpoint);

			SendStopwatch = new Stopwatch();
			SendStopwatch.Start();

			var receiveBuffer = new byte[4096];

			// Connection handling main loop for server mode and listener.
			while (IsActive)
			{
				// Executes receive update
				if (Socket.Available > 0)
				{
					EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
					var receiveCount = Socket.Receive(receiveBuffer, 0, receiveBuffer.Length, ref remoteEndpoint);

					var packet = Packet.FromBytes(receiveBuffer, 0, receiveCount);

					// Checks if the packet comes from an already connected remote host.
					if (!_connectedClients.Any(c => Equals(c.RemoteEndpoint, (IPEndPoint)remoteEndpoint)))
					{
						switch (packet.Type)
						{
							case PacketType.ConnectionRequest:
								{
									// Enqueues the request to be accepted later.
									lock (_pendingRequests)
									{
										if (ValidatePacket(packet, AppId, ushort.MaxValue) &&
											!_pendingRequests.Contains((IPEndPoint)remoteEndpoint))
										{
											_pendingRequests.Enqueue((IPEndPoint)remoteEndpoint);
										}

										// Refuses the request if the request is invalid.
										else
										{
											Socket.Send(
												Packet.ToBytes(new Packet()
												{
													AppId = AppId,
													SequenceNumber = 0,
													AckSequenceNumber = 0,
													AckBitfield = new Bitfield(4),
													Type = PacketType.ConnectionRefuse,
												}),
												remoteEndpoint);
										}
									}
								}

								break;
							case PacketType.DisconnectionNotify:
							case PacketType.ConnectionAccept:
							case PacketType.ConnectionRefuse:
							case PacketType.KeepAlive:
							case PacketType.Data:
							case PacketType.Invalid:
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					else
					{
						// Forwards the packet to the RudpClient instance that represents the remote host.
						_connectedClients
							.First(c => c.RemoteEndpoint.Equals(remoteEndpoint))
							.ReceiveUpdate(packet);
					}
				}

				lock (SendStopwatch)
				{
					// Executes send update based on the send rate.
					if (SendStopwatch.ElapsedMilliseconds >= 1000 / SendRate)
					{
						SendStopwatch.Restart();

						// Sends all enqueued connection accept replies.
						_acceptReplyList
							.ForEach(t => Socket.Send(Packet.ToBytes(t.packet), t.endPoint));
						_acceptReplyList.Clear();

						// Gets and sends the packet for this window of each client.
						_connectedClients
							.ForEach(c =>
							{
								var packet = c.SendUpdate();
								Socket.Send(Packet.ToBytes(packet), c.RemoteEndpoint);
							});

						// Clears disconnected clients.
						_connectedClients
							.Where(c => c.State != ClientState.Connected)
							.ToList()
							.ForEach(c => _connectedClients.Remove(c));
					}
				}

				Thread.Yield();
			}

			// Disconnects all remote host when closing the listener.
			_connectedClients
				.ForEach(c => Socket.Send(Packet.ToBytes(c.GetDisconnectPacket()), c.RemoteEndpoint));

			Socket.Close();
		}
	}
}

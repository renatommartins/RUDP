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
	/// Reliable User Datagram Protocol client implementation.
	/// </summary>
	public class RudpClient : RudpBase
	{
		/// <summary>
		/// Data to be sent next packet.
		/// </summary>
		private readonly Queue<byte[]> _sendDataQueue = new Queue<byte[]>();

		/// <summary>
		/// Table containing callback handle for sent packet events.
		/// </summary>
		private readonly Dictionary<ushort, Action<RudpClient, ushort, PacketStatus>> _pendingAckPackets = new Dictionary<ushort, Action<RudpClient, ushort, PacketStatus>>();

		/// <summary>
		/// List of packet delivery results.
		/// </summary>
		private readonly Dictionary<ushort, PacketStatus> _packetResults = new Dictionary<ushort, PacketStatus>();

		/// <summary>
		/// Stopwatch used as timestamp for each packet.
		/// </summary>
		private readonly Stopwatch _rttStopwatch = new Stopwatch();

		/// <summary>
		/// Timestamp for the pending acknowledge packets.
		/// </summary>
		private readonly Dictionary<ushort, long> _pendingPacketsTime = new Dictionary<ushort, long>();

		/// <summary>
		/// RTT of the last 33 packets.
		/// </summary>
		private readonly Dictionary<ushort, int> _rttList = new Dictionary<ushort, int>();

		/// <summary>
		/// Data received from the remote host.
		/// </summary>
		private readonly Queue<byte[]> _receiveDataQueue = new Queue<byte[]>();

		/// <summary>
		/// Tracks which of the last 32 packets from remote host were received.
		/// </summary>
		private readonly HashSet<ushort> _remotePacketAcks = new HashSet<ushort>();

		/// <summary>
		/// Contains the bound socket in client mode.
		/// </summary>
		private ISocket _socket;

		/// <summary>
		/// Handles time limit when connecting to remote host in client mode.
		/// </summary>
		private Stopwatch _timeoutStopwatch;

		/// <summary>
		/// Sequence number for the next packet.
		/// </summary>
		private ushort _nextSeqNumber;

		/// <summary>
		/// Sequence number last acknowledged from the remote host.
		/// </summary>
		private ushort _lastAckSeqNum;

		/// <summary>
		/// Last sequence number received from remote host.
		/// </summary>
		private ushort _lastRemoteSeqNumber;

		/// <summary>
		/// Initializes a new instance of the <see cref="RudpClient"/> class.
		/// Creates a new RudpClient instance in client mode.
		/// </summary>
		/// <param name="appId">Application Identifier to help avoid mismatched and/or unwanted data.</param>
		/// <param name="socket">Socket instance to inject.</param>
		/// <param name="updateMode">Execution mode for internal logic.</param>
		public RudpClient(ushort appId, ISocket socket = null, UpdateMode updateMode = UpdateMode.Thread)
			: base(appId, socket, null, updateMode)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RudpClient"/> class.
		/// Creates a new RudpClient instance in server mode.
		/// </summary>
		/// <param name="listener">Listener handling the communication thread.</param>
		/// <param name="endPoint">Remote host that this instance represents.</param>
		/// <param name="seqNumInit">Starting sequence number.</param>
		internal RudpClient(RudpListener listener, IPEndPoint endPoint, ushort seqNumInit)
			: base(listener.AppId, listener.Socket, endPoint, listener.UpdateMode)
		{
			Listener = listener;
			AppId = Listener.AppId;
			RemoteEndpoint = endPoint;
			_nextSeqNumber = seqNumInit;
			_rttStopwatch.Start();

			State = ClientState.Connected;
		}

		/// <summary>
		/// Gets received byte sequences count.
		/// </summary>
		public int Available => _receiveDataQueue.Count;

		/// <summary>
		/// Gets the remote endpoint.
		/// </summary>
		public IPEndPoint RemoteEndpoint { get; private set; }

		/// <summary>
		/// Gets average Round Trip Time over the last 32 packets.
		/// </summary>
		public int Rtt { get; private set; }

		/// <summary>
		/// Gets client state.
		/// </summary>
		public ClientState State { get; private set; }

		/// <summary>
		/// Gets listener in server mode.
		/// </summary>
		internal RudpListener Listener { get; private set; }

		/// <summary>
		/// Closes connection.
		/// </summary>
		public void Close()
		{
			IsActive = false;
		}

		/// <summary>
		/// Starts connection attempt to a remote host.
		/// </summary>
		/// <param name="address">Remote host IP address.</param>
		/// <param name="port">Remote host port number.</param>
		/// <returns>Method that executes update step if client is in <see cref="UpdateMode"/>.<see cref="UpdateMode.External"/> mode.</returns>
		public Action Connect(IPAddress address, int port)
		{
			return Connect(new IPEndPoint(address, port));
		}

		/// <summary>
		/// Starts connection attempt to a remote host.
		/// </summary>
		/// <param name="hostname">Remote host domain name.</param>
		/// <param name="port">Remote host port number.</param>
		/// <returns>Method that executes update step if client is in <see cref="UpdateMode"/>.<see cref="UpdateMode.External"/> mode.</returns>
		public Action Connect(string hostname, int port)
		{
			return Connect(Dns.GetHostAddresses(hostname)[0], port);
		}

		/// <summary>
		/// Starts connection attempt to a remote host.
		/// </summary>
		/// <param name="remoteEp">Remote host endpoint.</param>
		/// <returns>Method that executes update step if client is in <see cref="UpdateMode"/>.<see cref="UpdateMode.External"/> mode.</returns>
		public Action Connect(IPEndPoint remoteEp)
		{
			RemoteEndpoint = remoteEp;
			_lastRemoteSeqNumber = ushort.MaxValue;
			State = ClientState.Connecting;

			switch (UpdateMode)
			{
				case UpdateMode.Thread:
					UpdateThread = new Thread(ClientThread);
					UpdateThread.Start();
					return null;
				case UpdateMode.Task:
					throw new NotImplementedException();
				case UpdateMode.External:
					return ClientThread;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Receives packet data in the order they were received.
		/// </summary>
		/// <returns>data from received packet.</returns>
		public byte[] Receive()
		{
			lock (_receiveDataQueue)
			{
				return _receiveDataQueue.Dequeue();
			}
		}

		/// <summary>
		/// Queues data to be sent next packet.
		/// </summary>
		/// <param name="buffer">data to send.</param>
		/// <returns>size of sent buffer.</returns>
		public ushort Send(byte[] buffer)
		{
			var seqNumber = _nextSeqNumber;
			lock (_sendDataQueue)
			lock (_pendingAckPackets)
			{
				_sendDataQueue.Enqueue(buffer);
				if (!_pendingAckPackets.ContainsKey(seqNumber))
				{
					_pendingAckPackets.Add(_nextSeqNumber, null);
				}
				else
				{
					_pendingAckPackets[seqNumber] = null;
				}
			}

			return seqNumber;
		}

		/// <summary>
		/// Gets the packet delivery results ordered by sequence number.
		/// </summary>
		/// <returns>Delivery results for all not cleared packets.</returns>
		public List<PacketResult> GetPacketResults()
		{
			List<PacketResult> resultList;
			lock (_packetResults)
			{
				resultList = _packetResults
					.Select(kv => new PacketResult
					{
						SequenceNumber = kv.Key,
						Status = kv.Value,
					})
					.ToList();
			}

			resultList.Sort(delegate(PacketResult result1, PacketResult result2)
			{
				if (result1.SequenceNumber == result2.SequenceNumber)
				{
					return 0;
				}

				if (Packet.SequenceNumberGreaterThan(result1.SequenceNumber, result2.SequenceNumber))
				{
					return 1;
				}

				return -1;
			});

			return resultList;
		}

		/// <summary>
		/// Clear the packet delivery result list.
		/// </summary>
		public void ClearPacketResults()
		{
			lock (_packetResults)
			{
				_packetResults.Clear();
			}
		}

		/// <summary>
		/// Executes communication send step.
		/// </summary>
		/// <returns>Next packet to send.</returns>
		internal Packet SendUpdate()
		{
			Packet packet;

			// Checks if none of the last 32 packets sent were acknowledged.
			// If true it assumes connection dropped.
			if (Packet.SequenceNumberGreaterThan((ushort)(_nextSeqNumber - 1), (ushort)(_lastAckSeqNum + 32)))
			{
				packet = GetDisconnectPacket();
				IsActive = false;
				State = ClientState.Disconnected;
			}
			else
			{
				lock (_sendDataQueue)
				{
					byte[] dataBuffer = default;

					// Sends a Data packet if there is data to be sent.
					if (_sendDataQueue.Count > 0)
					{
						var bufferSize = _sendDataQueue
							.Select(d => d.Length)
							.Sum();
						dataBuffer = new byte[bufferSize];
						var offset = 0;
						while (_sendDataQueue.Count > 0)
						{
							var dequeuedBuffer = _sendDataQueue.Dequeue();
							Array.Copy(dequeuedBuffer, 0, dataBuffer, offset, dequeuedBuffer.Length);
							offset += dequeuedBuffer.Length;
						}
					}

					// Sends a KeepAlive packet if there is no data to send.
					else
					{
						lock (_pendingAckPackets)
						{
							_pendingAckPackets.Add(_nextSeqNumber, null);
						}
					}

					packet = new Packet()
					{
						AppId = AppId,
						SequenceNumber = _nextSeqNumber++,
						AckSequenceNumber = _lastRemoteSeqNumber,
						AckBitfield = GetReceivedBitfield(),
						Type = dataBuffer is null ? PacketType.KeepAlive : PacketType.Data,
						Data = dataBuffer is null ? default : new ArraySegment<byte>(dataBuffer),
					};
				}
			}

			// Add packet to timestamp list.
			_pendingPacketsTime.Add(packet.SequenceNumber, _rttStopwatch.ElapsedMilliseconds);

			// Add the packet to result list.
			lock (_packetResults)
			{
				_packetResults.Add(packet.SequenceNumber, PacketStatus.Pending);
			}

			return packet;
		}

		/// <summary>
		/// Assembles a disconnect <see cref="Packet"/> for this <see cref="RudpClient"/>.
		/// </summary>
		/// <returns>Disconnect <see cref="Packet"/> instance.</returns>
		internal Packet GetDisconnectPacket() =>
			new Packet()
			{
				AppId = AppId,
				SequenceNumber = _nextSeqNumber++,
				AckSequenceNumber = _lastRemoteSeqNumber,
				AckBitfield = GetReceivedBitfield(),
				Type = PacketType.DisconnectionNotify,
			};

		/// <summary>
		/// Executes communication receive step.
		/// </summary>
		/// <param name="packet">Received packet from remote host.</param>
		internal void ReceiveUpdate(Packet packet)
		{
			if (!ValidatePacket(packet, AppId, _lastRemoteSeqNumber))
			{
				return;
			}

			// Updates the received packet tracking from the remote host.
			_remotePacketAcks
				.RemoveWhere(sn => !Packet.SequenceNumberGreaterThan(sn, (ushort)(packet.SequenceNumber - 33)));
			_remotePacketAcks.Add(_lastRemoteSeqNumber);
			_lastRemoteSeqNumber = packet.SequenceNumber;

			// Updates sent packets acknowledgements.
			lock (_pendingAckPackets)
			lock (_packetResults)
			{
				void HandleAcknowledgedSequenceNumber(ushort sequenceNumber, PacketStatus packetStatus)
				{
					if (!_pendingAckPackets.ContainsKey(sequenceNumber))
					{
						return;
					}

					// Calls the registered callback for this packet's result.
					_pendingAckPackets[sequenceNumber]?.Invoke(this, sequenceNumber, packetStatus);
					if (!_packetResults.ContainsKey(sequenceNumber))
					{
						_packetResults.Add(sequenceNumber, packetStatus);
					}
					else
					{
						_packetResults[sequenceNumber] = packetStatus;
					}

					if (packetStatus == PacketStatus.Successful)
					{
						// RTT measurement.
						_rttList.Add(sequenceNumber, (int)(_rttStopwatch.ElapsedMilliseconds - _pendingPacketsTime[sequenceNumber]));
					}

					_pendingPacketsTime.Remove(sequenceNumber);
					_pendingAckPackets.Remove(sequenceNumber);
				}

				// Clears the pending acknowledge packets and measures RTT.
				packet.GetAcknowledgedPackets()
					.ForEach(sn => HandleAcknowledgedSequenceNumber(sn, PacketStatus.Successful));

				// Sequence numbers behinds by more than 32 are considered dropped.
				_pendingAckPackets
					.Select(p => p.Key)
					.Where(sn => !Packet.SequenceNumberGreaterThan(sn, (ushort)(packet.AckSequenceNumber - 33)))
					.ToList()
					.ForEach(sn => HandleAcknowledgedSequenceNumber(sn, PacketStatus.Dropped));

				_lastAckSeqNum = packet.AckSequenceNumber;
			}

			// Clears RTT measurements more than 33 packets old.
			_rttList
				.Select(p => p.Key)
				.Where(sn => !Packet.SequenceNumberGreaterThan(sn, (ushort)(packet.AckSequenceNumber - 33)))
				.ToList()
				.ForEach(sn => _rttList.Remove(sn));

			// Calculates average RTT over the last 33 packets.
			Rtt = _rttList.Sum(p => p.Value) / _rttList.Count > 0 ? _rttList.Count : 1;

			switch (State)
			{
				case ClientState.Connecting:
					switch (packet.Type)
					{
						case PacketType.ConnectionAccept:
							var packetData = packet.Data;
							SendRate = packetData.Array?[packetData.Offset + 0] ?? default;
							SendStopwatch = new Stopwatch();
							SendStopwatch.Start();
							_timeoutStopwatch.Stop();
							State = ClientState.Connected;
							break;
						case PacketType.ConnectionRefuse:
							IsActive = false;
							State = ClientState.Disconnected;
							break;
						case PacketType.ConnectionRequest:
						case PacketType.DisconnectionNotify:
						case PacketType.KeepAlive:
						case PacketType.Data:
						case PacketType.Invalid:
						default:
							throw new ArgumentOutOfRangeException();
					}

					break;
				case ClientState.Connected:
					switch (packet.Type)
					{
						case PacketType.DisconnectionNotify:
							IsActive = false;
							State = ClientState.Disconnected;
							break;
						case PacketType.Data:
							_receiveDataQueue.Enqueue(packet.Data.ToArray());
							break;
						case PacketType.KeepAlive:
							break;
						case PacketType.ConnectionAccept:
						case PacketType.ConnectionRefuse:
						case PacketType.ConnectionRequest:
						case PacketType.Invalid:
						default:
							throw new ArgumentOutOfRangeException();
					}

					break;
				case ClientState.Disconnecting:
				case ClientState.Disconnected:
				case ClientState.ForceClose:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Creates the bitfield acknowledge representation of the last 32 received packets.
		/// </summary>
		/// <returns>Bitfield representing the last 32 received packets.</returns>
		private Bitfield GetReceivedBitfield()
		{
			var bitfield = new Bitfield(4);

			for (ushort i = (ushort)(_lastRemoteSeqNumber - 1), j = 0;
				Packet.SequenceNumberGreaterThan(i, (ushort)(_lastRemoteSeqNumber - 33));
				i--, j++)
			{
				if (_remotePacketAcks.Contains(i))
				{
					bitfield[j] = true;
				}
			}

			return bitfield;
		}

		/// <summary>
		/// Implements communication logic in client mode.
		/// </summary>
		private void ClientThread()
		{
			// Instantiates RudpInternalSocket if null and binds the actual socket
			if (_socket == null)
			{
				_socket = new RudpInternalSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			}

			_socket.Bind(new IPEndPoint(IPAddress.Any, RemoteEndpoint.Port));

			// Sends connection request for client mode.
			_socket.Send(
				Packet.ToBytes(new Packet()
				{
					AppId = AppId,
					SequenceNumber = _nextSeqNumber++,
					AckSequenceNumber = 0,
					AckBitfield = new Bitfield(4),
					Type = PacketType.ConnectionRequest,
				}),
				RemoteEndpoint);

			IsActive = true;

			// Starts connection request timeout countdown
			_timeoutStopwatch = new Stopwatch();
			_timeoutStopwatch.Start();

			_rttStopwatch.Start();

			byte[] receiveBuffer = new byte[4096];

			// Connection handling main loop in client mode.
			while (IsActive)
			{
				if (_timeoutStopwatch.ElapsedMilliseconds > 500)
				{
					IsActive = false;
					State = ClientState.Disconnected;
				}

				// Executes receive update
				if (_socket.Available > 0)
				{
					EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
					var receiveCount = _socket.Receive(receiveBuffer, 0, receiveBuffer.Length, ref endPoint);

					if (endPoint.Equals(RemoteEndpoint))
					{
						ReceiveUpdate(Packet.FromBytes(receiveBuffer, 0, receiveCount));
					}
				}

				if (State == ClientState.Connected)
				{
					lock (SendStopwatch)
					{
						// Executes send update based on the send rate.
						if (SendStopwatch.ElapsedMilliseconds >= 1000 / SendRate)
						{
							SendStopwatch.Restart();
							var packet = SendUpdate();
							_socket.Send(Packet.ToBytes(packet), RemoteEndpoint);
						}
					}
				}

				Thread.Yield();
			}

			// Closes the connection and releases the socket.
			SendStopwatch?.Stop();
			_timeoutStopwatch?.Stop();
			State = ClientState.Disconnected;
			Close();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using System.IO;

using RUDP.Interfaces;
using RUDP.Enumerations;
using RUDP.Utils;

namespace RUDP
{
	public delegate void SendEventCallback(RudpClient client, ushort seqNumber, PacketResult sendEvent);

	/// <summary>
	/// Reliable User Datagram Protocol client implementation.
	/// </summary>
	public class RudpClient
	{
		/// <summary>
		/// States in which a client may be in.
		/// </summary>
		private enum State
		{
			Disconnected,
			Connecting,
			Connected,
			Disconnecting,
			ForceClose
		};

		/// <summary>
		/// Application Identifier.
		/// </summary>
		public ushort AppId { get; private set; }
		/// <summary>
		/// Number of packets sent per second.
		/// </summary>
		public int SendRate { get; private set; }
		/// <summary>
		/// Received byte sequences count.
		/// </summary>
		public int Available => _receiveDataQueue.Count;
		/// <summary>
		/// Indicates whether this client is connected to a remote host.
		/// </summary>
		public bool Connected => _state == State.Connected;
		/// <summary>
		/// Gets or Sets whether the socket remains connected until all data is sent.
		/// </summary>
		public LingerOption LingerState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		/// <summary>
		/// Gets the remote endpoint.
		/// </summary>
		public IPEndPoint RemoteEndpoint { get; private set; }
		/// <summary>
		/// Average Round Trip Time over the last 32 packets
		/// </summary>
		public int RTT { get; private set; }

		public bool IsConnecting { get; private set; }

		/// <summary>
		/// Contains the actual bound socket in server mode.
		/// </summary>
		internal RudpListener Listener { get; private set; }

		/// <summary>
		/// Contains the bound socket in client mode.
		/// </summary>
		private IRudpSocket _socket;
		/// <summary>
		/// Keeps client mode communication thread running.
		/// </summary>
		private bool _isActive = false;
		/// <summary>
		/// Communication handling thread in client mode.
		/// </summary>
		private Thread _clientThread;
		/// <summary>
		/// Handles timing between sent packets in client mode.
		/// </summary>
		private Stopwatch _sendStopwatch;
		/// <summary>
		/// Handles time limit when connecting to remote host in client mode.
		/// </summary>
		private Stopwatch _timeoutStopwatch;

		/// <summary>
		/// Sequence number for the next packet.
		/// </summary>
		private ushort _nextSeqNumber = 0;
		/// <summary>
		/// Sequence number last acknowleged from the remote host.
		/// </summary>
		private ushort _lastAckSeqNum = 0;
		/// <summary>
		/// Data to be sent next packet.
		/// </summary>
		private Queue<byte[]> _sendDataQueue = new Queue<byte[]>();
		/// <summary>
		/// Table containing callback handle for sent packet events.
		/// </summary>
		private Dictionary<ushort, SendEventCallback> _pendingAckPackets = new Dictionary<ushort, SendEventCallback>();
		/// <summary>
		/// List of packet delivery results.
		/// </summary>
		private Dictionary<ushort, PacketResult> _packetResults = new Dictionary<ushort, PacketResult>();

		/// <summary>
		/// Stopwatch used as timestamp for each packet.
		/// </summary>
		private Stopwatch _rttStopwatch = new Stopwatch();
		/// <summary>
		/// Timestamp for the pending ackowledge packets.
		/// </summary>
		private Dictionary<ushort, long> _pendingPacketsTime = new Dictionary<ushort, long>();
		/// <summary>
		/// RTT of the last 33 packets.
		/// </summary>
		private Dictionary<ushort, int> _rttList = new Dictionary<ushort, int>();

		/// <summary>
		/// Data received from the remote host.
		/// </summary>
		private Queue<byte[]> _receiveDataQueue = new Queue<byte[]>();
		/// <summary>
		/// Last sequence number received from remote host.
		/// </summary>
		private ushort _lastRemoteSeqNumber = 0;
		/// <summary>
		/// Tracks which of the last 32 packets from remote host were received.
		/// </summary>
		private HashSet<ushort> _remotePacketAcks = new HashSet<ushort>();

		/// <summary>
		/// Client state.
		/// </summary>
		private State _state = State.Disconnected;

		/// <summary>
		/// Creates a new RudpClient instance in client mode.
		/// </summary>
		/// <param name="appId"></param>
		public RudpClient(ushort appId)
		{
			AppId = appId;
		}

		/// <summary>
		/// Creates a new RudpClient instance in client mode.
		/// </summary>
		/// <param name="appId"></param>
		/// <param name="socket">socket instance to replace the standard.</param>
		public RudpClient(ushort appId, IRudpSocket socket) : this(appId)
		{
			_socket = socket;
		}

		/// <summary>
		/// Creates a new RudpClient instance in server mode.
		/// </summary>
		/// <param name="listener">Listener handling the communicaiton thread.</param>
		/// <param name="endPoint">Remote host that this instance represents.</param>
		/// <param name="seqNumInit">Starting sequence number.</param>
		internal RudpClient(RudpListener listener, IPEndPoint endPoint, ushort seqNumInit)
		{
			this.Listener = listener;
			AppId = Listener.AppId;
			RemoteEndpoint = endPoint;
			_nextSeqNumber = seqNumInit;
			_rttStopwatch.Start();

			_state = State.Connected;
		}

		/// <summary>
		/// Closes connection.
		/// </summary>
		public void Close()
		{
			_isActive = false;
		}

		/// <summary>
		/// Starts connection attemp to a remote host.
		/// </summary>
		/// <param name="address">Remote host IP address.</param>
		/// <param name="port">Remote host port number.</param>
		public void Connect(IPAddress address, int port)
		{
			Connect(new IPEndPoint(address, port));
		}

		/// <summary>
		/// Starts connection attemp to a remote host.
		/// </summary>
		/// <param name="hostname">Remote host domain name.</param>
		/// <param name="port">Remote host port number.</param>
		public void Connect(string hostname, int port)
		{
			Connect(Dns.GetHostAddresses(hostname)[0], port);
		}

		/// <summary>
		/// Starts connection attemp to a remote host.
		/// </summary>
		/// <param name="remoteEP">Remote host endpoint.</param>
		public void Connect(IPEndPoint remoteEP)
		{
			RemoteEndpoint = remoteEP;
			_lastRemoteSeqNumber = ushort.MaxValue;
			_state = State.Connecting;
			IsConnecting = true;

			_clientThread = new Thread(new ThreadStart(ClientThread));
			_clientThread.Start();
		}

		/// <summary>
		/// Receives packet data in the order they were received.
		/// </summary>
		/// <returns>data from received packet.</returns>
		public byte[] Receive()
		{
			lock(_receiveDataQueue)
				return _receiveDataQueue.Dequeue();
		}

		/// <summary>
		/// Queues data to be sent next packet.
		/// </summary>
		/// <param name="buffer">data to send.</param>
		/// <returns>size of sent buffer.</returns>
		public ushort Send(byte[] buffer)
		{
			ushort seqNumber = _nextSeqNumber;
			lock (_sendDataQueue)
				lock (_pendingAckPackets)
				{
					_sendDataQueue.Enqueue(buffer);
					if (!_pendingAckPackets.ContainsKey(seqNumber))
						_pendingAckPackets.Add(_nextSeqNumber, null);
					else
						_pendingAckPackets[seqNumber] = null;
				}

			return seqNumber;
		}

		[Obsolete]
		/// <summary>
		/// Queues data to be sent next packet.
		/// </summary>
		/// <param name="buffer">data to send.</param>
		/// <param name="seqNumber">sequence number of the packet that will carry the data.</param>
		/// <param name="callback">callback handle to notify packet result.</param>
		/// <returns></returns>
		public int Send(byte[] buffer, out ushort seqNumber, SendEventCallback callback)
		{
			seqNumber = _nextSeqNumber;
			lock(_sendDataQueue)
				lock(_pendingAckPackets)
				{
					_sendDataQueue.Enqueue(buffer);
					if (!_pendingAckPackets.ContainsKey(seqNumber))
						_pendingAckPackets.Add(_nextSeqNumber, callback);
					else
						_pendingAckPackets[seqNumber] = callback;
				}

			return buffer.Length;
		}

		/// <summary>
		/// Gets the packet delivery results ordered by sequence number.
		/// </summary>
		/// <returns></returns>
		public List<(ushort seqNum, PacketResult rudpEvent)> GetPacketResults()
		{
			List<(ushort seqNum, PacketResult rudpEvent)> resultList = new List<(ushort, PacketResult)>();
			lock(_packetResults)
				foreach (var result in _packetResults)
					resultList.Add((result.Key, result.Value));
			resultList.Sort(delegate((ushort seqNum, PacketResult rudpEvent) result1, (ushort seqNum, PacketResult rudpEvent) result2)
			{
				if (result1.seqNum == result2.seqNum)
					return 0;
				else if (Packet.SequenceNumberGreaterThan(result1.seqNum, result2.seqNum))
					return 1;
				else
					return -1;
			});

			return resultList;
		}

		/// <summary>
		/// Clear the packet delivery result list.
		/// </summary>
		public void ClearPacketResults()
		{
			lock(_packetResults)
				_packetResults.Clear();
		}

		/// <summary>
		/// Updates communication send step.
		/// </summary>
		/// <returns>Next packet to send.</returns>
		internal Packet SendUpdate()
		{
			Packet packet = null;

			//Checks if none of the last 32 packets sent were acknowledged.
			//If true it assumes connection dropped.
			if (Packet.SequenceNumberGreaterThan((ushort)(_nextSeqNumber - 1), (ushort)(_lastAckSeqNum + 32)))
			{
				packet = new Packet()
				{
					AppId = AppId,
					SequenceNumber = _nextSeqNumber++,
					AckSequenceNumber = _lastRemoteSeqNumber,
					AckBitfield = GetReceivedBitfield(),
					Type = PacketType.DisconnectionNotify,
				};

				_isActive = false;
				_state = State.Disconnected;
			}
			else
				lock (_sendDataQueue)
					// Sends a Data packet if there is data to be sent.
					if (_sendDataQueue.Count > 0)
					{
						using (MemoryStream stream = new MemoryStream())
						{
							// Concatenates all pending data in a sigle byte array.
							while (_sendDataQueue.Count > 0)
							{
								byte[] buffer = _sendDataQueue.Dequeue();
								stream.Write(buffer, 0, buffer.Length);
							}

							packet = new Packet()
							{
								AppId = AppId,
								SequenceNumber = _nextSeqNumber++,
								AckSequenceNumber = _lastRemoteSeqNumber,
								AckBitfield = GetReceivedBitfield(),
								Type = PacketType.Data,
								Data = new ArraySegment<byte>(stream.ToArray()),
							};
						}
					}
					// Sends a KeepAlive packet if there is no data to send.
					else
					{
						lock (_pendingAckPackets)
							_pendingAckPackets.Add(_nextSeqNumber, null);
						packet = new Packet()
						{
							AppId = AppId,
							SequenceNumber = _nextSeqNumber++,
							AckSequenceNumber = _lastRemoteSeqNumber,
							AckBitfield = GetReceivedBitfield(),
							Type = PacketType.KeepAlive,
							Data = new ArraySegment<byte>(),
						};
					}

			// Add packet to timestamp list.
			_pendingPacketsTime.Add(packet.SequenceNumber, _rttStopwatch.ElapsedMilliseconds);
			// Add the packet to result list.
			lock(_packetResults)
				_packetResults.Add(packet.SequenceNumber, PacketResult.Pending);

			return packet;
		}

		internal Packet GetDisconnectPacket()
		{
			return new Packet()
			{
				AppId = AppId,
				SequenceNumber = _nextSeqNumber,
				AckSequenceNumber = _lastRemoteSeqNumber,
				AckBitfield = GetReceivedBitfield(),
				Type = PacketType.DisconnectionNotify
			};
		}

		/// <summary>
		/// Creates the bitfield acknowledge representation of the last 32 received packets.
		/// </summary>
		/// <returns>Bitfield representing the last 32 received packets.</returns>
		private Bitfield GetReceivedBitfield()
		{
			Bitfield bitfield = new Bitfield(4);

			for (ushort i = (ushort)(_lastRemoteSeqNumber-1), j = 0; Packet.SequenceNumberGreaterThan(i, (ushort)(_lastRemoteSeqNumber - 33)); i--, j++)
				if (_remotePacketAcks.Contains(i))
					bitfield[j] = true;

			return bitfield;
		}

		/// <summary>
		/// Updates communication receive step.
		/// </summary>
		/// <param name="packet">Received packet from remote host.</param>
		internal void ReceiveUpdate(Packet packet)
		{
			if (packet.Validate(AppId, _lastRemoteSeqNumber))
			{
				// Updates the received packet tracking from the remote host.
				_remotePacketAcks.Add(_lastRemoteSeqNumber);
				_remotePacketAcks.RemoveWhere(seqNum => !Packet.SequenceNumberGreaterThan(seqNum, (ushort)(packet.SequenceNumber - 33)));
				_lastRemoteSeqNumber = packet.SequenceNumber;

				// Updates sent packets acknowledgements.
				lock(_pendingAckPackets)
				{
					// Clears the pending acknowledge packets and measures RTT.
					// Starting by the sequence number acknowledge.
					if (_pendingAckPackets.ContainsKey(packet.AckSequenceNumber))
					{
						// Calls the registered callback for this packet's result.
						_pendingAckPackets[packet.AckSequenceNumber]?.Invoke(this, packet.AckSequenceNumber, PacketResult.Successful);
						// Add the packet result to result list.
						lock (_packetResults)
							if (!_packetResults.ContainsKey(packet.AckSequenceNumber))
								_packetResults.Add(packet.AckSequenceNumber, PacketResult.Successful);
							else
								_packetResults[packet.AckSequenceNumber] = PacketResult.Successful;

						// RTT measurement.
						_rttList.Add(packet.AckSequenceNumber, (int)(_rttStopwatch.ElapsedMilliseconds - _pendingPacketsTime[packet.AckSequenceNumber]));

						_pendingPacketsTime.Remove(packet.AckSequenceNumber);
						_pendingAckPackets.Remove(packet.AckSequenceNumber);
					}
					// Then the 32 previous using the bitfield.
					for (ushort i = (ushort)(packet.AckSequenceNumber - 1), j = 0; Packet.SequenceNumberGreaterThan(i, (ushort)(packet.AckSequenceNumber - 33)); i--, j++)
					{
						if (packet.AckBitfield[j] == true && _pendingAckPackets.ContainsKey(i))
						{
							// Calls the registered callback for this packet's result.
							_pendingAckPackets[i]?.Invoke(this, i, PacketResult.Successful);
							// Add the packet result to result list.
							lock (_packetResults)
								if (!_packetResults.ContainsKey(i))
									_packetResults.Add(i, PacketResult.Successful);
								else
									_packetResults[i] = PacketResult.Successful;

							// RTT measurement.
							_rttList.Add(i, (int)(_rttStopwatch.ElapsedMilliseconds - _pendingPacketsTime[i]));

							_pendingPacketsTime.Remove(i);
							_pendingAckPackets.Remove(i);
						}
					}
					// Sequence numbers behinds by more than 32 are considered dropped.
					foreach (var pair in _pendingAckPackets.Where(seqNum => !Packet.SequenceNumberGreaterThan(seqNum.Key, (ushort)(packet.AckSequenceNumber - 33))).ToList())
					{
						// Calls the registered callback for this packet's result.
						_pendingAckPackets[pair.Key]?.Invoke(this, pair.Key, PacketResult.Dropped);
						// Add the packet result to result list.
						lock (_packetResults)
							if (!_packetResults.ContainsKey(pair.Key))
								_packetResults.Add(pair.Key, PacketResult.Dropped);
							else
								_packetResults[pair.Key] = PacketResult.Dropped;

						_pendingPacketsTime.Remove(pair.Key);
						_pendingAckPackets.Remove(pair.Key);
					}
					_lastAckSeqNum = packet.AckSequenceNumber;
				}

				// Clears RTT measurements more than 33 packets old.
				foreach (var pair in _rttList.Where(seqNum => !Packet.SequenceNumberGreaterThan(seqNum.Key, (ushort)(packet.AckSequenceNumber - 33))).ToList())
					_rttList.Remove(pair.Key);

				// Calculates average RTT over the last 33 packets.
				int rtt = 0;
				foreach (KeyValuePair<ushort, int> pair in _rttList)
					rtt += pair.Value;
				if(_rttList.Count > 0)
					rtt /= _rttList.Count;
				RTT = rtt;

				switch(_state)
				{
					case State.Connecting:
						switch(packet.Type)
						{
							case PacketType.ConnectionAccept:
								var packetData = packet.Data; 
								SendRate = packetData.Array[packetData.Offset + 0];
								_sendStopwatch = new Stopwatch();
								_sendStopwatch.Start();
								_timeoutStopwatch.Stop();
								_state = State.Connected;
								IsConnecting = false;
								break;
							case PacketType.ConnectionRefuse:
								_isActive = false;
								_state = State.Disconnected;
								break;
						}
						break;
					case State.Connected:
						switch(packet.Type)
						{
							case PacketType.DisconnectionNotify:
								_isActive = false;
								_state = State.Disconnected;
								break;
							case PacketType.Data:
								_receiveDataQueue.Enqueue(packet.Data.ToArray());
								break;
						}
						break;
					case State.Disconnecting:
						break;
					default:
						break;
				}
			}
		}


		/// <summary>
		/// Implements communication logic in client mode. 
		/// </summary>
		private void ClientThread()
		{
			// Instantiates RudpInternalSocket if null and binds the actual socket
			if (_socket == null)
				_socket = new RudpInternalSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_socket.Bind(new IPEndPoint(IPAddress.Any, RemoteEndpoint.Port));

			// Sends connection request for client mode.
			_socket.SendTo(new Packet()
			{
				AppId = AppId,
				SequenceNumber = _nextSeqNumber++,
				AckSequenceNumber = 0,
				AckBitfield = new Bitfield(4),
				Type = PacketType.ConnectionRequest
			}.ToBytes(),
			RemoteEndpoint);

			_isActive = true;

			// Starts connection request timeout countdown
			_timeoutStopwatch = new Stopwatch();
			_timeoutStopwatch.Start();

			_rttStopwatch.Start();

			byte[] receiveBuffer = new byte[4096];

			// Connection handling main loop in client mode.
			while (_isActive)
			{
				if (_timeoutStopwatch.ElapsedMilliseconds > 500)
				{
					_isActive = false;
					_state = State.Disconnected;
					IsConnecting = false;
				}

				// Executes receive update
				if (_socket.Available > 0)
				{
					EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
					int receiveCount = _socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref endPoint);

					if(endPoint.Equals(RemoteEndpoint))
						ReceiveUpdate(new Packet(receiveBuffer, 0, receiveCount));
				}
				
				if(_state == State.Connected)
					lock(_sendStopwatch)
						// Executes send update based on the send rate.
						if(_sendStopwatch.ElapsedMilliseconds >= 1000/SendRate)
						{
							_sendStopwatch.Restart();
							Packet packet = SendUpdate();
							_socket.SendTo(packet.ToBytes(), RemoteEndpoint);
						}

				Thread.Yield();
			}

			// Closes the connection and releases the socket.
			_sendStopwatch?.Stop();
			_timeoutStopwatch?.Stop();
			_state = State.Disconnected;
			Close();
		}
	}
}

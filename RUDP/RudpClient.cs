using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

using RUDP.Interfaces;
using RUDP.Enumerations;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.IO;

namespace RUDP
{
	public delegate void SendEventCallback(ushort seqNumber, RudpEvent sendEvent);

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
		/// Contains the actual bound socket in server mode.
		/// </summary>
		internal RudpListener Listener { get; private set; }

		/// <summary>
		/// Contains the bound socket in client mode.
		/// </summary>
		private IRudpSocket _socket;
		/// <summary>
		/// Communication handling thread in client mode.
		/// </summary>
		private Thread _clientThread;
		/// <summary>
		/// Tracks send delay between packets in client mode.
		/// </summary>
		private Stopwatch _sendStopwatch;
		private Stopwatch _timeoutStopwatch;

		private ushort _nextSeqNumber = 0;
		private ushort _lastAckSeqNum = 0;
		private Queue<byte[]> _sendDataQueue = new Queue<byte[]>();
		private Dictionary<ushort, SendEventCallback> _pendingAckPackets = new Dictionary<ushort, SendEventCallback>();

		private Queue<byte[]> _receiveDataQueue = new Queue<byte[]>();
		private ushort _lastRemoteSeqNumber = 0;
		private HashSet<ushort> _remotePacketAcks = new HashSet<ushort>();

		private State _state = State.Disconnected;
		private bool _isActive = false;

		public RudpClient(ushort appId)
		{
			AppId = appId;
		}

		internal RudpClient(RudpListener listener, IPEndPoint endPoint, ushort seqNumInit)
		{
			this.Listener = listener;
			AppId = Listener.AppId;
			RemoteEndpoint = endPoint;
			_nextSeqNumber = seqNumInit;

			_state = State.Connected;
		}

		public void Close()
		{
			_isActive = false;
		}

		public void Connect(IPAddress address, int port)
		{
			Connect(new IPEndPoint(address, port));
		}

		public void Connect(string hostname, int port)
		{
			Connect(Dns.GetHostAddresses(hostname)[0], port);
		}

		public void Connect(IPEndPoint remoteEP)
		{
			RemoteEndpoint = remoteEP;
			_lastRemoteSeqNumber = ushort.MaxValue;
			_state = State.Connecting;

			_clientThread = new Thread(new ThreadStart(ClientThread));
			_clientThread.Start();
		}

		public byte[] Receive()
		{
			lock(_receiveDataQueue)
				return _receiveDataQueue.Dequeue();
		}

		public int Send(byte[] buffer)
		{
			ushort seqNum;
			return Send(buffer, out seqNum, null);
		}

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

		internal (Packet packet, RudpEvent rudpEvent) SendUpdate()
		{
			Packet packet = null;
			RudpEvent rudpEvent = RudpEvent.Successful;

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

				rudpEvent = RudpEvent.Disconnected;
				_isActive = false;
				_state = State.Disconnected;
			}
			else
				lock (_sendDataQueue)
					if (_sendDataQueue.Count > 0)
					{
						using (MemoryStream stream = new MemoryStream())
						{
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
								Data = stream.ToArray(),
							};
						}
					}
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
							Data = null,
						};
					}

			return (packet, rudpEvent);
		}

		private Bitfield GetReceivedBitfield()
		{
			Bitfield bitfield = new Bitfield(4);

			for (ushort i = (ushort)(_lastRemoteSeqNumber-1), j = 0; Packet.SequenceNumberGreaterThan(i, (ushort)(_lastRemoteSeqNumber - 33)); i--, j++)
				if (_remotePacketAcks.Contains(i))
					bitfield[j] = true;

			return bitfield;
		}

		internal void ReceiveUpdate(Packet packet)
		{
			if (packet.Validate(AppId, _lastRemoteSeqNumber))
			{
				_remotePacketAcks.Add(_lastRemoteSeqNumber);
				_remotePacketAcks.RemoveWhere(seqNum => !Packet.SequenceNumberGreaterThan(seqNum, (ushort)(packet.SequenceNumber - 33)));
				_lastRemoteSeqNumber = packet.SequenceNumber;

				lock(_pendingAckPackets)
				{
					if (_pendingAckPackets.ContainsKey(packet.AckSequenceNumber))
					{
						_pendingAckPackets[packet.AckSequenceNumber]?.Invoke(packet.AckSequenceNumber, RudpEvent.Successful);
						_pendingAckPackets.Remove(packet.AckSequenceNumber);
					}
					for (ushort i = (ushort)(packet.AckSequenceNumber - 1), j = 0; Packet.SequenceNumberGreaterThan(i, (ushort)(packet.AckSequenceNumber - 33)); i--, j++)
					{
						if (packet.AckBitfield[j] == true && _pendingAckPackets.ContainsKey(i))
						{
							_pendingAckPackets[i]?.Invoke(i, RudpEvent.Successful);
							_pendingAckPackets.Remove(i);
						}
					}
					foreach (var pair in _pendingAckPackets.Where(seqNum => !Packet.SequenceNumberGreaterThan(seqNum.Key, (ushort)(packet.AckSequenceNumber - 33))).ToList())
					{
						_pendingAckPackets[pair.Key]?.Invoke(pair.Key, RudpEvent.Dropped);
						_pendingAckPackets.Remove(pair.Key);
					}
					_lastAckSeqNum = packet.AckSequenceNumber;
				}

				switch(_state)
				{
					case State.Connecting:
						switch(packet.Type)
						{
							case PacketType.ConnectionAccept:
								SendRate = packet.Data[0];
								_sendStopwatch = new Stopwatch();
								_sendStopwatch.Start();
								_timeoutStopwatch.Stop();
								_state = State.Connected;
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
								_receiveDataQueue.Enqueue(packet.Data);
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


		private void ClientThread()
		{
			_socket = new RudpInternalSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_socket.Bind(new IPEndPoint(IPAddress.Any, RemoteEndpoint.Port));

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
			_timeoutStopwatch = new Stopwatch();
			_timeoutStopwatch.Start();

			byte[] receiveBuffer = new byte[4096];

			while (_isActive)
			{
				if (_timeoutStopwatch.ElapsedMilliseconds > 500)
				{
					_isActive = false;
					_state = State.Disconnected;
				}

				if (_socket.Available > 0)
				{
					EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
					int receiveCount = _socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref endPoint);

					if(endPoint.Equals(RemoteEndpoint))
					{
						Packet packet = new Packet(receiveBuffer, 0, receiveCount);
						ReceiveUpdate(packet);
					}					
				}
				
				if(_state == State.Connected)
					lock(_sendStopwatch)
						if(_sendStopwatch.ElapsedMilliseconds >= 1000/SendRate)
						{
							_sendStopwatch.Restart();
							(Packet packet, RudpEvent rudpEvent) = SendUpdate();
							_socket.SendTo(packet.ToBytes(), RemoteEndpoint);

							if (rudpEvent == RudpEvent.Disconnected)
								break;
						}

				Thread.Yield();
			}
			_sendStopwatch?.Stop();
			_timeoutStopwatch?.Stop();
			Close();
		}
	}
}

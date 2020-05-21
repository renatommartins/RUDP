using RUDP;
using RUDP.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Sample_ChatConsoleApp
{
	/// <summary>
	/// Sample networked chat server using RudpListener as underlying communication protocol.
	/// </summary>
	class ChatServer
	{
		/// <summary>
		/// RUDP listener used for communication with the clients.
		/// </summary>
		RudpListener _listener;
		/// <summary>
		/// Connected clients list.
		/// </summary>
		List<RudpClient> _clients = new List<RudpClient>();
		/// <summary>
		/// List of the messages sent to each client that are waiting for acknowledgement.
		/// </summary>
		Dictionary<RudpClient, Dictionary<ushort, string>> _pendingAckMessages = new Dictionary<RudpClient, Dictionary<ushort, string>>();
		/// <summary>
		/// List of the messages to broadcast to clients with the exception of the sender.
		/// </summary>
		List<(string message, List<RudpClient> targets)> _broadcastMessageList = new List<(string, List<RudpClient>)>();

		/// <summary>
		/// Creates a new ChatServer instance and starts listening to new connections immediately.
		/// </summary>
		/// <param name="appId"></param>
		/// <param name="bindEndpoint"></param>
		public ChatServer(ushort appId, IPEndPoint bindEndpoint)
		{
			_listener = new RudpListener(appId, bindEndpoint, 100);
			_listener.Start();
		}

		/// <summary>
		/// Server mode main loop.
		/// </summary>
		public void ServerMain()
		{
			while (_listener.Active)
			{
				// Accepts all remote clients requesting to connect.
				while (_listener.Pending())
				{
					RudpClient newClient = _listener.AcceptClient();
					// Adds client to connected clients list.
					_clients.Add(newClient);
					// Notifies the other users taht a new user joined.
					_broadcastMessageList.Add(($"Client [{newClient.RemoteEndpoint}] has connected", _clients));
					// Prepares the new client pending messages list.
					lock (_pendingAckMessages)
						_pendingAckMessages.Add(newClient, new Dictionary<ushort, string>());
				}

				// Checks for disconnected clients
				List<RudpClient> disconnectedClients = new List<RudpClient>();
				foreach (RudpClient client in _clients)
					if (!client.Connected)
					{
						disconnectedClients.Add(client);
						// Notifies the other users that a disconnection happened.
						_broadcastMessageList.Add(($"Client [{client.RemoteEndpoint}] has disconnected", _clients));
					}
				foreach (RudpClient client in disconnectedClients)
				{
					_clients.Remove(client);
					// Releases the disconnected client pending messages list.
					lock (_pendingAckMessages)
						_pendingAckMessages.Remove(client);
				}

				// Checks the acknowledgement state of sent packets for each client.
				foreach (RudpClient client in _clients)
				{
					var resultList = client.GetPacketResults();
					foreach(var result in resultList)
						lock(_pendingAckMessages)
							if(_pendingAckMessages[client].ContainsKey(result.seqNum))
								// Clears the message if it was acknowledged by the client.
								if(result.rudpEvent == RudpEvent.Successful)
								{
									Console.WriteLine($"MESSAGE DELIVERED [{client.RemoteEndpoint} - {result.seqNum}: \"{_pendingAckMessages[client][result.seqNum]}\"]");
									_pendingAckMessages[client].Remove(result.seqNum);
								}
								// If message gets dropped, it is resent until acknowledged by the client.
								else if(result.rudpEvent == RudpEvent.Dropped)
								{
									Console.WriteLine($"RESENDING DROPPED MESSAGE[{client.RemoteEndpoint} - {result.seqNum} : \"{_pendingAckMessages[client][result.seqNum]}\"]");
									ushort newSeqNum = client.Send(Encoding.UTF8.GetBytes(_pendingAckMessages[client][result.seqNum]));

									// Replaces the sequence number for the new one to keep tracking the message retry.
									_pendingAckMessages[client].Add(newSeqNum, _pendingAckMessages[client][result.seqNum]);
									_pendingAckMessages[client].Remove(result.seqNum);
								}
					client.ClearPacketResults();
				}

				// Checks each connected client for messages and adds to the broadcast list if there is.
				foreach (RudpClient client in _clients)
					while (client.Available > 0)
						_broadcastMessageList.Add((Encoding.UTF8.GetString(client.Receive()), _clients.Where(c => c != client).ToList()));

				// Broadcasts each message in the broadcast message.
				foreach (var message in _broadcastMessageList)
				{
					foreach (RudpClient client in message.targets)
					{
						ushort seqNum = client.Send(Encoding.UTF8.GetBytes(message.message));
						// Adds the message to the client's pending acknowledgement list.
						lock (_pendingAckMessages)
							_pendingAckMessages[client].Add(seqNum, message.message);
					}
				}

				_broadcastMessageList.Clear();
			}
		}
	}
}

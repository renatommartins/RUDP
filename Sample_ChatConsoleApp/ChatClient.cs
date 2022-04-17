namespace Sample_ChatConsoleApp
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Threading;
	using RUDP;
	using RUDP.Enumerations;

	/// <summary>
	/// Sample networked chat client using RudpClient as underlying communication protocol.
	/// </summary>
	internal class ChatClient
	{
		/// <summary>
		/// User's nickname in the chat.
		/// </summary>
		private readonly string _clientName;

		/// <summary>
		/// RUDP client used for communication with the server.
		/// </summary>
		private readonly RudpClient _client;

		/// <summary>
		/// Stores user input.
		/// </summary>
		private readonly StringBuilder _writeContent = new StringBuilder();

		/// <summary>
		/// Received messages log.
		/// </summary>
		private List<(string text, DateTime timestamp, int rtt, bool sent)> _messageLog = new List<(string, DateTime, int, bool)>();

		/// <summary>
		/// Messages sent by client waiting for server acknowledgement.
		/// </summary>
		private Dictionary<ushort, string> _pendingAckMessages = new Dictionary<ushort, string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatClient"/> class and tries to connect to remote immediately.
		/// </summary>
		/// <param name="clientName">Name to be used in chat.</param>
		/// <param name="appId">Application identifier.</param>
		/// <param name="endPoint">Remote endpoint to connect to.</param>
		public ChatClient(string clientName, ushort appId, IPEndPoint endPoint)
		{
			_clientName = clientName;
			_client = new RudpClient(appId);

			_client.Connect(endPoint);
		}

		/// <summary>
		/// Client mode main loop.
		/// </summary>
		public void ClientMain()
		{
			// Waits until handshake ends.
			Console.WriteLine("Connecting...");
			while (_client.State == ClientState.Connecting)
			{
				Thread.Sleep(50);
			}

			// Checks if handshake was successful.
			if (_client.State != ClientState.Connected)
			{
				Console.WriteLine("Connection failed!");
				return;
			}

			// Initial UI draw
			DrawUi();

			// Keeps track of last console window size.
			var lastWidth = Console.WindowWidth;
			var lastHeight = Console.WindowHeight;

			// Runs while client is connected and stopping the program if it disconnects.
			while (_client.State == ClientState.Connected)
			{
				// If console size changes, resizes and redraws the UI.
				if (lastWidth != Console.WindowWidth || Console.WindowHeight != lastHeight)
				{
					lastWidth = Console.WindowWidth;
					lastHeight = Console.WindowHeight;

					DrawUi();
				}

				// Check if client has received messages from the server.
				if (_client.Available > 0)
				{
					// Adds the received message to message log and redraws UI.
					_messageLog.Add((Encoding.UTF8.GetString(_client.Receive()), DateTime.Now, _client.Rtt, false));
					DrawUi();
				}

				// Checks the acknowledgement state of sent packets.
				var resultList = _client.GetPacketResults();

				lock (_pendingAckMessages)
				{
					resultList
						.Where(result => _pendingAckMessages.ContainsKey(result.SequenceNumber))
						.ToList()
						.ForEach(pr =>
						{
							switch (pr.Status)
							{
								// Just clears the message sent if it was acknowledged by the server.
								case PacketStatus.Successful:
									_pendingAckMessages.Remove(pr.SequenceNumber);
									break;

								// If message is dropped, it is resent until it is acknowledged by the server.
								case PacketStatus.Dropped:
									// Notifies the user that it is trying to resend a dropped message.
									_messageLog.Add(($"System - RESENDING DROPPED MESSAGE [{pr.SequenceNumber}]",
										DateTime.Now, 0,
										true));
									var newSeqNum =
										_client.Send(Encoding.UTF8.GetBytes(_pendingAckMessages[pr.SequenceNumber]));

									// Replaces the sequence number for the new one to keep tracking the message retry.
									_pendingAckMessages.Add(newSeqNum, _pendingAckMessages[pr.SequenceNumber]);
									_pendingAckMessages.Remove(pr.SequenceNumber);
									break;

								case PacketStatus.Pending:
								case PacketStatus.Delayed:
									break;
								default:
									throw new ArgumentOutOfRangeException();
							}
						});
				}

				_client.ClearPacketResults();

				// Checks for user input.
				if (!Console.KeyAvailable)
				{
					continue;
				}

				var keyInfo = Console.ReadKey(true);

				// Sends user input to server if "enter" is pressed.
				if (keyInfo.Key != ConsoleKey.Enter)
				{
					Console.Write(keyInfo.KeyChar);
					_writeContent.Append(keyInfo.KeyChar);
				}

				// else stores the input.
				else
				{
					// Checks for exit command and exits application if it matches.
					if (string.Equals(_writeContent.ToString(), "/exit"))
					{
						_client.Close();
						continue;
					}

					// Sends user input to server.
					var sendString = _clientName + ": " + _writeContent;
					var seqNum = _client.Send(Encoding.UTF8.GetBytes(sendString));

					// Adds the message to the list of messages waiting server acknowledgement.
					lock (_pendingAckMessages)
					{
						_pendingAckMessages.Add(seqNum, sendString);
					}

					// Add message to log, so the user can view his sent messages.
					_messageLog.Add((sendString, DateTime.Now, 0, true));

					// Clear user input and redraws UI.
					_writeContent.Clear();
					DrawUi();
				}
			}

			Console.Clear();
			Console.WriteLine("Connection dropped!");
		}

		/// <summary>
		/// Draws user UI.
		/// </summary>
		private void DrawUi()
		{
			Console.Clear();

			// Draws top bar.
			for (var i = 0; i < Console.WindowWidth * 2; i++)
			{
				Console.Write('=');
			}

			// Draws Side columns.
			for (var i = 0; i < Console.WindowHeight - 5; i++)
			{
				Console.SetCursorPosition(0, i + 2);
				Console.Write('|');
				Console.SetCursorPosition(Console.WindowWidth - 1, i + 2);
				Console.Write('|');
			}

			// Draws user text box
			for (var i = 0; i < Console.WindowWidth; i++)
			{
				Console.Write('=');
			}

			Console.Write("| Send: ");
			Console.SetCursorPosition(Console.WindowWidth - 1, Console.WindowHeight - 2);
			Console.Write('|');
			for (var i = 0; i < Console.WindowWidth; i++)
			{
				Console.Write('=');
			}

			// Draws received messages bottom to top with the latest one at the bottom.
			for (var i = 0; i < _messageLog.Count && i < Console.WindowHeight - 5; i++)
			{
				Console.SetCursorPosition(2, Console.WindowHeight - 4 - i);
				var message = _messageLog[_messageLog.Count - i - 1];

				// Checks if the message was sent by the user.
				Console.Write(message.sent
					? $"> [{message.timestamp.ToLongTimeString()}] - \"{message.text}\""
					: $"< [{message.timestamp.ToLongTimeString()}] - \"{message.text}\" [RTT: {message.rtt} ms]");
			}

			// Sets console cursor position at the text box for the user.
			Console.SetCursorPosition(9, Console.WindowHeight - 2);
			if (_writeContent.Length > 0)
			{
				Console.Write(_writeContent.ToString());
			}
		}
	}
}

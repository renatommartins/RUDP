using RUDP;
using RUDP.Enumerations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace Sample_ChatConsoleApp
{
	/// <summary>
	/// Sample networked chat client using RudpClient as underlying communication protocol.
	/// </summary>
	class ChatClient
	{
		/// <summary>
		/// User's nickname in the chat.
		/// </summary>
		string _clientName;
		/// <summary>
		/// RUDP client used for communication with the server.
		/// </summary>
		RudpClient _client;
		/// <summary>
		/// Stores user input.
		/// </summary>
		StringBuilder _writeContent = new StringBuilder();
		/// <summary>
		/// Received messages log.
		/// </summary>
		List<(string text, DateTime timestamp, int rtt, bool sent)> _messageLog = new List<(string, DateTime, int, bool)>();
		/// <summary>
		/// Messages sent by client waiting for server ackowledgement.
		/// </summary>
		Dictionary<ushort, string> _pendingAckMessages = new Dictionary<ushort, string>();

		/// <summary>
		/// Creates a new ChatClient and tries to connect to remote immediately.
		/// </summary>
		/// <param name="clientName"></param>
		/// <param name="appId"></param>
		/// <param name="endPoint"></param>
		public ChatClient(string clientName, ushort appId, IPEndPoint endPoint)
		{
			this._clientName = clientName;
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
			while (_client.IsConnecting)
				Thread.Sleep(50);

			// Checks if handshake was successful.
			if (!_client.Connected)
			{
				Console.WriteLine("Connection failed!");
				return;
			}

			// Initial UI draw
			DrawUI();

			// Keeps track of last console window size.
			int lastWidth = Console.WindowWidth;
			int lastHeight = Console.WindowHeight;

			// Runs while client is connected and stopping the program if it disconnects.
			while (_client.Connected)
			{
				// If console size changes, resizes and redraws the UI.
				if (lastWidth != Console.WindowWidth || Console.WindowHeight != lastHeight)
				{
					lastWidth = Console.WindowWidth;
					lastHeight = Console.WindowHeight;

					DrawUI();
				}

				// Check if client has received messages from the server.
				if (_client.Available > 0)
				{
					// Adds the received message to message log and redraws UI.
					_messageLog.Add((Encoding.UTF8.GetString(_client.Receive()), DateTime.Now, _client.RTT, false));
					DrawUI();
				}

				// Checks the acknowledgement state of sent packets.
				var resultList = _client.GetPacketResults();
				foreach(var result in resultList)
					if(_pendingAckMessages.ContainsKey(result.seqNum))
						// Just clears the message sent if it was acknowledged by the server.
						if (result.rudpEvent == RudpEvent.Successful)
							_pendingAckMessages.Remove(result.seqNum);
						// If message is dropped, it is resent until it is acknowledged by the server.
						else if (result.rudpEvent == RudpEvent.Dropped)
						{
							// Notifies the user that it is trying to resend a dropped message.
							_messageLog.Add(($"System - RESENDING DROPPED MESSAGE [{result.seqNum}]", DateTime.Now, 0, true));
							ushort newSeqNum = _client.Send(Encoding.UTF8.GetBytes(_pendingAckMessages[result.seqNum]));

							// Replaces the sequence number for the new one to keep tracking the message retry.
							_pendingAckMessages.Add(newSeqNum, _pendingAckMessages[result.seqNum]);
							_pendingAckMessages.Remove(result.seqNum);
						}
				_client.ClearPacketResults();

				// Checks for user input.
				if (Console.KeyAvailable)
				{
					ConsoleKeyInfo keyInfo = Console.ReadKey(true);

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
						if (String.Equals(_writeContent.ToString(), "/exit"))
						{
							_client.Close();
							continue;
						}

						// Sends user input to server.
						string sendString = _clientName + ": " + _writeContent.ToString();
						ushort seqNum = _client.Send(Encoding.UTF8.GetBytes(sendString));

						// Adds the message to the list of messages waiting server acknowledgement.
						lock (_pendingAckMessages)
							_pendingAckMessages.Add(seqNum, sendString);

						// Add message to log, so the user can view his sent messages.
						_messageLog.Add((sendString, DateTime.Now, 0, true));

						// Clear user input and redraws UI.
						_writeContent.Clear();
						DrawUI();
					}

				}
			}

			Console.Clear();
			Console.WriteLine("Connection dropped!");
			return;
		}

		/// <summary>
		/// Draws user UI.
		/// </summary>
		void DrawUI()
		{
			Console.Clear();

			// Draws top bar.
			for (int i = 0; i < Console.WindowWidth * 2; i++)
				Console.Write('=');

			// Draws Side columns.
			for (int i = 0; i < Console.WindowHeight - 5; i++)
			{
				Console.SetCursorPosition(0, i + 2);
				Console.Write('|');
				Console.SetCursorPosition(Console.WindowWidth - 1, i + 2);
				Console.Write('|');
			}

			// Draws user text box
			for (int i = 0; i < Console.WindowWidth; i++)
				Console.Write('=');
			Console.Write("| Send: ");
			Console.SetCursorPosition(Console.WindowWidth - 1, Console.WindowHeight - 2);
			Console.Write('|');
			for (int i = 0; i < Console.WindowWidth; i++)
				Console.Write('=');

			// Draws received messages bottom to top with the latest one at the bottom.
			for (int i = 0; i < _messageLog.Count && i < Console.WindowHeight - 5; i++)
			{
				Console.SetCursorPosition(2, Console.WindowHeight - 4 - i);
				var message = _messageLog[_messageLog.Count - i - 1];
				// Checks if the message was sent by the user.
				if (message.sent)
					Console.Write($"> [{message.timestamp.ToLongTimeString()}] - \"{message.text}\"");
				// Or received from the server.
				else
					Console.Write($"< [{message.timestamp.ToLongTimeString()}] - \"{message.text}\" [RTT: {message.rtt} ms]");
			}

			// Sets console cursor position at the text box for the user.
			Console.SetCursorPosition(9, Console.WindowHeight - 2);
			if (_writeContent.Length > 0)
				Console.Write(_writeContent.ToString());
		}
	}
}

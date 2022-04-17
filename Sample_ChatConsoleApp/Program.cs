namespace Sample_ChatConsoleApp
{
	using System;
	using System.Net;

	/// <summary>
	/// Sample chat using RUDP protocol.
	/// </summary>
	internal static class Program
	{
		private const string HelpText =
			"Usage: Sample_ChatConsoleApp\n" +
			"[-s server mode(default)] [-c name (client mode)]\n" +
			"appid address:port\n";

		private const string InvalidFormatText =
			"invalid format\n";

		private static bool _isServer = true;
		private static IPAddress _bindAddress;
		private static IPAddress _remoteAddress;
		private static int _port;
		private static ushort _appId;
		private static string _clientName;

		private static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.Write(HelpText);
				return;
			}

			for (var i = 0; i < args.Length - 2; i++)
			{
				if (string.Equals(args[i], "-s"))
				{
					_isServer = true;
				}

				if (!string.Equals(args[i], "-c"))
				{
					continue;
				}

				_isServer = false;
				_clientName = args[i + 1];
			}

			try
			{
				_appId = ushort.Parse(args[^2]);
			}
			catch
			{
				Console.Write("appid " + InvalidFormatText);
				return;
			}

			var endpointStrings = args[^1].Split(':');
			if (endpointStrings.Length == 2)
			{
				try
				{
					if (_isServer)
					{
						_bindAddress = IPAddress.Parse(endpointStrings[0]);
					}
					else
					{
						_remoteAddress = IPAddress.Parse(endpointStrings[0]);
					}

					_port = int.Parse(endpointStrings[1]);
				}
				catch
				{
					Console.Write("address:port " + InvalidFormatText);
					return;
				}
			}

			if (_isServer)
			{
				ChatServer chatServer = new ChatServer(_appId, new IPEndPoint(_bindAddress, _port));
				chatServer.ServerMain();
			}
			else
			{
				ChatClient chatClient = new ChatClient(_clientName, _appId, new IPEndPoint(_remoteAddress, _port));
				chatClient.ClientMain();
			}
		}
	}
}

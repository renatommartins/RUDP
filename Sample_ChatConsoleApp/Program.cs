using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;

namespace Sample_ChatConsoleApp
{
	class Program
	{
		const string helpText =
			"Usage: Sample_ChatConsoleApp\n" +
			"[-s server mode(default)] [-c name (client mode)]\n" +
			"appid address:port\n";

		const string invalidFormatText =
			"invalid format\n";

		static bool _isServer = true;
		static string _host;
		static IPAddress _bindAddress;
		static IPAddress _remoteAddress;
		static int _port;
		static ushort _appId;
		static string _clientName;
		

		static void Main(string[] args)
		{
			#region args Parsing
			if (args.Length < 2)
			{
				Console.Write(helpText);
				return;
			}

			for (int i = 0; i < args.Length - 2; i++)
			{
				if (String.Equals(args[i], "-s"))
					_isServer = true;

				if (String.Equals(args[i], "-c"))
				{
					_isServer = false;
					_clientName = args[i + 1];
				}
			}

			try
			{
				_appId = ushort.Parse(args[args.Length - 2]);
			}
			catch
			{
				Console.Write("appid " + invalidFormatText);
				return;
			}

			string[] endpointStrings = args[args.Length - 1].Split(':');
			if (endpointStrings.Length == 2)
			{
				try
				{
					if (_isServer)
						_bindAddress = IPAddress.Parse(endpointStrings[0]);
					else
						_remoteAddress = IPAddress.Parse(endpointStrings[0]);

					_port = int.Parse(endpointStrings[1]);
				}
				catch
				{
					Console.Write("address:port " + invalidFormatText);
					return;
				}
			}
			#endregion

			if(_isServer)
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

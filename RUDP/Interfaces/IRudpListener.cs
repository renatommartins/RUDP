using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace RUDP.Interfaces
{
	public interface IRudpListener
	{
		bool Active { get; }
		bool ExclusiveAddressUse { get; set; }
		EndPoint LocalEndpoint { get; }
		IRudpSocket Server { get; }

		IRudpClient AcceptClient();
		void AllowNatTraversal(bool allowed);
		IRudpListener Create(int port);
		bool Pending();
		void Start();
		void Start(int backlog);
		void Stop();
	}
}

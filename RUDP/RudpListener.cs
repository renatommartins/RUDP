using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using RUDP.Interfaces;

namespace RUDP
{
	class RudpListener : IRudpListener
	{
		public bool Active => throw new NotImplementedException();

		public bool ExclusiveAddressUse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public EndPoint LocalEndpoint => throw new NotImplementedException();

		public IRudpPSocket Server => throw new NotImplementedException();

		private IRudpPSocket _socket;

		public IRudpClient AcceptClient()
		{
			throw new NotImplementedException();
		}

		public void AllowNatTraversal(bool allowed)
		{
			throw new NotImplementedException();
		}

		public IRudpListener Create(int port)
		{
			throw new NotImplementedException();
		}

		public bool Pending()
		{
			throw new NotImplementedException();
		}

		public void Start()
		{
			throw new NotImplementedException();
		}

		public void Start(int backlog)
		{
			throw new NotImplementedException();
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}

		private void ListenerThread()
		{
			throw new NotImplementedException();
		}
	}
}

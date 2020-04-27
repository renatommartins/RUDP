using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using RUDP.Interfaces;
using RUDP.Enumerations;

namespace RUDP
{
	public delegate void SendEventCallback(ushort seqNumber, RudpEvent sendEvent);

	public class RudpClient
	{
		public bool Active { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public int Available => throw new NotImplementedException();

		public IRudpSocket Client { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public bool Connected => throw new NotImplementedException();

		public bool ExclusiveAddressUse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public LingerOption LingerState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public int ReceiveBufferSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public int SendBufferSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public int UpdateRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		private IRudpSocket _socket;

		public void Close()
		{
			throw new NotImplementedException();
		}

		public void Connect(IPAddress address, int port)
		{
			throw new NotImplementedException();
		}

		public void Connect(IPAddress[] ipAddresses, int port)
		{
			throw new NotImplementedException();
		}

		public void Connect(IPEndPoint remoteEP)
		{
			throw new NotImplementedException();
		}

		public void Connect(string hostname, int port)
		{
			throw new NotImplementedException();
		}

		public int Receive(byte[] buffer)
		{
			throw new NotImplementedException();
		}

		public int Send(byte[] buffer)
		{
			throw new NotImplementedException();
		}

		public int Send(byte[] buffer, out ushort seqNumber, SendEventCallback callback)
		{
			throw new NotImplementedException();
		}

		private void ClientThread()
		{
			throw new NotImplementedException();
		}
	}
}

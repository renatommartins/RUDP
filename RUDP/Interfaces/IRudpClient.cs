using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

using RUDP.Enumerations;

namespace RUDP.Interfaces
{
	public interface IRudpClient
	{
		public delegate void SendEventCallback(ushort sequenceNumber, RudpEvent sendEvent);

		bool Active { get; set; }
		int Available { get; }
		IRudpPSocket Client { get; set; }
		bool Connected { get; }
		bool ExclusiveAddressUse { get; set; }
		LingerOption LingerState { get; set; }
		int ReceiveBufferSize { get; set; }
		int SendBufferSize { get; set; }
		int UpdateRate { get; set; }

		void Close();
		void Connect(IPAddress address, int port);
		void Connect(IPAddress[] ipAddresses, int port);
		void Connect(IPEndPoint remoteEP);
		void Connect(string hostname, int port);
		int Receive(byte[] buffer);
		int Send(byte[] buffer);
		int Send(byte[] buffer, out ushort seqNumber, SendEventCallback callback);
	}
}

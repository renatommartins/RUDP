using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace RUDP.Interfaces
{
	public interface ISocket
	{
		int Available { get; }
		void Bind(EndPoint localEp);
		void Close();
		int Receive(byte[] receiveBuffer, int offset, int length, ref EndPoint endPoint);
		int Send(byte[] sendBuffer, EndPoint endPoint);
	}
}

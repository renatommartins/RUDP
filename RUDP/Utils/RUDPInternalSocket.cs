using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using RUDP.Interfaces;

namespace RUDP.Utils
{
	internal class RudpInternalSocket : ISocket
	{
		private readonly Socket _socket;

		public RudpInternalSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
		{
			_socket = new Socket(addressFamily, socketType, protocolType);
		}

		public int Available
			=> _socket.Available;

		public void Bind(EndPoint localEp)
			=> _socket.Bind(localEp);

		public void Close()
			=> _socket.Close();

		public int Receive(byte[] receiveBuffer, int offset, int length, ref EndPoint endPoint)
			=> _socket.ReceiveFrom(receiveBuffer, offset, length, SocketFlags.None, ref endPoint);

		public int Send(byte[] sendBuffer, EndPoint endPoint)
			=> _socket.SendTo(sendBuffer, endPoint);
	}
}

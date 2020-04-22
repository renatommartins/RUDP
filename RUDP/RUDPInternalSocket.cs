using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

using RUDP.Interfaces;

namespace RUDP
{
	internal class RUDPInternalSocket : Socket, IRUDPSocket
	{
		public RUDPInternalSocket(SocketInformation socketInformation) : base(socketInformation)
		{
		}

		public RUDPInternalSocket(SocketType socketType, ProtocolType protocolType) : base(socketType, protocolType)
		{
		}

		public RUDPInternalSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) : base(addressFamily, socketType, protocolType)
		{
		}

		ISocket ISocket.Accept()
		{
			return (ISocket)Accept();
		}
	}
}

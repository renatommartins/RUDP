using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

using RUDP.Interfaces;

namespace RUDP.Utils
{
	internal class RudpInternalSocket : Socket, IRudpSocket
	{
		public RudpInternalSocket(SocketInformation socketInformation) : base(socketInformation)
		{
		}

		public RudpInternalSocket(SocketType socketType, ProtocolType protocolType) : base(socketType, protocolType)
		{
		}

		public RudpInternalSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) : base(addressFamily, socketType, protocolType)
		{
		}

		ISocket ISocket.Accept()
		{
			return (ISocket)Accept();
		}
	}
}

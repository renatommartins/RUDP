using System;
using System.Collections.Generic;
using System.Text;

namespace RUDP.Enumerations
{
	public enum PacketType : ushort
	{
		ConnectionRequest,
		ConnectionAccept,
		DisconnectionNotify,
		KeepAlive,
		Data
	}
}

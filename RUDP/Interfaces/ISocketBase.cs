using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace RUDP.Interfaces
{
	public interface ISocketBase
	{
		#region Properties
		AddressFamily AddressFamily { get; }
		int Available { get; }
		bool Blocking { get; set; }
		bool Connected { get; }
		bool DontFragment { get; set; }
		bool DualMode { get; set; }
		bool EnableBroadcast { get; set; }
		bool ExclusiveAddressUse { get; set; }
		IntPtr Handle { get; }
		bool IsBound { get; }
		LingerOption LingerState { get; set; }
		EndPoint LocalEndPoint { get; }
		bool MulticastLoopback { get; set; }
		bool NoDelay { get; set; }
		ProtocolType ProtocolType { get; }
		int ReceiveBufferSize { get; set; }
		int ReceiveTimeout { get; set; }
		EndPoint RemoteEndPoint { get; }
		int SendBufferSize { get; set; }
		int SendTimeout { get; set; }
		SocketType SocketType { get; }
		short Ttl { get; set; }
		bool UseOnlyOverlappedIO { get; set; }
		#endregion

		#region Methods
		object GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName);
		void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);
		byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength);
		int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue);
		int IOControl(IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue);
		void SetIPProtectionLevel(IPProtectionLevel level);
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue);
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue);
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue);
		void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue);
		void Shutdown(SocketShutdown how);
		#endregion
	}
}

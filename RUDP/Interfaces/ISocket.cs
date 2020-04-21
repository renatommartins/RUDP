using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace RUDP.Interfaces
{
	public interface ISocket : ISocketBase
	{
		ISocket Accept();
		void Bind(EndPoint localEP);
		void Close();
		void Close(int timeout);
		void Connect(EndPoint remoteEP);
		void Connect(IPAddress address, int port);
		void Connect(IPAddress[] addresses, int port);
		void Connect(string host, int port);
		void Disconnect(bool reuseSocket);
		SocketInformation DuplicateAndClose(int targetProcessId);
		void Listen(int backlog);
		int Receive(byte[] buffer);
		int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags);
		int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode);
		int Receive(byte[] buffer, int size, SocketFlags socketFlags);
		int Receive(byte[] buffer, SocketFlags socketFlags);
		int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP);
		int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP);
		int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP);
		int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP);
		int ReceiveMessageFrom(byte[] buffer, int offset, int size, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation);
		int Send(byte[] buffer);
		int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags);
		int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode);
		int Send(byte[] buffer, int size, SocketFlags socketFlags);
		int Send(byte[] buffer, SocketFlags socketFlags);
		void SendFile(string fileName);
		void SendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags);
		int SendTo(byte[] buffer, EndPoint remoteEP);
		int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP);
		int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP);
		int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP);
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace RUDP.Interfaces
{
	public interface ISocketAsync : ISocketBase
	{
		bool AcceptAsync(SocketAsyncEventArgs e);
		IAsyncResult BeginAccept(AsyncCallback callback, object state);
		IAsyncResult BeginAccept(int receiveSize, AsyncCallback callback, object state);
		IAsyncResult BeginAccept(Socket acceptSocket, int receiveSize, AsyncCallback callback, object state);
		IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state);
		IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object state);
		IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object state);
		IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object state);
		IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback callback, object state);
		IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state);
		IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback, object state);
		IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		IAsyncResult BeginReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback callback, object state);
		IAsyncResult BeginReceiveMessageFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback callback, object state);
		IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state);
		IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback callback, object state);
		IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback callback, object state);
		IAsyncResult BeginSendFile(string fileName, AsyncCallback callback, object state);
		IAsyncResult BeginSendFile(string fileName, byte[] preBuffer, byte[] postBuffer, TransmitFileOptions flags, AsyncCallback callback, object state);
		IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback callback, object state);
		bool ConnectAsync(SocketAsyncEventArgs e);
		bool DisconnectAsync(SocketAsyncEventArgs e);
		Socket EndAccept(out byte[] buffer, IAsyncResult asyncResult);
		Socket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult);
		Socket EndAccept(IAsyncResult asyncResult);
		void EndConnect(IAsyncResult asyncResult);
		void EndDisconnect(IAsyncResult asyncResult);
		int EndReceive(IAsyncResult asyncResult);
		int EndReceive(IAsyncResult asyncResult, out SocketError errorCode);
		int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint);
		int EndReceiveMessageFrom(IAsyncResult asyncResult, ref SocketFlags socketFlags, ref EndPoint endPoint, out IPPacketInformation ipPacketInformation);
		int EndSend(IAsyncResult asyncResult);
		int EndSend(IAsyncResult asyncResult, out SocketError errorCode);
		void EndSendFile(IAsyncResult asyncResult);
		int EndSendTo(IAsyncResult asyncResult);
		bool ReceiveFromAsync(SocketAsyncEventArgs e);
		bool ReceiveMessageFromAsync(SocketAsyncEventArgs e);
		bool SendAsync(SocketAsyncEventArgs e);
		bool SendPacketsAsync(SocketAsyncEventArgs e);
		bool SendToAsync(SocketAsyncEventArgs e);
	}
}

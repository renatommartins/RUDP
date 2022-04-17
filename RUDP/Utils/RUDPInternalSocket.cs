namespace RUDP.Utils
{
	using System.Net;
	using System.Net.Sockets;
	using RUDP.Interfaces;

	/// <summary>
	/// Default socket implementation to use.
	/// </summary>
	internal class RudpInternalSocket : ISocket
	{
		private readonly Socket _socket;

		/// <summary>
		/// Initializes a new instance of the <see cref="RudpInternalSocket"/> class.
		/// </summary>
		/// <param name="addressFamily"><see cref="AddressFamily"/> to use.</param>
		/// <param name="socketType"><see cref="SocketType"/> to use.</param>
		/// <param name="protocolType"><see cref="ProtocolType"/> to use.</param>
		public RudpInternalSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
		{
			_socket = new Socket(addressFamily, socketType, protocolType);
		}

		/// <inheritdoc cref="ISocket.Available"/>
		public int Available => _socket.Available;

		/// <inheritdoc cref="ISocket.Bind"/>
		public void Bind(EndPoint localEp) => _socket.Bind(localEp);

		/// <inheritdoc cref="ISocket.Close"/>
		public void Close() => _socket.Close();

		/// <inheritdoc cref="ISocket.Receive"/>
		public int Receive(byte[] receiveBuffer, int offset, int length, ref EndPoint endPoint)
			=> _socket.ReceiveFrom(receiveBuffer, offset, length, SocketFlags.None, ref endPoint);

		/// <inheritdoc cref="ISocket.Send"/>
		public int Send(byte[] sendBuffer, EndPoint endPoint) => _socket.SendTo(sendBuffer, endPoint);
	}
}

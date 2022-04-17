namespace RUDP.Interfaces
{
	using System.Net;

	/// <summary>
	/// Represents a communication socket.
	/// </summary>
	public interface ISocket
	{
		/// <summary>
		/// Gets number of available bytes received from remote endpoint.
		/// </summary>
		int Available { get; }

		/// <summary>
		/// Binds the local endpoint resource to this socket.
		/// </summary>
		/// <param name="localEp">describes which local endpoint to be bound.</param>
		void Bind(EndPoint localEp);

		/// <summary>
		/// Closes the socket and frees local bound resources.
		/// </summary>
		void Close();

		/// <summary>
		/// Receives the first message, if any, in the socket internal queue.
		/// </summary>
		/// <param name="receiveBuffer"><see cref="T:byte[]"/> to copy the message bytes to.</param>
		/// <param name="offset">Offset in <paramref name="receiveBuffer"/> at where to copy bytes to.</param>
		/// <param name="length">Maximum length of bytes to copy to <paramref name="receiveBuffer"/>.</param>
		/// <param name="endPoint">Instance of <see cref="EndPoint"/> in where to copy remote endpoint information for this message.</param>
		/// <returns>Message length in bytes.</returns>
		int Receive(byte[] receiveBuffer, int offset, int length, ref EndPoint endPoint);

		/// <summary>
		/// Sends a message to the remote endpoint.
		/// </summary>
		/// <param name="sendBuffer">Collection of bytes to send.</param>
		/// <param name="endPoint">Endpoint where to send the message to.</param>
		/// <returns>Length of the message sent in bytes.</returns>
		int Send(byte[] sendBuffer, EndPoint endPoint);
	}
}

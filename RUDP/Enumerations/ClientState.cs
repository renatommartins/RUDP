namespace RUDP.Enumerations
{
	/// <summary>
	/// States in which a client may be in.
	/// </summary>
	public enum ClientState
	{
		/// <summary>
		/// Not connected to a remote endpoint.
		/// </summary>
		Disconnected,

		/// <summary>
		/// Attempting connection to remote endpoint.
		/// </summary>
		Connecting,

		/// <summary>
		/// Connected to remote endpoint.
		/// </summary>
		Connected,

		/// <summary>
		/// Disconnecting from remote endpoint.
		/// </summary>
		Disconnecting,

		/// <summary>
		/// Unilaterally closed connection.
		/// </summary>
		ForceClose,
	}
}

namespace RUDP.Enumerations
{
	/// <summary>
	/// Enumeration to describe a <see cref="RUDP.Utils.Packet"/>'s purpose.
	/// </summary>
	public enum PacketType : ushort
	{
		/// <summary>
		/// Request a connection attempt.
		/// </summary>
		ConnectionRequest,

		/// <summary>
		/// Accept a connection attempt.
		/// </summary>
		ConnectionAccept,

		/// <summary>
		/// Refuse a connection attempt.
		/// </summary>
		ConnectionRefuse,

		/// <summary>
		/// Notify disconnection.
		/// </summary>
		DisconnectionNotify,

		/// <summary>
		/// No data present.
		/// </summary>
		KeepAlive,

		/// <summary>
		/// Data is present.
		/// </summary>
		Data,

		/// <summary>
		/// Values equal and above are invalid.
		/// </summary>
		Invalid,
	}
}

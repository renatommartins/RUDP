namespace RUDP.Enumerations
{
	/// <summary>
	/// <see cref="RUDP.Utils.Packet"/> delivery status.
	/// </summary>
	public enum PacketStatus
	{
		/// <summary>
		/// Successfully acknowledged by remote endpoint.
		/// </summary>
		Successful,

		/// <summary>
		/// Not acknowledged by the remote endpoint during the valid acknowledge window.
		/// </summary>
		Dropped,

		/// <summary>
		/// Not acknowledged by the remote endpoint, but is still in the valid acknowledge window.
		/// </summary>
		Pending,

		/// <summary>
		/// Still not acknowledged by the remote endpoint despite a later packet being acknowledged first.
		/// </summary>
		Delayed,
	}
}

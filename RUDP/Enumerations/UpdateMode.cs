namespace RUDP.Enumerations
{
	/// <summary>
	/// Logic execution mode.
	/// </summary>
	public enum UpdateMode
	{
		/// <summary>
		/// Uses a <see cref="System.Threading.Thread"/> to execute.
		/// </summary>
		Thread,

		/// <summary>
		/// Uses a <see cref="System.Threading.Tasks.Task"/> to execute.
		/// </summary>
		Task,

		/// <summary>
		/// Uses an external invocation to execute.
		/// </summary>
		External,
	}
}

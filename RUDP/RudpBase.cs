using System;
using RUDP.Utils;

namespace RUDP
{
	using System.Diagnostics;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;
	using RUDP.Enumerations;
	using RUDP.Interfaces;

	/// <summary>
	/// Shared Rudp logic.
	/// </summary>
	public abstract class RudpBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RudpBase"/> class.
		/// </summary>
		/// <param name="appId">Application identifier to help avoid mismatched and/or unwanted data..</param>
		/// <param name="socket"><see cref="ISocket"/> implementation instance to use.</param>
		/// <param name="localEndpoint">Local endpoint to be bound to this application.</param>
		/// <param name="updateMode">Mode of execution to use.</param>
		protected RudpBase(ushort appId, ISocket socket, EndPoint localEndpoint, UpdateMode updateMode)
		{
			AppId = appId;
			Socket = socket;
			LocalEndpoint = localEndpoint;
			UpdateMode = updateMode;
			IsActive = false;
		}

		/// <summary>
		/// Gets or sets application Identifier.
		/// </summary>
		public ushort AppId { get; protected set; }

		/// <summary>
		/// Gets or sets number of packets sent per second.
		/// </summary>
		public int SendRate { get; protected set; }

		/// <summary>
		/// Gets or sets the local endpoint.
		/// </summary>
		public EndPoint LocalEndpoint { get; protected set; }

		/// <summary>
		/// Gets or sets the bound socket.
		/// </summary>
		public ISocket Socket { get; protected set; }

		/// <summary>
		/// Gets or sets the update mode.
		/// </summary>
		public UpdateMode UpdateMode { get; protected set; }

		/// <summary>
		/// Gets or sets timer for handling timing between sent packets.
		/// </summary>
		protected Stopwatch SendStopwatch { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether keeps client mode communication thread running.
		/// </summary>
		protected bool IsActive { get; set; }

		/// <summary>
		/// Gets or sets communication handling thread.
		/// </summary>
		protected Thread UpdateThread { get; set; }

		/// <summary>
		/// Gets or sets communication handling task.
		/// </summary>
		protected Task UpdateTask { get; set; }

		/// <summary>
		/// Checks if <paramref name="packet"/> is valid.
		/// </summary>
		/// <param name="packet"><see cref="Packet"/> to be validated.</param>
		/// <param name="appId">Expected application identifier.</param>
		/// <param name="lastSequenceNum">Last sequence number sent.</param>
		/// <returns>Whether <paramref name="packet"/> is valid or not.</returns>
		internal bool ValidatePacket(Packet packet, ushort appId, ushort lastSequenceNum)
		{
			if (AppId != appId)
			{
				return false;
			}

			if (!Packet.SequenceNumberGreaterThan(packet.SequenceNumber, lastSequenceNum))
			{
				return false;
			}

			if (packet.Type >= PacketType.Invalid)
			{
				return false;
			}

			switch (packet.Type)
			{
				case PacketType.ConnectionAccept:
					if (packet.Data.Count < 2)
					{
						return false;
					}

					break;
				case PacketType.Data:
					if (packet.Data.Count == 0)
					{
						return false;
					}

					break;
				case PacketType.ConnectionRequest:
				case PacketType.ConnectionRefuse:
				case PacketType.DisconnectionNotify:
				case PacketType.KeepAlive:
					break;
				case PacketType.Invalid:
					return false;
			}

			return true;
		}
	}
}

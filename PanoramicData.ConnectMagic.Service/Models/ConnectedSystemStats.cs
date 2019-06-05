using System;

namespace PanoramicData.ConnectMagic.Service.Models
{
	public class ConnectedSystemStats
	{
		/// <summary>
		/// The last time that the system started it's sync
		/// </summary>
		public DateTimeOffset LastSyncStarted { get; set; }

		/// <summary>
		/// The last time that the system successfully completed it's sync
		/// </summary>
		public DateTimeOffset LastSyncCompleted { get; set; }
	}
}
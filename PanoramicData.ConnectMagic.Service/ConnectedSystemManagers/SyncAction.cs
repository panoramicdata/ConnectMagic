using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	/// <summary>
	/// Information required for a sync action
	/// </summary>
	[DebuggerDisplay("{" + nameof(Type) + "} {" + nameof(Permission) + "}; State: {" + nameof(StateItem) + "}; ConnectedSystem: {" + nameof(ConnectedSystemItem) + "}")]
	public class SyncAction
	{
		/// <summary>
		/// The Item from State to use when performing actions - may be null
		/// </summary>
		public JObject StateItem { get; set; }

		/// <summary>
		/// The Item from the ConnectedSystem to use when performing actions - may be null
		/// </summary>
		public JObject ConnectedSystemItem { get; set; }

		/// <summary>
		/// The type of action to perform
		/// </summary>
		public SyncActionType Type { get; set; }

		/// <summary>
		/// This is calculated during processing and added the SyncAction afterwards for information
		/// </summary>
		internal DataSetPermission Permission { get; set; }
	}
}
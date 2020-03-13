using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	/// <summary>
	/// Information required for a sync action
	/// </summary>
	public class SyncAction
	{
		/// <summary>
		/// The Item from State to use when performing actions - may be null
		/// </summary>
		public StateItem? StateItem { get; set; }

		/// <summary>
		/// The Item from the ConnectedSystem to use when performing actions - may be null
		/// </summary>
		public JObject? ConnectedSystemItem { get; set; }

		/// <summary>
		/// The type of action to perform
		/// </summary>
		public SyncActionType Type { get; set; }

		/// <summary>
		/// A comment
		/// </summary>
		public string? Comment { get; set; }

		/// <summary>
		/// This is calculated during processing and added the SyncAction afterwards for information
		/// </summary>
		public DirectionPermissions Permission { get; set; }

		/// <summary>
		/// A list of things to do
		/// </summary>
		public List<string> Functions { get; set; } = new List<string>();

		/// <summary>
		/// Description of Inward Update Mappings
		/// </summary>
		public List<Change> InwardChanges { get; set; } = new List<Change>();

		/// <summary>
		/// Description of Outward Update Mappings
		/// </summary>
		public List<Change> OutwardChanges { get; set; } = new List<Change>();

		/// <summary>
		/// Helper method for debug
		/// </summary>
		public string InwardUpdateMappingsDescription => string.Join("\n", InwardChanges);

		/// <summary>
		/// Helper method for debug
		/// </summary>
		public string OutwardUpdateMappingsDescription => string.Join("\n", OutwardChanges);

		/// <summary>
		/// Helper method for debug
		/// </summary>
		public string FunctionsDescription => string.Join("\n", Functions);

		public override string ToString()
			=> $"{Type} {Permission}; State: {StateItem}; ConnectedSystem: {ConnectedSystemItem}";
	}
}
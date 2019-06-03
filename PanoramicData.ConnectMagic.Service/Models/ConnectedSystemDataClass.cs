using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A connected system data class
	/// </summary>
	[DataContract]
	public class ConnectedSystemDataSet : DataSet
	{
		/// <summary>
		/// The expression by which the connected system is queried
		/// The language for this will vary per system
		/// </summary>
		[DataMember(Name = "QueryConfig")]
		public object QueryConfig { get; set; }

		/// <summary>
		/// The associated State dataset's name
		/// </summary>
		[DataMember(Name = "StateDataSetName")]
		public string StateDataSetName { get; set; }

		/// <summary>
		/// The system fields to fetch
		/// </summary>
		[DataMember(Name = "SystemFields")]
		public List<SystemField> SystemFields { get; set; }

		/// <summary>
		/// Creation direction
		/// - None
		///   - items are not added to either the ConnectedSystem or State
		///   - items are not removed from either the ConnectedSystem or State
		/// - In
		///   - items are added to State if they are in the ConnectedSystem
		///   - items are removed from State if they are not in the ConnectedSystem
		/// - Out
		///   - items are added the ConnectedSystem if they are in State
		///   - items are removed from the ConnectedSystem if they are not in State
		/// - Join
		///	- Not supported
		/// </summary>
		[DataMember(Name = "CreateDeleteDirection")]
		public SyncDirection CreateDeleteDirection { get; set; }

		/// <summary>
		/// The mappings
		/// </summary>
		[DataMember(Name = "Mappings")]
		public List<Mapping> Mappings { get; set; }
	}
}
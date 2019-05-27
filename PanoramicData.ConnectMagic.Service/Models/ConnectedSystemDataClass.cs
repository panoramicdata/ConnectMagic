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
		[DataMember(Name= "QueryConfig")]
		public object QueryConfig { get; set; }

		/// <summary>
		/// The associated State dataset's name
		/// </summary>
		[DataMember(Name = "StateDataSetName")]
		public string StateDataSetName { get; set; }

		[DataMember(Name = "SystemFields")]
		public List<SystemField> SystemFields { get; set; }

		[DataMember(Name = "Mappings")]
		public List<Mapping> Mappings { get; set; }
	}
}
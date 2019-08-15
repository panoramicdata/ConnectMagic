using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A QueryConfig
	/// </summary>
	[DataContract]
	public class QueryConfig
	{
		/// <summary>
		/// The type of entity being queried
		/// </summary>
		[DataMember(Name = "Type")]
		public string Type { get; set; }

		/// <summary>
		/// The query (syntax varies per ConnectedSystemType)
		/// </summary>
		[DataMember(Name = "Query")]
		public string Query { get; set; }
	}
}
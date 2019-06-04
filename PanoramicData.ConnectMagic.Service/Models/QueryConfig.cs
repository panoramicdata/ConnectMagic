using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A QueryConfig
	/// </summary>
	[DataContract]
	public class QueryConfig
	{
		[DataMember(Name = "Query")]
		public string Query { get; set; }
	}
}
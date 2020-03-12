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
		public string? Type { get; set; }

		/// <summary>
		/// The Get List query
		/// Syntax varies per ConnectedSystemType
		/// </summary>
		[DataMember(Name = "Query")]
		public string? Query { get; set; }

		/// <summary>
		/// Options string which can be used by the ConnectedSystem for extra configuration
		/// </summary>
		[DataMember(Name = "Options")]
		public string? Options { get; set; }

		/// <summary>
		/// Filter is applied post as an NCalc that must return true or false
		/// </summary>
		[DataMember(Name = "Filter")]
		public string? Filter { get; set; }

		/// <summary>
		/// The query for the Delete action.
		/// Syntax varies per ConnectedSystemType.
		/// </summary>
		[DataMember(Name = "CreateQuery")]
		public string? CreateQuery { get; set; }

		/// <summary>
		/// The query for the Delete action.
		/// Syntax varies per ConnectedSystemType.
		/// </summary>
		[DataMember(Name = "UpdateQuery")]
		public string? UpdateQuery { get; set; }

		/// <summary>
		/// The query for the Delete action.
		/// Syntax varies per ConnectedSystemType.
		/// </summary>
		[DataMember(Name = "DeleteQuery")]
		public string? DeleteQuery { get; set; }
	}
}
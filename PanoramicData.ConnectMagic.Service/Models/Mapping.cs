using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A mapping
	/// </summary>
	[DataContract]
	public class Mapping
	{
		/// <summary>
		/// Name
		/// </summary>
		[DataMember(Name = "SystemExpression")]
		public string SystemExpression { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		[DataMember(Name = "StateExpression")]
		public string StateExpression { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		[DataMember(Name = "Direction")]
		public SyncDirection Direction { get; set; }
	}
}
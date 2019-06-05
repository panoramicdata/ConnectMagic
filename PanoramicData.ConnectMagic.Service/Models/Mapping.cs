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
		/// An expression to evaluated against the source
		/// </summary>
		[DataMember(Name = "Expression")]
		public string Expression { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		[DataMember(Name = "Direction")]
		public SyncDirection Direction { get; set; }

		/// <summary>
		/// The destination field name
		/// </summary>
		[DataMember(Name = "Field")]
		public string Field { get; set; }
	}
}
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A System Field
	/// </summary>
	[DataContract]

	public class SystemField
	{
		/// <summary>
		/// Name
		/// </summary>
		[DataMember(Name = "Name")]
		public string Name { get; set; }
	}
}
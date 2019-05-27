using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// An extensible item
	/// </summary>
	[DataContract]
	public abstract class Item
	{
		/// <summary>
		/// Further information can be specified as a JObject.
		/// </summary>
		[DataMember(Name ="Extra")]
		public JObject Extra { get; set; }
	}
}
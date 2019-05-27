using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// Credentials
	/// </summary>
	[DataContract]
	[DebuggerDisplay("{PublicText}")]
	public class Credentials : Item
	{
		/// <summary>
		/// Account
		/// </summary>
		[DataMember(Name="Account")]
		public string Account { get; set; }

		/// <summary>
		/// Public credential, such as username
		/// </summary>
		[DataMember(Name = "PublicText")]
		public string PublicText { get; set; }

		/// <summary>
		/// Private credential, such as password or access key
		/// </summary>
		[DataMember(Name = "PrivateText")]
		public string PrivateText { get; set; }
	}
}
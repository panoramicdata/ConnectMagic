using Newtonsoft.Json.Linq;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// Credentials
	/// </summary>
	public class Credentials : Item
	{
		/// <summary>
		/// Account
		/// </summary>
		public string Account { get; set; }

		/// <summary>
		/// Public credential, such as username
		/// </summary>
		public string PublicText { get; set; }

		/// <summary>
		/// Private credential, such as password or access key
		/// </summary>
		public string PrivateText { get; set; }
	}
}
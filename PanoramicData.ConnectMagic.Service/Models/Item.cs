using Newtonsoft.Json.Linq;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// An extensible item
	/// </summary>
	public class Item
	{
		/// <summary>
		/// Further information can be specified as a JObject.
		/// </summary>
		public JObject Extra { get; set; }
	}
}
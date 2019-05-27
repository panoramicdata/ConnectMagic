using System.Diagnostics;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A named and described item
	/// </summary>
	[DataContract]
	[DebuggerDisplay("{Name}")]
	public abstract class NamedItem : Item
	{
		/// <summary>
		/// The name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The description
		/// </summary>
		public string Description { get; set; }
	}
}
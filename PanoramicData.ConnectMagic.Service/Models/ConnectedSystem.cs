using System.Collections.Generic;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A system
	/// </summary>
	public class ConnectedSystem : NamedItem
	{
		/// <summary>
		/// The system type
		/// </summary>
		public SystemType Type { get; set; }

		/// <summary>
		/// The system credentials
		/// </summary>
		public Credentials Credentials { get; set; }

		/// <summary>
		/// DataTypes available on the connected system
		/// </summary>
		public List<ConnectedSystemDataClass> DataClasses {get;set;}
	}
}

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A system
	/// </summary>
	[DataContract]
	[DebuggerDisplay("{Type}: {Name}")]
	public class ConnectedSystem : NamedItem
	{
		/// <summary>
		/// The system type
		/// </summary>
		[DataMember(Name = "Type")]
		public SystemType Type { get; set; }

		/// <summary>
		/// The system credentials
		/// </summary>
		[DataMember(Name = "Credentials")]
		public Credentials Credentials { get; set; }

		/// <summary>
		/// DataSets available on the connected system
		/// </summary>
		[DataMember(Name = "DataSets")]
		public List<ConnectedSystemDataSet> Datasets { get; set; }
	}
}

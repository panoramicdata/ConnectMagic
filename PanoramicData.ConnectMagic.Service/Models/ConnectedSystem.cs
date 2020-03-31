using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		public ConnectedSystem()
		{
			// Needed for JSON deserialisation
		}

		public ConnectedSystem(SystemType type, string name)
		{
			Type = type;
			Name = name;
		}

		/// <summary>
		/// The system type
		/// </summary>
		[DataMember(Name = "Type")]
		public SystemType Type { get; set; }

		/// <summary>
		/// The system credentials
		/// </summary>
		[DataMember(Name = "Credentials")]
		public Credentials Credentials { get; set; } = new Credentials();

		/// <summary>
		/// Any system-specific configuration
		/// </summary>
		[DataMember(Name = "Configuration")]
		public Dictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();

		/// <summary>
		/// DataSets available on the connected system
		/// </summary>
		[DataMember(Name = "DataSets")]
		public List<ConnectedSystemDataSet> Datasets { get; set; } = new List<ConnectedSystemDataSet>();

		/// <summary>
		/// The enabled state of the system, defaults to true
		/// </summary>
		[DataMember(Name = "IsEnabled")]
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// The desired loop periodicity, in seconds
		/// </summary>
		[DataMember(Name = "LoopPeriodicitySeconds")]
		public int LoopPeriodicitySeconds { get; set; } = 300;

		/// <summary>
		/// Permissions
		/// </summary>
		public Permissions Permissions { get; set; } = new Permissions();

		/// <summary>
		/// Enabled datasets
		/// </summary>
		public IEnumerable<ConnectedSystemDataSet> EnabledDatasets => Datasets.Where(ds => ds.IsEnabled);
	}
}

using System.Diagnostics;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A mapping
	/// </summary>
	[DataContract]
	[DebuggerDisplay("{" + nameof(Direction) + "} | System: '{" + nameof(SystemExpression) + "}' - State: '{" + nameof(StateExpression) + "}'")]
	public class Mapping
	{
		/// <summary>
		/// An expression to evaluated against the source
		/// </summary>
		[DataMember(Name = "SystemExpression")]
		public string SystemExpression { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		[DataMember(Name = "Direction")]
		public SyncDirection Direction { get; set; }

		/// <summary>
		/// The destination field name
		/// </summary>
		[DataMember(Name = "StateExpression")]
		public string StateExpression { get; set; }
	}
}
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
		/// A description
		/// </summary>
		[DataMember(Name = "Description")]
		public string Description { get; set; }

		/// <summary>
		/// An expression to determine whether this mapping should be applied.  By default, 'true'
		/// </summary>
		[DataMember(Name = "ConditionExpression")]
		public string ConditionExpression { get; set; }

		/// <summary>
		/// An expression to evaluated against the source
		/// </summary>
		[DataMember(Name = "SystemExpression")]
		public string SystemExpression { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		[DataMember(Name = "Direction")]
		public MappingType Direction { get; set; }

		/// <summary>
		/// The destination field name
		/// </summary>
		[DataMember(Name = "StateExpression")]
		public string StateExpression { get; set; }

		/// <summary>
		/// The option system function to execute if there is a difference between the evaluation of SystemExpression and the evaluation of StateExpression
		/// </summary>
		public string SystemFunction { get; set; }

		/// <summary>
		/// If present, used to target the outbound object field
		/// </summary>
		public string? SystemOutField { get; set; }

		/// <summary>
		/// Whether the mapping is enabled.
		/// </summary>
		public bool Enabled { get; set; } = true;
	}
}
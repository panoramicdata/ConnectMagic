using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Config
{
	/// <summary>
	/// System configuration
	/// </summary>
	[DataContract]
	public class Configuration
	{
		/// <summary>
		/// The configuration name
		/// </summary>
		[Required]
		[MinLength(1)]
		public string Name { get; set; }

		/// <summary>
		/// The configuration description
		/// </summary>
		[Required]
		[MinLength(1)]
		public string Description { get; set; }

		/// <summary>
		/// The configuration version - format is free but suggest either increasing version number or date/time based versioning
		/// </summary>
		public string Version { get; set; } = "v1";

		/// <summary>
		/// Systems
		/// </summary>
		[DataMember(Name = "ConnectedSystems")]
		public List<ConnectedSystem> ConnectedSystems { get; set; }

		[DataMember(Name = "State")]
		public State State { get; set; }
	}
}

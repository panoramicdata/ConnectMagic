using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;
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
		/// Systems
		/// </summary>
		[DataMember(Name = "ConnectedSystems")]
		public List<ConnectedSystem> ConnectedSystems { get; set; }

		[DataMember(Name = "State")]
		public State State { get; set; }
	}
}

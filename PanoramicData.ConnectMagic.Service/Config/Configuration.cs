using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;

namespace PanoramicData.ConnectMagic.Service.Config
{
	/// <summary>
	/// System configuration
	/// </summary>
	public class Configuration
	{
		/// <summary>
		/// Systems
		/// </summary>
		public List<ConnectedSystem> ConnectedSystems { get; set; }
	}
}

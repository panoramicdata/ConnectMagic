using System.Collections.Generic;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A connected system data class
	/// </summary>
	public class ConnectedSystemDataSet : DataSet
	{
		/// <summary>
		/// The expression by which the connected system is queried
		/// The language for this will vary per system
		/// </summary>
		public string QueryExpression { get; set; }

		/// <summary>
		/// The associated State dataset's name
		/// </summary>
		public string StateDataSetName { get; set; }
	}
}
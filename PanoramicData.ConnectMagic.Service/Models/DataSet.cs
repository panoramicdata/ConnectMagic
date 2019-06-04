using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A DataSet
	/// </summary>
	[DataContract]
	public abstract class DataSet : NamedItem
	{
		/// <summary>
		/// The fields to retrieve from the connected system
		/// </summary>
		[DataMember(Name = "Fields")]
		public List<Field> Fields { get; set; }
	}
}
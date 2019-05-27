using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	[DataContract]
	[JsonConverter(typeof(StringEnumConverter))]
	public enum SyncDirection
	{
		/// <summary>
		/// In
		/// </summary>
		[EnumMember(Value = "In")]
		In,

		/// <summary>
		/// Out
		/// </summary>
		[EnumMember(Value = "Out")]
		Out
	}
}
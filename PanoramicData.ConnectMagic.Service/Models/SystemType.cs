using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	[DataContract]
	[JsonConverter(typeof(StringEnumConverter))]
	public enum SystemType
	{
		/// <summary>
		/// Unknown
		/// </summary>
		[EnumMember(Value = "Unknown")]
		Unknown,

		/// <summary>
		/// AutoTask
		/// </summary>
		[EnumMember(Value = "AutoTask")]
		AutoTask,

		/// <summary>
		/// SalesForce
		/// </summary>
		[EnumMember(Value = "SalesForce")]
		SalesForce,

		/// <summary>
		/// Certify
		/// </summary>
		[EnumMember(Value = "Certify")]
		Certify,

		/// <summary>
		/// LogicMonitor
		/// </summary>
		[EnumMember(Value = "LogicMonitor")]
		LogicMonitor,

		/// <summary>
		/// ServiceNow
		/// </summary>
		[EnumMember(Value = "ServiceNow")]
		ServiceNow,

		/// <summary>
		/// SolarWinds
		/// </summary>
		[EnumMember(Value = "SolarWinds")]
		SolarWinds,
	}
}
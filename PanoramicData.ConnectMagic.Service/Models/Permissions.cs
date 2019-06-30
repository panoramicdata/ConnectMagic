﻿using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// Permissions
	/// </summary>
	[DataContract]
	public class Permissions
	{
		/// <summary>
		/// Whether write is permitted
		/// </summary>
		[DataMember(Name = "CanWrite")]
		public bool CanWrite { get; set; }

		/// <summary>
		/// Whether create is permitted
		/// </summary>
		[DataMember(Name = "CanCreate")]
		public bool CanCreate { get; set; }

		/// <summary>
		/// Whether update is permitted
		/// </summary>
		[DataMember(Name = "CanUpdate")]
		public bool CanUpdate { get; set; }

		/// <summary>
		/// Whether delete is permitted
		/// </summary>
		[DataMember(Name = "CanDelete")]
		public bool CanDelete { get; set; }
	}
}
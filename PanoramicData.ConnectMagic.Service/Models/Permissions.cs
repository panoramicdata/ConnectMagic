using System.Runtime.Serialization;

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
		/// Whether create inward is permitted
		/// </summary>
		[DataMember(Name = "CanCreateIn")]
		public bool CanCreateIn { get; set; }

		/// <summary>
		/// Whether update inward is permitted
		/// </summary>
		[DataMember(Name = "CanUpdateIn")]
		public bool CanUpdateIn { get; set; }

		/// <summary>
		/// Whether delete inward is permitted
		/// </summary>
		[DataMember(Name = "CanDeleteIn")]
		public bool CanDeleteIn { get; set; }

		/// <summary>
		/// Whether create outward is permitted
		/// </summary>
		[DataMember(Name = "CanCreateOut")]
		public bool CanCreateOut { get; set; }

		/// <summary>
		/// Whether update outward is permitted
		/// </summary>
		[DataMember(Name = "CanUpdateOut")]
		public bool CanUpdateOut { get; set; }

		/// <summary>
		/// Whether delete outward is permitted
		/// </summary>
		[DataMember(Name = "CanDeleteOut")]
		public bool CanDeleteOut { get; set; }
	}
}
namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// A connected system data class
	/// </summary>
	public class ConnectedSystemDataClass : NamedItem
	{
		/// <summary>
		/// The expression by which the connected system is identified
		/// </summary>
		public string IdentifiedBy { get; set; }
	}
}
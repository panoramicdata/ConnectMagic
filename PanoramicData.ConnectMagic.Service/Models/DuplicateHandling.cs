namespace PanoramicData.ConnectMagic.Service.Models
{
	public enum DuplicateHandling
	{
		/// <summary>
		/// User intervention required
		/// </summary>
		Remedy,

		/// <summary>
		/// All but the first one are discarded
		/// </summary>
		Discard,

		/// <summary>
		/// Remove duplicates in the ConnectedSystem
		/// </summary>
		RemoveFromConnectedSystem
	}
}
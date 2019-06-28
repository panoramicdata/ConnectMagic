using Newtonsoft.Json.Linq;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public class SyncAction
	{
		public JObject StateItem { get; set; }
		public JObject ConnectedSystemItem { get; set; }
		public SyncActionType Type { get; set; }
	}
}
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.ConnectedSystemManagers;
using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.Test
{
	internal class TestConnectedSystemManager : ConnectedSystemManagerBase
	{
		internal TestConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			ILogger<TestConnectedSystemManager> logger)
			: base(connectedSystem, state, logger)
		{
		}

		public List<SyncAction> TestProcessConnectedSystemItems(ConnectedSystemDataSet dataSet, List<JObject> connectedSystemItems)
			=> ProcessConnectedSystemItems(dataSet, connectedSystemItems);

		public override Task<object> QueryLookupAsync(string query, string field) => throw new System.NotImplementedException();
		public override Task RefreshDataSetsAsync(CancellationToken cancellationToken) => throw new System.NotImplementedException();
		internal override void CreateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new System.NotImplementedException();
		internal override void DeleteOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new System.NotImplementedException();
		internal override void UpdateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new System.NotImplementedException();
	}
}

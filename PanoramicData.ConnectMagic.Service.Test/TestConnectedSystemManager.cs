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
		public Dictionary<string, List<JObject>> Items { get; set; } = new Dictionary<string, List<JObject>>();

		private readonly ILogger _logger;

		internal TestConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			ILogger<TestConnectedSystemManager> logger)
			: base(connectedSystem, state, logger)
		{
			_logger = logger;
			// Initialise the list
			_logger.LogInformation("Populating DataSet1 with initial data");
			var items = new List<JObject>
			{
				new JObject(new JProperty("ConnectedSystemKey", "Key1"), new JProperty("FirstName", "Bob1"), new JProperty("LastName", "Smith1"), new JProperty("Description", "Description1")),
				new JObject(new JProperty("ConnectedSystemKey", "Key2"), new JProperty("FirstName", "Bob2"), new JProperty("LastName", "Smith2"), new JProperty("Description", "Description1")),
				new JObject(new JProperty("ConnectedSystemKey", "Key3"), new JProperty("FirstName", "Bob3"), new JProperty("LastName", "Smith3"), new JProperty("Description", "Description1")),
				new JObject(new JProperty("ConnectedSystemKey", "Key4"), new JProperty("FirstName", "Bob4"), new JProperty("LastName", "Smith4"), new JProperty("Description", "Description1")),
				new JObject(new JProperty("ConnectedSystemKey", "Key5"), new JProperty("FirstName", "Bob5"), new JProperty("LastName", "Smith5"), new JProperty("Description", "Description1"))
			};
			Items.Add("DataSet1", items);

		}

		public List<SyncAction> TestProcessConnectedSystemItems(ConnectedSystemDataSet dataSet, List<JObject> connectedSystemItems)
			=> ProcessConnectedSystemItems(dataSet, connectedSystemItems);

		public override Task<object> QueryLookupAsync(string query, string field) => throw new System.NotImplementedException();
		public override async Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			//foreach (var dataSet in ConnectedSystem.Datasets)
			//{
			//	_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
			//	_logger.LogDebug($"Got {_items.Count} results for {dataSet.Name}.");

			//	// Convert to JObjects for easier generic manipulation
			//	var connectedSystemItems = _items
			//		.Select(entity => JObject.FromObject(entity))
			//		.ToList();
			//	ProcessConnectedSystemItems(dataSet, connectedSystemItems);
			//}
			throw new System.NotImplementedException();
		}

		internal override void CreateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new System.NotImplementedException();

		internal override void DeleteOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new System.NotImplementedException();

		internal override void UpdateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new System.NotImplementedException();
	}
}

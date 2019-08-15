using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.ConnectedSystemManagers;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System;
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

		public async Task<List<SyncAction>> TestProcessConnectedSystemItemsAsync(ConnectedSystemDataSet dataSet, List<JObject> connectedSystemItems)
			=> await ProcessConnectedSystemItemsAsync(
				dataSet,
				connectedSystemItems,
				null)
				.ConfigureAwait(false);

		public override Task<object> QueryLookupAsync(QueryConfig queryConfig, string field)
			=> throw new NotSupportedException();

		public override Task RefreshDataSetsAsync(CancellationToken cancellationToken) =>
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
			throw new NotSupportedException();

		internal override Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		internal override Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		public override void Dispose()
		{
			// Nothing to be done
		}
	}
}

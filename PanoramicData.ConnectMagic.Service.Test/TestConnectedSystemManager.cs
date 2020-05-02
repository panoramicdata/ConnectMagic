using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.ConnectedSystemManagers;
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
			TimeSpan maxFileAge,
			ILogger<TestConnectedSystemManager> logger)
			: base(connectedSystem, state, maxFileAge, logger)
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

		public Task<List<SyncAction>?> TestProcessConnectedSystemItemsAsync(ConnectedSystemDataSet dataSet, List<JObject> connectedSystemItems)
			=> ProcessConnectedSystemItemsAsync(
				dataSet,
				connectedSystemItems,
				new ConnectedSystem { Name = "Fake" },
				default);

		public override Task<object?> QueryLookupAsync(
			QueryConfig queryConfig,
			string field,
			bool valueIfZeroMatchesFoundSets,
			object? valueIfZeroMatchesFound,
			bool valueIfMultipleMatchesFoundSets,
			object? valueIfMultipleMatchesFound,
			CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public override Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken) =>
			throw new NotSupportedException();

		internal override Task<JObject> CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		internal override Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, SyncAction syncAction, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public override void Dispose()
		{
			// Nothing to be done
		}

		public override Task ClearCacheAsync()
			=> throw new NotSupportedException();

		public override Task PatchAsync(string entityClass, string entityId, Dictionary<string, object> patches, CancellationToken cancellationToken)
			=> throw new NotSupportedException();
	}
}

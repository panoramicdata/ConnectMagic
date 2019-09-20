using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class SalesforceConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly ILogger _logger;
		private readonly SalesforceClient _salesforceClient;

		public SalesforceConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILogger<SalesforceConnectedSystemManager> logger)
			: base(connectedSystem, state, maxFileAge, logger)
		{
			_logger = logger;
			_salesforceClient = new SalesforceClient(
				connectedSystem.Credentials.Account,
				connectedSystem.Credentials.ClientId,
				connectedSystem.Credentials.ClientSecret,
				connectedSystem.Credentials.PublicText,
				connectedSystem.Credentials.PrivateText);
		}

		public override async Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken)
		{
			_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
			var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
			var query = new SubstitutionString(inputText);
			var substitutedQuery = query.ToString();

			var connectedSystemItems = await _salesforceClient.GetAllJObjectsAsync(substitutedQuery).ConfigureAwait(false);
			_logger.LogDebug($"Got {connectedSystemItems.Count} results for {dataSet.Name}.");

			await ProcessConnectedSystemItemsAsync(
				dataSet,
				connectedSystemItems,
				GetFileInfo(ConnectedSystem, dataSet),
				cancellationToken)
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal override Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		/// <inheritdoc />
		internal override Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		/// <inheritdoc />
		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public override async Task<object> QueryLookupAsync(QueryConfig queryConfig, string field, CancellationToken cancellationToken)
		{
			var substitutedQuery = new SubstitutionString(queryConfig.Query).ToString();
			var connectedSystemItems = await _salesforceClient
				.GetAllJObjectsAsync(substitutedQuery)
				.ConfigureAwait(false);
			_logger.LogDebug($"Got {connectedSystemItems.Count} results for query '{queryConfig.Query}'.");
			switch (connectedSystemItems.Count)
			{
				case 1:
					return connectedSystemItems[0][field];
				default:
					throw new ConfigurationException($"Lookup found {connectedSystemItems.Count} records using query '{queryConfig.Query}'.  Expected 1.");
			}
		}

		public override Task ClearCacheAsync()
			=> Task.CompletedTask;

		public override void Dispose()
			=> _salesforceClient.Dispose();
	}
}
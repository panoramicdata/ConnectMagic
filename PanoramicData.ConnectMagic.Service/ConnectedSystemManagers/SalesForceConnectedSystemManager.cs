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
			ILogger<SalesforceConnectedSystemManager> logger)
			: base(connectedSystem, state, logger)
		{
			_logger = logger;
			_salesforceClient = new SalesforceClient(
				connectedSystem.Credentials.Account,
				connectedSystem.Credentials.ClientId,
				connectedSystem.Credentials.ClientSecret,
				connectedSystem.Credentials.PublicText,
				connectedSystem.Credentials.PrivateText);
		}

		public override async Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
				var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
				var query = new SubstitutionString(inputText);
				var substitutedQuery = query.ToString();

				var connectedSystemItems = await _salesforceClient.GetAllJObjectsAsync(substitutedQuery).ConfigureAwait(false);
				_logger.LogDebug($"Got {connectedSystemItems.Count} results for {dataSet.Name}.");

				await ProcessConnectedSystemItemsAsync(dataSet, connectedSystemItems).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		internal override Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		/// <inheritdoc />
		internal override Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		/// <inheritdoc />
		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		public override async Task<object> QueryLookupAsync(string query, string field)
		{
			var substitutedQuery = new SubstitutionString(query).ToString();
			var connectedSystemItems = await _salesforceClient
				.GetAllJObjectsAsync(substitutedQuery)
				.ConfigureAwait(false);
			_logger.LogDebug($"Got {connectedSystemItems.Count} results for query '{query}'.");
			switch (connectedSystemItems.Count)
			{
				case 1:
					return connectedSystemItems[0][field];
				default:
					throw new ConfigurationException($"Lookup found {connectedSystemItems.Count} records using query '{query}'.  Expected 1.");
			}
		}

		public override void Dispose()
			=> _salesforceClient.Dispose();
	}
}
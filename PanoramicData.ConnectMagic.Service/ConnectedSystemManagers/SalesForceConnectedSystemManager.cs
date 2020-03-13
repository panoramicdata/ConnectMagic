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
		private readonly SalesforceClient _salesforceClient;

		public SalesforceConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILoggerFactory loggerFactory)
			: base(connectedSystem, state, maxFileAge, loggerFactory.CreateLogger<SalesforceConnectedSystemManager>())
		{
			_salesforceClient = new SalesforceClient(
				connectedSystem.Credentials.Account,
				connectedSystem.Credentials.ClientId,
				connectedSystem.Credentials.ClientSecret,
				connectedSystem.Credentials.PublicText,
				connectedSystem.Credentials.PrivateText);
		}

		public override async Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken)
		{
			Logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
			var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
			var query = new SubstitutionString(inputText);
			var substitutedQuery = query.ToString();

			var connectedSystemItems = await _salesforceClient.GetAllJObjectsAsync(substitutedQuery).ConfigureAwait(false);
			Logger.LogDebug($"Got {connectedSystemItems.Count} results for {dataSet.Name}.");

			await ProcessConnectedSystemItemsAsync(
				dataSet,
				connectedSystemItems,
				ConnectedSystem,
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
		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, SyncAction syncAction, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public override async Task<object?> QueryLookupAsync(
			QueryConfig queryConfig,
			string field,
			bool valueIfZeroMatchesFoundSets,
			object? valueIfZeroMatchesFound,
			bool valueIfMultipleMatchesFoundSets,
			object? valueIfMultipleMatchesFound,
			CancellationToken cancellationToken)
		{
			var substitutedQuery = new SubstitutionString(queryConfig.Query).ToString();
			var connectedSystemItems = await _salesforceClient
				.GetAllJObjectsAsync(substitutedQuery)
				.ConfigureAwait(false);
			Logger.LogDebug($"Got {connectedSystemItems.Count} results for query '{queryConfig.Query}'.");
			return connectedSystemItems.Count switch
			{
				1 => connectedSystemItems[0][field],
				_ => throw new ConfigurationException($"Lookup found {connectedSystemItems.Count} records using query '{queryConfig.Query}'.  Expected 1.")
			};
		}

		public override Task ClearCacheAsync()
			=> Task.CompletedTask;

		public override void Dispose()
			=> _salesforceClient.Dispose();
	}
}
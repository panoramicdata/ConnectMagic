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
			// Ensure we have what we need
			if (connectedSystem!.Credentials.Account == "")
			{
				throw new ConfigurationException($"ConnectedSystem '{connectedSystem!.Name}'s {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.Account)} must be set to a url when specified, otherwise omit this configuration item.");
			}
			if (string.IsNullOrWhiteSpace(connectedSystem!.Credentials.ClientId))
			{
				throw new ConfigurationException($"ConnectedSystem '{connectedSystem!.Name}'s {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.ClientId)} must be set");
			}
			if (string.IsNullOrWhiteSpace(connectedSystem!.Credentials.ClientSecret))
			{
				throw new ConfigurationException($"ConnectedSystem '{connectedSystem!.Name}'s {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.ClientSecret)} must be set");
			}
			if (string.IsNullOrWhiteSpace(connectedSystem!.Credentials.PublicText))
			{
				throw new ConfigurationException($"ConnectedSystem '{connectedSystem!.Name}'s {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.PublicText)} must be set");
			}
			if (string.IsNullOrWhiteSpace(connectedSystem!.Credentials.PrivateText))
			{
				throw new ConfigurationException($"ConnectedSystem '{connectedSystem!.Name}'s {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.PrivateText)} must be set");
			}

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
		internal override Task<JObject> CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem, CancellationToken cancellationToken)
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
			if (string.IsNullOrWhiteSpace(queryConfig.Query))
			{
				throw new ConfigurationException($"{nameof(queryConfig.Query)} must be set");
			}

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
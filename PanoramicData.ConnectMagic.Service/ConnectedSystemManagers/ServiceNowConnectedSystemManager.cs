using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using ServiceNow.Api;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class ServiceNowConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly ServiceNowClient _serviceNowClient;
		private readonly ICache<JObject> _cache;

		public ServiceNowConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILoggerFactory loggerFactory)
			: base(connectedSystem, state, maxFileAge, loggerFactory.CreateLogger<ServiceNowConnectedSystemManager>())
		{
			_serviceNowClient = new ServiceNowClient(
				connectedSystem.Credentials.Account,
				connectedSystem.Credentials.PublicText,
				connectedSystem.Credentials.PrivateText,
				loggerFactory.CreateLogger<ServiceNowClient>());
			_cache = new QueryCache<JObject>(TimeSpan.FromMinutes(1));
		}

		public override Task ClearCacheAsync()
		{
			_cache.Clear();
			return Task.CompletedTask;
		}

		public override async Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken)
		{
			Logger.LogDebug($"Refreshing DataSet {dataSet.Name}");

			var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
			var query = new SubstitutionString(inputText);
			var substitutedQuery = query.ToString();
			// Send the query off to ServiceNow
			var connectedSystemItems = await _serviceNowClient
				.GetAllByQueryAsync(dataSet.QueryConfig.Type, substitutedQuery, extraQueryString: dataSet.QueryConfig.Options)
				.ConfigureAwait(false);
			Logger.LogDebug($"Got {connectedSystemItems.Count} results for {dataSet.Name}.");

			await ProcessConnectedSystemItemsAsync(
				dataSet,
				connectedSystemItems,
				ConnectedSystem,
				cancellationToken
				).ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal override async Task<JObject> CreateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			Logger.LogDebug("Creating ServiceNow item");
			var newConnectedSystemItem = await _serviceNowClient
				.CreateAsync(dataSet.QueryConfig.Type, connectedSystemItem)
				.ConfigureAwait(false);
			Logger.LogDebug($"Created ServiceNow item with sys_id={newConnectedSystemItem["sys_id"]}");
			return newConnectedSystemItem;
		}

		/// <inheritdoc />
		internal override async Task DeleteOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			Logger.LogDebug($"Deleting item with id {connectedSystemItem["sys_id"]}");
			var sysId = connectedSystemItem["sys_id"]?.ToString();
			if (string.IsNullOrWhiteSpace(sysId))
			{
				throw new ConfigurationException($"Cannot delete ServiceNow item with sysId: '{sysId}'");
			}
			await _serviceNowClient
				.DeleteAsync(dataSet.QueryConfig.Type, sysId)
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal async override Task UpdateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			SyncAction syncAction,
			CancellationToken cancellationToken
			)
		{
			if (syncAction.ConnectedSystemItem == null)
			{
				throw new InvalidOperationException($"{nameof(syncAction.ConnectedSystemItem)} must not be null when Updating Outwards.");
			}

			if (syncAction.Functions.Count != 0)
			{
				throw new NotSupportedException("Implement functions");
			}

			// Handle simple update
			await _serviceNowClient
				.UpdateAsync(
					dataSet.QueryConfig?.Type ?? throw new ConfigurationException($"DataSet {dataSet.Name} is missing a type"),
				syncAction.ConnectedSystemItem,
				cancellationToken)
				.ConfigureAwait(false);
		}

		public override async Task<object?> QueryLookupAsync(
			QueryConfig queryConfig,
			string field,
			bool valueIfZeroMatchesFoundSets,
			object? valueIfZeroMatchesFound,
			bool valueIfMultipleMatchesFoundSets,
			object? valueIfMultipleMatchesFound,
			CancellationToken cancellationToken)
		{
			try
			{
				var cacheKey = queryConfig.Query ?? throw new ConfigurationException("Query must be provided when performing lookups.");
				Logger.LogTrace($"Performing lookup: for field {field}\n{queryConfig.Query} in for type {queryConfig.Type}");

				// Is it cached?
				JObject connectedSystemItem;
				if (_cache.TryGet(cacheKey, out var @object))
				{
					// Yes. Use that
					connectedSystemItem = @object;
				}
				else
				{
					// No.

					var serviceNowResult = (await _serviceNowClient
								.GetAllByQueryAsync(queryConfig.Type, queryConfig.Query)
								.ConfigureAwait(false))
								.ToList();

					switch (serviceNowResult.Count)
					{
						case 0:
							if (valueIfZeroMatchesFoundSets)
							{
								return valueIfZeroMatchesFound;
							}
							throw new LookupException($"Got 0 results for QueryLookup '{queryConfig.Query}' and no default value is configured.");
						case 1:
							// Convert to JObjects for easier generic manipulation
							connectedSystemItem = serviceNowResult
								.Select(entity => JObject.FromObject(entity))
								.Single();

							_cache.Store(cacheKey, connectedSystemItem);
							break;
						default:
							if (valueIfMultipleMatchesFoundSets)
							{
								return valueIfMultipleMatchesFound;
							}
							throw new LookupException($"Got {serviceNowResult.Count} results for QueryLookup '{queryConfig.Query}' and no default value is configured.");
					}

					// Convert to JObjects for easier generic manipulation
					connectedSystemItem = serviceNowResult
						.Select(entity => JObject.FromObject(entity))
						.Single();

					_cache.Store(cacheKey, connectedSystemItem);
				}

				// Determine the field value
				if (!connectedSystemItem.TryGetValue(field, out var fieldValue))
				{
					throw new ConfigurationException($"Field {field} not present for QueryLookup.");
				}
				return fieldValue;
			}
			catch (Exception e)
			{
				Logger.LogError(e, "Failed to Lookup");
				throw;
			}
		}

		public override void Dispose()
			=> _serviceNowClient?.Dispose();
	}
}
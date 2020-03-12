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
				GetFileInfo(ConnectedSystem, dataSet),
				cancellationToken
				).ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal override async Task CreateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			var _ = await _serviceNowClient
				.CreateAsync(dataSet.QueryConfig.Type, connectedSystemItem)
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal override async Task DeleteOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			Logger.LogDebug($"Deleting item with id {connectedSystemItem["sys_id"]}");
			await _serviceNowClient
				.DeleteAsync(dataSet.QueryConfig.Type, connectedSystemItem["sys_id"].ToString())
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal override Task UpdateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			SyncAction syncAction,
			CancellationToken cancellationToken
			)
			=> throw new NotSupportedException();

		public override async Task<object> QueryLookupAsync(QueryConfig queryConfig, string field, CancellationToken cancellationToken)
		{
			try
			{
				var cacheKey = queryConfig.Query;
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

					var autoTaskResult = (await _serviceNowClient
								.GetAllByQueryAsync(queryConfig.Type, queryConfig.Query)
								.ConfigureAwait(false))
								.ToList();

					if (autoTaskResult.Count != 1)
					{
						throw new LookupException($"Got {autoTaskResult.Count} results for QueryLookup '{queryConfig.Query}' in of type {queryConfig.Type}. Expected one.");
					}

					// Convert to JObjects for easier generic manipulation
					connectedSystemItem = autoTaskResult
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
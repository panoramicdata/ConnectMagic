using AutoTask.Api;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class AutoTaskConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly Client _autoTaskClient;
		private readonly ILogger _logger;
		private readonly ICache<JObject> _cache;

		public AutoTaskConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILogger<AutoTaskConnectedSystemManager> logger)
			: base(connectedSystem, state, maxFileAge, logger)
		{
			_autoTaskClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
			_logger = logger;
			_cache = new QueryCache<JObject>(TimeSpan.FromMinutes(1));
		}

		public override System.Threading.Tasks.Task ClearCacheAsync()
		{
			_cache.Clear();
			return System.Threading.Tasks.Task.CompletedTask;
		}

		public override async System.Threading.Tasks.Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken)
		{
			_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
			var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
			var query = new SubstitutionString(inputText);
			var substitutedQuery = query.ToString();
			// Send the query off to AutoTask
			try
			{
				var autoTaskResult = await _autoTaskClient
					.GetAllAsync(substitutedQuery)
					.ConfigureAwait(false);
				_logger.LogDebug($"Got {autoTaskResult.Count()} results for {dataSet.Name}.");
				// Convert to JObjects for easier generic manipulation
				var connectedSystemItems = autoTaskResult
					.Select(entity => JObject.FromObject(entity))
					.ToList();

				await ProcessConnectedSystemItemsAsync(
					dataSet,
					connectedSystemItems,
					GetFileInfo(ConnectedSystem, dataSet),
					cancellationToken
					).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				throw;
			}
		}

		/// <inheritdoc />
		internal override async System.Threading.Tasks.Task CreateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			var itemToCreate = MakeAutoTaskObject(dataSet, connectedSystemItem);
			var _ = await _autoTaskClient
				.CreateAsync(itemToCreate)
				.ConfigureAwait(false);
		}

		private Entity MakeAutoTaskObject(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
		{
			var type = Type.GetType($"AutoTask.Api.{dataSet.QueryConfig.Type}, {typeof(Entity).Assembly.FullName}");
			if (type == null)
			{
				throw new ConfigurationException($"AutoTask type {dataSet.QueryConfig.Type} not supported.");
			}
			var instance = Activator.CreateInstance(type);
			var jObjectPropertyNames = connectedSystemItem.Properties().Select(p => p.Name);

			var typePropertyInfos = type.GetProperties();
			foreach (var propertyInfo in typePropertyInfos.Where(pi => jObjectPropertyNames.Contains(pi.Name)))
			{
				propertyInfo.SetValue(instance, connectedSystemItem[propertyInfo.Name].ToObject(propertyInfo.PropertyType));
			}
			return (Entity)instance;
		}

		/// <inheritdoc />
		internal override async System.Threading.Tasks.Task DeleteOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			var entity = MakeAutoTaskObject(dataSet, connectedSystemItem);
			_logger.LogDebug($"Deleting item with id {entity.id}");
			await _autoTaskClient
				.DeleteAsync(entity)
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal override System.Threading.Tasks.Task UpdateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
			=> throw new NotSupportedException();

		public override async Task<object> QueryLookupAsync(QueryConfig queryConfig, string field, CancellationToken cancellationToken)
		{
			try
			{
				var cacheKey = queryConfig.Query;
				_logger.LogTrace($"Performing lookup: for field {field}\n{queryConfig.Query}");

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

					var autoTaskResult = (await _autoTaskClient
								.QueryAsync(queryConfig.Query)
								.ConfigureAwait(false))
								.ToList();

					if (autoTaskResult.Count != 1)
					{
						throw new LookupException($"Got {autoTaskResult.Count} results for QueryLookup '{queryConfig.Query}'. Expected one.");
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
				_logger.LogError(e, "Failed to Lookup");
				throw;
			}
		}

		public override void Dispose()
			=> _autoTaskClient?.Dispose();
	}
}
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
		private readonly ICache<JObject> _cache;

		public AutoTaskConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILoggerFactory loggerFactory)
			: base(connectedSystem, state, maxFileAge, loggerFactory.CreateLogger<AutoTaskConnectedSystemManager>())
		{
			_autoTaskClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText, loggerFactory.CreateLogger<Client>());
			_cache = new QueryCache<JObject>(TimeSpan.FromMinutes(1));
		}

		public override System.Threading.Tasks.Task ClearCacheAsync()
		{
			_cache.Clear();
			return System.Threading.Tasks.Task.CompletedTask;
		}

		public override async System.Threading.Tasks.Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken)
		{
			Logger.LogDebug($"Refreshing DataSet {dataSet.Name}");

			var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
			var query = new SubstitutionString(inputText);
			var substitutedQuery = query.ToString();
			// Send the query off to AutoTask
			var autoTaskResult = await _autoTaskClient
				.GetAllAsync(substitutedQuery)
				.ConfigureAwait(false);
			Logger.LogDebug($"Got {autoTaskResult.Count()} results for {dataSet.Name}.");
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

		/// <inheritdoc />
		internal override async System.Threading.Tasks.Task CreateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			// TODO - Handle functions
			var itemToCreate = MakeAutoTaskObject(dataSet, connectedSystemItem);
			var _ = await _autoTaskClient
				.CreateAsync(itemToCreate)
				.ConfigureAwait(false);
		}

		private Entity MakeAutoTaskObject(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
		{
			var type = Type.GetType($"AutoTask.Api.{dataSet.QueryConfig.Type}, {typeof(Entity).Assembly.FullName}")
				?? throw new ConfigurationException($"AutoTask type {dataSet.QueryConfig.Type} not supported.");

			var instance = Activator.CreateInstance(type)
				?? throw new ConfigurationException($"AutoTask type {dataSet.QueryConfig.Type} could not be created.");

			var connectedSystemItemPropertyNames = connectedSystemItem.Properties().Select(p => p.Name);

			var typePropertyInfos = type.GetProperties();
			foreach (var propertyInfo in typePropertyInfos.Where(pi => connectedSystemItemPropertyNames.Contains(pi.Name)))
			{
				propertyInfo.SetValue(instance, connectedSystemItem[propertyInfo.Name]!.ToObject(propertyInfo.PropertyType));
			}

			var entity = (Entity)instance;

			const string UserDefinedFieldPrefix = "UserDefinedFields.";

			// Set the UserDefinedFields
			foreach (var connectedSystemItemUdfName in connectedSystemItemPropertyNames.Where(n => n.StartsWith(UserDefinedFieldPrefix)))
			{
				var targetFieldName = connectedSystemItemUdfName.Substring(UserDefinedFieldPrefix.Length);

				var targetField = entity.UserDefinedFields.SingleOrDefault(udf => udf.Name == targetFieldName)
					?? throw new ConfigurationException($"Could not find UserDefinedField {targetFieldName} on Entity.");

				targetField.Value = connectedSystemItem[connectedSystemItemUdfName]!.ToString();
			}

			return entity;
		}

		/// <inheritdoc />
		internal override async System.Threading.Tasks.Task DeleteOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			var entity = MakeAutoTaskObject(dataSet, connectedSystemItem);
			Logger.LogDebug($"Deleting item with id {entity.id}");
			await _autoTaskClient
				.DeleteAsync(entity)
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal async override System.Threading.Tasks.Task UpdateOutwardsAsync(
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
				throw new NotImplementedException("Implement functions");
			}

			// Handle simple update
			var existingItem = MakeAutoTaskObject(dataSet, syncAction.ConnectedSystemItem);
			var _ = await _autoTaskClient
				.UpdateAsync(existingItem)
				.ConfigureAwait(false);
		}

		public override async Task<object> QueryLookupAsync(QueryConfig queryConfig, string field, CancellationToken cancellationToken)
		{
			try
			{
				var cacheKey = queryConfig.Query;
				Logger.LogTrace($"Performing lookup: for field {field}\n{queryConfig.Query}");

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
				Logger.LogError(e, "Failed to Lookup");
				throw;
			}
		}

		public override void Dispose()
			=> _autoTaskClient?.Dispose();
	}
}
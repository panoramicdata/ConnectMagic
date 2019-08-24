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
			ILogger<AutoTaskConnectedSystemManager> logger)
			: base(connectedSystem, state, logger)
		{
			_autoTaskClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
			_logger = logger;
			_cache = new QueryCache<JObject>(TimeSpan.FromMinutes(1));
		}

		public override async System.Threading.Tasks.Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			_cache.Clear();
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
				var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
				var query = new SubstitutionString(inputText);
				var substitutedQuery = query.ToString();
				// Send the query off to AutoTask
				var autoTaskResult = await _autoTaskClient
					.QueryAsync(substitutedQuery)
					.ConfigureAwait(false);
				_logger.LogDebug($"Got {autoTaskResult.Count()} results for {dataSet.Name}.");
				// Convert to JObjects for easier generic manipulation
				var connectedSystemItems = autoTaskResult
					.Select(entity => JObject.FromObject(entity))
					.ToList();

				await ProcessConnectedSystemItemsAsync(
					dataSet,
					connectedSystemItems,
					GetFileInfo(ConnectedSystem, dataSet)
					).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		internal override async System.Threading.Tasks.Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
		{
			switch(dataSet.QueryConfig.Type)
			{
				case nameof(ExpenseReport):
					var expensesReport = new ExpenseReport
					{
						WeekEnding = connectedSystemItem["WeekEnding"].ToString(),
						Name = connectedSystemItem["Name"].ToString(),
						SubmitterID = long.Parse(connectedSystemItem["SubmitterId"].ToString()),
					};
					var result = await _autoTaskClient
						.CreateAsync(expensesReport)
						.ConfigureAwait(false);
					break;
				default:
					throw new NotSupportedException($"AutoTask QueryConfig Type '{dataSet.QueryConfig.Type}' not supported.");
			}
		}

		/// <inheritdoc />
		internal override System.Threading.Tasks.Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		/// <inheritdoc />
		internal override System.Threading.Tasks.Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		public override async Task<object> QueryLookupAsync(QueryConfig queryConfig, string field)
		{
			try
			{
				var cacheKey = queryConfig.Query;
				_logger.LogDebug($"Performing lookup: for field {field}\n{queryConfig.Query}");

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
		{
			_autoTaskClient?.Dispose();
		}
	}
}
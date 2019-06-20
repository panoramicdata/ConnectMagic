using AutoTask.Api;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class AutoTaskConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly Client autoTaskClient;
		private readonly ILogger _logger;

		public AutoTaskConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			ILogger<AutoTaskConnectedSystemManager> logger)
			: base(connectedSystem, state, logger)
		{
			autoTaskClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
			_logger = logger;
		}

		public override async System.Threading.Tasks.Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
				var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
				var query = new SubstitutionString(inputText);
				var substitutedQuery = query.ToString();
				// Send the query off to AutoTask
				var autoTaskResult = await autoTaskClient
					.ExecuteQueryAsync(substitutedQuery)
					.ConfigureAwait(false);
				_logger.LogDebug($"Got {autoTaskResult.Count()} results for {dataSet.Name}.");
				// Convert to JObjects for easier generic manipulation
				var connectedSystemItems = autoTaskResult
					.Select(entity => JObject.FromObject(entity))
					.ToList();

				ProcessConnectedSystemItems(dataSet, connectedSystemItems);
			}
		}

		/// <inheritdoc />
		internal override void CreateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotImplementedException();

		/// <inheritdoc />
		internal override void DeleteOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotImplementedException();

		/// <inheritdoc />
		internal override void UpdateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotImplementedException();

		public override async Task<object> QueryLookupAsync(string query, string field)
		{
			try
			{
				_logger.LogDebug("Performing lookup.");
				var autoTaskResult = (await autoTaskClient
							.ExecuteQueryAsync(query)
							.ConfigureAwait(false))
							.ToList();

				if (autoTaskResult.Count != 1)
				{
					throw new ConfigurationException($"Got {autoTaskResult.Count} results for QueryLookup.  There can be only 1!");
				}

				// Convert to JObjects for easier generic manipulation
				var connectedSystemItem = autoTaskResult
					.Select(entity => JObject.FromObject(entity))
					.Single();

				if (!connectedSystemItem.TryGetValue(field, out var result))
				{
					throw new ConfigurationException($"Field {field} not present for QueryLookup.");
				}
				return result;
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Failed to Lookup");
				throw;
			}
		}
	}
}
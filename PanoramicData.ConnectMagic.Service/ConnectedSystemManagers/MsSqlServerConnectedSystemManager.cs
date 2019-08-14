using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class MsSqlServerConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly ILogger _logger;
		private readonly ConnectedSystem _connectedSystem;

		public MsSqlServerConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			ILogger<MsSqlServerConnectedSystemManager> logger)
			: base(connectedSystem, state, logger)
		{
			_logger = logger;
			_connectedSystem = connectedSystem;
		}

		public override async Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			using (var connection = new SqlConnection(_connectedSystem.Credentials.ConnectionString))
			{
				_logger.LogDebug($"Opening MS SQL connection for {_connectedSystem.Name}...");
				await connection.OpenAsync().ConfigureAwait(false);

				foreach (var dataSet in ConnectedSystem.Datasets)
				{
					_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");

					// Process any ncalc in the query
					var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
					var query = new SubstitutionString(inputText);
					var substitutedQuery = query.ToString();

					// Send the query off to MS SQL Server
					var results = (await connection.QueryAsync<object>(substitutedQuery).ConfigureAwait(false)).ToList();

					_logger.LogDebug($"Got {results.Count} results for {dataSet.Name}.");

					// Convert to JObjects for easier generic manipulation
					var connectedSystemItems = results
						.Select(entity => JObject.FromObject(entity))
						.ToList();

					await ProcessConnectedSystemItemsAsync(
						dataSet,
						connectedSystemItems,
						GetFileInfo(ConnectedSystem, dataSet))
						.ConfigureAwait(false);
				}
			}
		}

		public override Task<object> QueryLookupAsync(string query, string field)
			=> throw new NotSupportedException();

		// TODO - Use a query "Create" template with token substitution
		internal override Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		// TODO - Use a query "Delete" template with token substitution
		internal override Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		// TODO - Use a query "Update" template with token substitution
		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		public override void Dispose()
		{
			// Nothing to be done.
		}
	}
}

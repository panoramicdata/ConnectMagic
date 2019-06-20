using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
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

					ProcessConnectedSystemItems(dataSet, connectedSystemItems);
				}
			}
		}

		public override Task<object> QueryLookupAsync(string query, string field) => throw new NotImplementedException();

		internal override void CreateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new NotImplementedException();

		internal override void DeleteOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new NotImplementedException();

		internal override void UpdateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem) => throw new NotImplementedException();
	}
}

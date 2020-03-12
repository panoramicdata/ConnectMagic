using Dapper;
using Microsoft.Data.SqlClient;
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
	internal class MsSqlServerConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly ConnectedSystem _connectedSystem;

		public MsSqlServerConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILoggerFactory loggerFactory)
			: base(connectedSystem, state, maxFileAge, loggerFactory.CreateLogger<MsSqlServerConnectedSystemManager>())
		{
			_connectedSystem = connectedSystem;
		}

		public override async Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken)
		{
			using var connection = new SqlConnection(_connectedSystem.Credentials.ConnectionString);

			Logger.LogDebug($"Opening MS SQL connection for {_connectedSystem.Name}...");
			await connection.OpenAsync().ConfigureAwait(false);

			Logger.LogDebug($"Refreshing DataSet {dataSet.Name}");

			// Process any ncalc in the query
			var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
			var query = new SubstitutionString(inputText);
			var substitutedQuery = query.ToString();

			// Send the query off to MS SQL Server
			var results = (await connection.QueryAsync<object>(substitutedQuery).ConfigureAwait(false)).ToList();

			Logger.LogDebug($"Got {results.Count} results for {dataSet.Name}.");

			// Convert to JObjects for easier generic manipulation
			var connectedSystemItems = results
				.Select(entity => JObject.FromObject(entity))
				.ToList();

			await ProcessConnectedSystemItemsAsync(
				dataSet,
				connectedSystemItems,
				GetFileInfo(ConnectedSystem, dataSet),
				cancellationToken)
				.ConfigureAwait(false);
		}

		public override Task<object?> QueryLookupAsync(
			QueryConfig queryConfig,
			string field,
			bool valueIfZeroMatchesFoundSets,
			object? valueIfZeroMatchesFound,
			bool valueIfMultipleMatchesFoundSets,
			object? valueIfMultipleMatchesFound,
			CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		// TODO - Use a query "Create" template with token substitution
		internal override Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		// TODO - Use a query "Delete" template with token substitution
		internal override Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		// TODO - Use a query "Update" template with token substitution
		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, SyncAction syncAction, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		public override Task ClearCacheAsync()
			=> Task.CompletedTask;

		public override void Dispose()
		{
			// Nothing to be done.
		}
	}
}

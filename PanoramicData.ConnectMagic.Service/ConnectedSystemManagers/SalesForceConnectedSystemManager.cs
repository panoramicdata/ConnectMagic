using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class SalesForceConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly ILogger _logger;

		public SalesForceConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			ILogger<SalesForceConnectedSystemManager> logger)
			: base(connectedSystem, state, logger)
		{
			_logger = logger;
			//salesForceClient = new Client(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
		}

		public override Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
				var query = new SubstitutionString(new JObject(dataSet.QueryConfig)["Query"]?.ToString() ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'"));

				//var items = await autoTaskClient
				//	.ExecuteQueryAsync(query.ToString())
				//	.ConfigureAwait(false);
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		internal override Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new System.NotImplementedException();

		/// <inheritdoc />
		internal override Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new System.NotImplementedException();

		/// <inheritdoc />
		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new System.NotImplementedException();

		public override Task<object> QueryLookupAsync(string query, string field)
			=> throw new NotSupportedException();
	}
}
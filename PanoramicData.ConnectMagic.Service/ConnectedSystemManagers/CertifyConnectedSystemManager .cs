using Certify.Api;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class CertifyConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly CertifyClient _certifyClient;
		private readonly ILogger _logger;

		public CertifyConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			ILogger<CertifyConnectedSystemManager> logger)
			: base(connectedSystem, state, logger)
		{
			_certifyClient = new CertifyClient(connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
			_logger = logger;
		}

		public override async Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
				var query = new SubstitutionString(new JObject(dataSet.QueryConfig)["Query"]?.ToString() ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'")).ToString();

				var configItems = query.Split('/');
				var type = configItems[0];
				switch(type)
				{
					default:
						// TODO HERE!!!!
						throw new NotSupportedException();
				}

				var index = uint.Parse(query);
				var id = Guid.Empty;

				var items = await _certifyClient.ExpenseReportGlds
					.GetAsync(index, id)
					.ConfigureAwait(false);

				// TODO populate the list from the source system
				var connectedSystemItems = new List<JObject>();
				ProcessConnectedSystemItems(dataSet, connectedSystemItems);
			}
		}

		/// <inheritdoc />
		internal override void CreateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> _logger.LogInformation("Create entry in Certify");

		/// <inheritdoc />
		internal override void UpdateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> _logger.LogInformation("Update entry in Certify");

		/// <inheritdoc />
		internal override void DeleteOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> _logger.LogInformation($"Delete entry in Certify");

		public override Task<object> QueryLookupAsync(string query, string field)
			=> throw new NotSupportedException();
	}
}
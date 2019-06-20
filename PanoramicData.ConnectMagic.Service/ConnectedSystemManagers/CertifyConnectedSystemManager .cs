using Certify.Api;
using Certify.Api.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
				List<JObject> connectedSystemItems;
				_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
				var query = new SubstitutionString(dataSet.QueryConfig.Query).ToString();

				var configItems = query.Split('|');
				var type = configItems[0];
				try
				{
					switch (type)
					{
						case "exprptglds":
							var exprptgldsConfig = new ExprptgldsConfig(configItems.Skip(1).ToList());
							// We have the index
							ExpenseReportGldPage expenseReportGldPage = await _certifyClient
								.ExpenseReportGlds
								.GetPageAsync(exprptgldsConfig.Index, active: 1)
								.ConfigureAwait(false);

							connectedSystemItems = expenseReportGldPage
								.ExpenseReportGlds
								.Select(entity => JObject.FromObject(entity))
								.ToList();
							break;
						default:
							throw new NotSupportedException();
					}
				}
				catch (Exception e)
				{
					_logger.LogError($"Could not fetch {type} due to {e.Message}");
					throw;
				}

				ProcessConnectedSystemItems(dataSet, connectedSystemItems);
			}
		}

		/// <inheritdoc />
		internal override void CreateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotImplementedException();

		/// <inheritdoc />
		internal override void UpdateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotImplementedException();

		/// <inheritdoc />
		internal override void DeleteOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotImplementedException();

		public override Task<object> QueryLookupAsync(string query, string field)
			=> throw new NotSupportedException();
	}
}
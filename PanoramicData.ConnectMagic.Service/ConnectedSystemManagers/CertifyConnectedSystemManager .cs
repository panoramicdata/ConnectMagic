using Certify.Api;
using Certify.Api.Extensions;
using Certify.Api.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("PanoramicData.ConnectMagic.Service.Test")]
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
							var expenseReportGlds = await _certifyClient
								.ExpenseReportGlds
								.GetAllAsync(exprptgldsConfig.Index, active: 1)
								.ConfigureAwait(false);

							connectedSystemItems = expenseReportGlds
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

				var syncActions = ProcessConnectedSystemItemsAsync(dataSet, connectedSystemItems);
			}
		}

		/// <inheritdoc />
		internal override async Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
		{
			// Split out the parameters in the query
			var parameters = dataSet.QueryConfig.Query.Split('|');
			switch (parameters[0])
			{
				case "exprptglds":
					if (!uint.TryParse(parameters[1], out var index))
					{
						throw new ConfigurationException($"Certify index {parameters[1]} could not be parsed as a UINT.");
					}

					var created = await _certifyClient
						.ExpenseReportGlds
						.CreateAsync(index, new ExpenseReportGld
						{
							Name = connectedSystemItem.Value<string>(nameof(ExpenseReportGld.Name)),
							Code = connectedSystemItem.Value<string>(nameof(ExpenseReportGld.Code)),
							Description = connectedSystemItem.Value<string>(nameof(ExpenseReportGld.Description)),
							Active = 1
						})
						.ConfigureAwait(false);
					break;
				default:
					throw new NotSupportedException($"Certify class {parameters[0]} not supported.");
			}
		}

		/// <inheritdoc />
		internal override async Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
		{
			var parameters = dataSet.QueryConfig.Query.Split('|');
			switch (parameters[0])
			{
				case "exprptglds":
					if (!uint.TryParse(parameters[1], out var index))
					{
						throw new ConfigurationException($"Certify index {parameters[1]} could not be parsed as a UINT.");
					}

					// Get the existing entry
					var id = new Guid(connectedSystemItem.Value<string>(nameof(ExpenseReportGld.Id)));
					var existingPage = await _certifyClient
						.ExpenseReportGlds
						.GetAsync(index, id)
						.ConfigureAwait(false);
					var existing = existingPage.ExpenseReportGlds.SingleOrDefault();
					if (existing == null)
					{
						throw new ConfigurationException($"Couldn't find Certify exprptglds entry with id {id}.");
					}

					SetPropertiesFromJObject(existing, connectedSystemItem);
					// Loop over the connectedSystemItem properties
					// Find each property on the target class and set the value if the property was found otherwise throw an exception
					// Update Certify

					var updated = await _certifyClient
						.ExpenseReportGlds
						.UpdateAsync(index, existing)
						.ConfigureAwait(false);

					break;
				default:
					throw new NotSupportedException($"Certify class {parameters[0]} not supported.");
			}
		}

		/// <inheritdoc />
		internal override async Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
		{ // Split out the parameters in the query
			var parameters = dataSet.QueryConfig.Query.Split('|');
			switch (parameters[0])
			{
				case "exprptglds":
					if (!uint.TryParse(parameters[1], out var index))
					{
						throw new ConfigurationException($"Certify index {parameters[1]} could not be parsed as a UINT.");
					}

					var updated = await _certifyClient
						.ExpenseReportGlds
						.UpdateAsync(index, new ExpenseReportGld
						{
							Id = new Guid(connectedSystemItem.Value<string>(nameof(ExpenseReportGld.Id))),
							Active = 0
						})
						.ConfigureAwait(false);
					break;
				default:
					throw new NotSupportedException($"Certify class {parameters[0]} not supported.");
			}
		}

		public override Task<object> QueryLookupAsync(string query, string field)
			=> throw new NotSupportedException();
	}
}
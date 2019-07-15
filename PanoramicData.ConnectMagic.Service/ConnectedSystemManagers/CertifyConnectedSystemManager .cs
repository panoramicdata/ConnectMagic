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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("PanoramicData.ConnectMagic.Service.Test")]
namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class CertifyConnectedSystemManager : ConnectedSystemManagerBase
	{
		private PropertyInfo[] ExpensePropertyInfos = typeof(Expense).GetProperties();

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
					var configItemsExceptFirst = configItems
						.Skip(1)
						.ToList();
					switch (type)
					{
						case "exprptglds":
							var exprptgldsConfig = new ExprptgldsConfig(configItemsExceptFirst);
							// We have the index
							var expenseReportGlds = await _certifyClient
								.ExpenseReportGlds
								.GetAllAsync(exprptgldsConfig.Index, active: 1)
								.ConfigureAwait(false);

							connectedSystemItems = expenseReportGlds
								.Select(entity => JObject.FromObject(entity))
								.ToList();
							break;
						case "expenses":
							var propertyFilters = new List<Filter>();

							string startDate = null;
							string endDate = null;
							string batchId = null;
							uint? processed = null;
							uint? includeDisapproved = null;
							var allFilters = configItemsExceptFirst.Select(ci => new Filter(ci));
							foreach (var filter in allFilters)
							{
								var name = filter.Name;
								var value = filter.Value;
								var isProperty = false;
								switch (name)
								{
									case "startDate":
										startDate = value;
										break;
									case "endDate":
										endDate = value;
										break;
									case "batchId":
										batchId = value;
										break;
									case "processed":
										processed = GetBoolUint(name, value);
										break;
									case "includeDisapproved":
										includeDisapproved = GetBoolUint(name, value);
										break;
									default:
										// Perhaps we will filter on this later?
										propertyFilters.Add(filter);
										isProperty = true;
										break;
								}
								if (!isProperty && filter.Operator != Operator.Equals)
								{
									throw new ConfigurationException($"Expense configItem {filter.Name} must in the form 'a==b'");
								}
							}

							// Fetch using the query filters
							var expenses = await _certifyClient
								.Expenses
								.GetAllAsync(
									startDate,
									endDate,
									batchId,
									processed,
									includeDisapproved
									)
								.ConfigureAwait(false) as IQueryable<Expense>;

							// Apply property filters
							foreach (var propertyFilter in propertyFilters)
							{
								// Does this refer to a valid property?
								var matchingPropertyInfo = ExpensePropertyInfos.SingleOrDefault(pi => string.Equals(pi.Name, propertyFilter.Name, StringComparison.InvariantCultureIgnoreCase));
								if (matchingPropertyInfo == null)
								{
									// No
									throw new ConfigurationException($"Expenses do not have property '{propertyFilter.Name}'");
								}
								// Yes

								// Filter on this criteria
								switch (propertyFilter.Operator)
								{
									case Operator.Equals:
										expenses = expenses.Where(e => matchingPropertyInfo.GetValue(e).ToString() == propertyFilter.Value);
										break;
									case Operator.NotEquals:
										expenses = expenses.Where(e => matchingPropertyInfo.GetValue(e).ToString() != propertyFilter.Value);
										break;
									case Operator.LessThanOrEquals:
										expenses = expenses.Where(e => string.Compare(matchingPropertyInfo.GetValue(e).ToString(), propertyFilter.Value) <= 0);
										break;
									case Operator.LessThan:
										expenses = expenses.Where(e => string.Compare(matchingPropertyInfo.GetValue(e).ToString(), propertyFilter.Value) < 0);
										break;
									case Operator.GreaterThanOrEquals:
										expenses = expenses.Where(e => string.Compare(matchingPropertyInfo.GetValue(e).ToString(), propertyFilter.Value) >= 0);
										break;
									case Operator.GreaterThan:
										expenses = expenses.Where(e => string.Compare(matchingPropertyInfo.GetValue(e).ToString(), propertyFilter.Value) > 0);
										break;
									default:
										throw new NotSupportedException($"Operator '{propertyFilter.Operator}' not supported.");
								}
							}

							connectedSystemItems = expenses
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

				await ProcessConnectedSystemItemsAsync(
					dataSet,
					connectedSystemItems,
					GetFileInfo(ConnectedSystem, dataSet))
					.ConfigureAwait(false);
			}
		}

		private uint? GetBoolUint(string configItemName, string value)
		{
			switch (value)
			{
				case "0":
					return 0;
				case "1":
					return 1;
				default:
					throw new ConfigurationException($"Expense configItem value {configItemName} should be set to '0' or '1'");
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
					try
					{
						var expenseReportGld = new ExpenseReportGld
						{
							Name = connectedSystemItem.Value<string>(nameof(ExpenseReportGld.Name)),
							Code = connectedSystemItem.Value<string>(nameof(ExpenseReportGld.Code)),
							Description = connectedSystemItem.Value<string>(nameof(ExpenseReportGld.Description)),
							Active = 1
						};

						_logger.LogInformation($"Creating new entry \"{expenseReportGld.Name}\" in Certify");

						var created = await _certifyClient
							.ExpenseReportGlds
							.CreateAsync(index, expenseReportGld)
							.ConfigureAwait(false);
					}
					catch (Refit.ApiException ex)
					{
						_logger.LogError(ex, ex.Message);
						throw;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, ex.Message);
						throw;
					}
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
					var id = connectedSystemItem.Value<Guid>("ID");
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

					_logger.LogInformation($"Updating entry {existing.Name} in Certify");

					_ = await _certifyClient
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

					var expenseReportGld = new ExpenseReportGld
					{
						Id = connectedSystemItem.Value<Guid>("ID"),
						Active = 0
					};

					_logger.LogInformation($"Deleting entry {expenseReportGld.Id} in Certify");

					_ = await _certifyClient
						.ExpenseReportGlds
						.UpdateAsync(index, expenseReportGld)
						.ConfigureAwait(false);
					break;
				default:
					throw new NotSupportedException($"Certify class {parameters[0]} not supported.");
			}
		}

		public override Task<object> QueryLookupAsync(string query, string field)
			=> throw new NotSupportedException();

		public override void Dispose()
		{
			// Nothing to be done.
		}
	}
}
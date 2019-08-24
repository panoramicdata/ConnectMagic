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
		private readonly PropertyInfo[] ExpensePropertyInfos = typeof(Expense).GetProperties();

		private readonly CertifyClient _certifyClient;
		private readonly ILogger _logger;

		public CertifyConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILogger<CertifyConnectedSystemManager> logger)
			: base(connectedSystem, state, maxFileAge, logger)
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
				var type = dataSet.QueryConfig.Type;
				var query = new SubstitutionString(dataSet.QueryConfig.Query).ToString();

				var configItems = query.Split('|');
				try
				{
					var configItemsExceptFirst = configItems
						.ToList();
					switch (type.ToLowerInvariant())
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
						case "expensereports":
							var expenseReports = await _certifyClient
								.ExpenseReports
								.GetAllAsync()
								.ConfigureAwait(false);

							connectedSystemItems = expenseReports
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
								switch (name.ToLowerInvariant())
								{
									case "startdate":
										startDate = value;
										break;
									case "enddate":
										endDate = value;
										break;
									case "batchid":
										batchId = value;
										break;
									case "processed":
										processed = GetBoolUint(name, value);
										break;
									case "includedisapproved":
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
							var expenses = (await _certifyClient
								.Expenses
								.GetAllAsync(
									startDate,
									endDate,
									batchId,
									processed,
									includeDisapproved
									)
								.ConfigureAwait(false))
								.AsQueryable();

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

							var expensesList = expenses.ToList();

							// Only one currency supported.
							var badCurrencies = expensesList
								.Where(e => e.Currency != "GBP")
								.Select(e => e.Currency)
								.Distinct()
								.ToList();
							if (badCurrencies.Count != 0)
							{
								throw new NotSupportedException($"Non-GBP currency type(s): {string.Join(";", badCurrencies)} not supported.");
							}

							connectedSystemItems = expensesList
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
					GetFileInfo(ConnectedSystem, dataSet),
					cancellationToken)
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
		internal override async Task CreateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
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
		internal override async Task UpdateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			var type = dataSet.QueryConfig.Type;
			var parameters = dataSet.QueryConfig.Query.Split('|');
			switch (type)
			{
				case "exprptglds":
					if (!uint.TryParse(parameters[0], out var index))
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
		internal override async Task DeleteOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			var type = dataSet.QueryConfig.Type;
			var parameters = dataSet.QueryConfig.Query.Split('|');
			switch (type)
			{
				case "exprptglds":
					if (!uint.TryParse(parameters[0], out var index))
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

		public override async Task<object> QueryLookupAsync(
			QueryConfig queryConfig,
			string field,
			CancellationToken cancellationToken)
		{
			var type = queryConfig.Type.ToLowerInvariant();
			var parameters = queryConfig.Query.Split('|');
			switch (type)
			{
				case "user":
					{
						var criterion = parameters[0];
						var criterionParameters = criterion.Split("==");
						if (criterionParameters.Length != 2 || criterionParameters[0] != "EmployeeID")
						{
							throw new NotSupportedException("Only EmployeeID Certify user parameter currently supported.");
						}
						if (!int.TryParse(criterionParameters[1], out var employeeId))
						{
							throw new ConfigurationException($"EmployeeID Certify user parameter '{criterionParameters[0]}' is not an integer.");
						}
						// It's a valid integer

						var user = (await _certifyClient
							.Users
							.GetAllAsync()
							.ConfigureAwait(false))
							.SingleOrDefault(u => u.EmployeeId == employeeId.ToString());
						if (user == default)
						{
							throw new Exception($"Certify user with EmployeeID={employeeId} not found.");
						}
						// We have the user
						var propertyInfos = typeof(User).GetProperties();
						var propertyInfo = propertyInfos.SingleOrDefault(pi => string.Equals(pi.Name, field, StringComparison.InvariantCultureIgnoreCase));
						if (propertyInfo == default)
						{
							throw new ConfigurationException($"Certify users don't have a property '{field}'.");
						}
						// We have the PropertyInfo
						return propertyInfo.GetValue(user);
					}
				case "expensereport":
					{
						var criterion = parameters[0];
						var criterionParameters = criterion.Split("==");
						if (criterionParameters.Length != 2 || criterionParameters[0] != "ID")
						{
							throw new NotSupportedException("Only ID Certify ExpenseReport parameter currently supported.");
						}
						if (!Guid.TryParse(criterionParameters[1], out var expenseReportId))
						{
							throw new ConfigurationException($"ID Certify ExpenseReport parameter '{criterionParameters[0]}' is not a Guid.");
						}
						// It's a valid Guid

						var expenseReport = (await _certifyClient
							.ExpenseReports.GetAsync(expenseReportId, null)
							.ConfigureAwait(false))
							.ExpenseReports
							.SingleOrDefault();
						if (expenseReport == default)
						{
							throw new Exception($"Certify ExpenseReport with ID={expenseReportId} not found.");
						}
						// We have the ExpenseReport
						var propertyInfos = typeof(ExpenseReport).GetProperties();
						var propertyInfo = propertyInfos.SingleOrDefault(pi => string.Equals(pi.Name, field, StringComparison.InvariantCultureIgnoreCase));
						if (propertyInfo == default)
						{
							throw new ConfigurationException($"Certify ExpenseReports don't have a property '{field}'.");
						}
						// We have the PropertyInfo
						return propertyInfo.GetValue(expenseReport);
					}
				default:
					throw new NotSupportedException($"Query of type '{type}' not supported.");
			}
		}

		public override void Dispose()
		{
			// Nothing to be done.
		}
	}
}
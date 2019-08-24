using LogicMonitor.Api;
using LogicMonitor.Api.Dashboards;
using LogicMonitor.Api.Devices;
using LogicMonitor.Api.Reports;
using LogicMonitor.Api.Users;
using LogicMonitor.Api.Websites;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class LogicMonitorConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly PortalClient _logicMonitorClient;
		private readonly ILogger _logger;
		private readonly ICache<JObject> _cache;

		public LogicMonitorConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILogger<LogicMonitorConnectedSystemManager> logger)
			: base(connectedSystem, state, maxFileAge, logger)
		{
			_logicMonitorClient = new PortalClient(connectedSystem.Credentials.Account, connectedSystem.Credentials.PublicText, connectedSystem.Credentials.PrivateText);
			_logger = logger;
			_cache = new QueryCache<JObject>(TimeSpan.FromMinutes(1));
		}

		public override async Task RefreshDataSetsAsync(CancellationToken cancellationToken)
		{
			_cache.Clear();
			foreach (var dataSet in ConnectedSystem.Datasets)
			{
				_logger.LogDebug($"Refreshing DataSet {dataSet.Name}");
				var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
				var query = new SubstitutionString(inputText);
				var substitutedQuery = new QueryConfig
				{
					Type = dataSet.QueryConfig.Type,
					Query = query.ToString()
				};
				// Send the query off to LogicMonitor
				var logicMonitorResult = await GetAllAsync(substitutedQuery)
					.ConfigureAwait(false);
				_logger.LogDebug($"Got {logicMonitorResult.Count} results for {dataSet.Name}.");
				// Convert to JObjects for easier generic manipulation
				var connectedSystemItems = logicMonitorResult
					.Select(entity => JObject.FromObject(entity))
					.ToList();

				await ProcessConnectedSystemItemsAsync(
					dataSet,
					connectedSystemItems,
					GetFileInfo(ConnectedSystem, dataSet)
					).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		internal override async Task CreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
		{
			switch (dataSet.QueryConfig.Type)
			{
				case nameof(Dashboard):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<DashboardCreationDto>())
						.ConfigureAwait(false);
					break;
				case nameof(DashboardGroup):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<DashboardGroupCreationDto>())
						.ConfigureAwait(false);
					break;
				case nameof(Device):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<DeviceCreationDto>())
						.ConfigureAwait(false);
					break;
				case nameof(DeviceGroup):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<DeviceGroupCreationDto>())
						.ConfigureAwait(false);
					break;
				case nameof(ReportGroup):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<ReportGroupCreationDto>())
						.ConfigureAwait(false);
					break;
				case nameof(User):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<UserCreationDto>())
						.ConfigureAwait(false);
					break;
				case nameof(Role):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<RoleCreationDto>())
						.ConfigureAwait(false);
					break;
				case nameof(Website):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<WebsiteCreationDto>())
						.ConfigureAwait(false);
					break;
				case nameof(WebsiteGroup):
					await _logicMonitorClient
						.CreateAsync(connectedSystemItem.ToObject<WebsiteGroupCreationDto>())
						.ConfigureAwait(false);
					break;
				default:
					throw new NotSupportedException($"LogicMonitor QueryConfig Type '{dataSet.QueryConfig.Type}' not supported.");
			}
		}

		/// <inheritdoc />
		internal override async Task DeleteOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
		{
			var id = connectedSystemItem["id"].ToObject<int>();
			switch (dataSet.QueryConfig.Type)
			{
				case nameof(Dashboard):
					await _logicMonitorClient
						.DeleteAsync<Dashboard>(id)
						.ConfigureAwait(false);
					break;
				case nameof(DashboardGroup):
					await _logicMonitorClient
						.DeleteAsync<DashboardGroup>(id)
						.ConfigureAwait(false);
					break;
				case nameof(Device):
					await _logicMonitorClient
						.DeleteAsync<Device>(id)
						.ConfigureAwait(false);
					break;
				case nameof(DeviceGroup):
					await _logicMonitorClient
						.DeleteAsync<DeviceGroup>(id)
						.ConfigureAwait(false);
					break;
				case nameof(ReportGroup):
					await _logicMonitorClient
						.DeleteAsync<ReportGroup>(id)
						.ConfigureAwait(false);
					break;
				case nameof(User):
					await _logicMonitorClient
						.DeleteAsync<User>(id)
						.ConfigureAwait(false);
					break;
				case nameof(Role):
					await _logicMonitorClient
						.DeleteAsync<Role>(id)
						.ConfigureAwait(false);
					break;
				case nameof(Website):
					await _logicMonitorClient
						.DeleteAsync<Website>(id)
						.ConfigureAwait(false);
					break;
				case nameof(WebsiteGroup):
					await _logicMonitorClient
						.DeleteAsync<WebsiteGroup>(id)
						.ConfigureAwait(false);
					break;
				default:
					throw new NotSupportedException($"LogicMonitor QueryConfig Type '{dataSet.QueryConfig.Type}' not supported.");
			}
		}

		/// <summary>
		/// Strategy: create a Patch containing all of the fields in the connectedSystemItem
		/// </summary>
		/// <param name="dataSet"></param>
		/// <param name="connectedSystemItem"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		internal override Task UpdateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject connectedSystemItem)
			=> throw new NotSupportedException();

		public override async Task<object> QueryLookupAsync(QueryConfig queryConfig, string field)
		{
			try
			{
				var cacheKey = queryConfig.Query;
				_logger.LogDebug($"Performing lookup: for field {field}\n{queryConfig.Query}");

				// Is it cached?
				JObject connectedSystemItem;
				if (_cache.TryGet(cacheKey, out var @object))
				{
					// Yes. Use that
					connectedSystemItem = @object;
				}
				else
				{
					// No.

					List<object> results = await GetAllAsync(queryConfig).ConfigureAwait(false);

					if (results.Count != 1)
					{
						throw new LookupException($"Got {results.Count} results for QueryLookup '{queryConfig.Query}'. Expected one.");
					}

					// Convert to JObjects for easier generic manipulation
					connectedSystemItem = results
						.Select(entity => JObject.FromObject(entity))
						.Single();

					_cache.Store(cacheKey, connectedSystemItem);
				}

				// Determine the field value
				if (!connectedSystemItem.TryGetValue(field, out var fieldValue))
				{
					throw new ConfigurationException($"Field {field} not present for QueryLookup.");
				}
				return fieldValue;
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Failed to Lookup");
				throw;
			}
		}

		private async Task<List<object>> GetAllAsync(QueryConfig queryConfig)
		{
			List<object> results;
			switch (queryConfig.Type)
			{
				case nameof(Dashboard):
					results = (await _logicMonitorClient
						.GetAllAsync<Dashboard>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				case nameof(DashboardGroup):
					results = (await _logicMonitorClient
						.GetAllAsync<DashboardGroup>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				case nameof(Device):
					results = (await _logicMonitorClient
						.GetAllAsync<Device>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				case nameof(DeviceGroup):
					results = (await _logicMonitorClient
						.GetAllAsync<DeviceGroup>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				case nameof(ReportGroup):
					results = (await _logicMonitorClient
						.GetAllAsync<ReportGroup>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				case nameof(User):
					results = (await _logicMonitorClient
						.GetAllAsync<User>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				case nameof(Role):
					results = (await _logicMonitorClient
						.GetAllAsync<Role>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				case nameof(Website):
					results = (await _logicMonitorClient
						.GetAllAsync<Website>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				case nameof(WebsiteGroup):
					results = (await _logicMonitorClient
						.GetAllAsync<WebsiteGroup>(queryConfig.Query)
						.ConfigureAwait(false))
						.Cast<object>()
						.ToList();
					break;
				default:
					throw new NotSupportedException($"LogicMonitor QueryConfig Type '{queryConfig.Type}' not supported.");
			}

			return results;
		}

		public override void Dispose()
			=> _logicMonitorClient?.Dispose();
	}
}
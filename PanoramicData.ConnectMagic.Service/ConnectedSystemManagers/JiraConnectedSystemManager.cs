using Atlassian.Jira;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class JiraConnectedSystemManager : ConnectedSystemManagerBase
	{
		private readonly Jira _jiraClient;

		public JiraConnectedSystemManager(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILoggerFactory loggerFactory)
			: base(connectedSystem, state, maxFileAge, loggerFactory.CreateLogger<ServiceNowConnectedSystemManager>())
		{
			_jiraClient = Jira.CreateRestClient(
				connectedSystem.Credentials.Account,
				connectedSystem.Credentials.PublicText,
				connectedSystem.Credentials.PrivateText);
		}

		public override async Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken)
		{
			Logger.LogDebug($"Refreshing Jira DataSet {dataSet.Name}");

			var inputText = dataSet.QueryConfig.Query ?? throw new ConfigurationException($"Missing Query in QueryConfig for dataSet '{dataSet.Name}'");
			var query = new SubstitutionString(inputText);
			var substitutedQuery = query.ToString();

			List<JObject> connectedSystemItems;
			switch (dataSet.QueryConfig.Type)
			{
				case "Issue":
					var issues = await _jiraClient
						.Issues
						.GetIssuesFromJqlAsync(dataSet.QueryConfig.Query)
						.ConfigureAwait(false);

					connectedSystemItems = issues
						.Select(issue => JObject.FromObject(issue))
						.ToList();
					break;
				default:
					throw new NotSupportedException($"Jira type '{dataSet.QueryConfig.Type}' not supported.");
			}
			Logger.LogDebug($"Got {connectedSystemItems.Count} results for Jira dataset {dataSet.Name}.");

			await ProcessConnectedSystemItemsAsync(
				dataSet,
				connectedSystemItems,
				ConnectedSystem,
				cancellationToken
				).ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal override async Task<JObject> CreateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			Logger.LogDebug($"Creating Jira {dataSet.QueryConfig.Type}");

			var newIssueId = await _jiraClient
				.Issues
				.CreateIssueAsync(connectedSystemItem.ToObject<Issue>())
				.ConfigureAwait(false);

			Logger.LogDebug($"Created Jira {dataSet.QueryConfig.Type} with id={newIssueId}");
			return JObject.FromObject(new { id = newIssueId });
		}

		/// <inheritdoc />
		internal override async Task DeleteOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			)
		{
			Logger.LogDebug($"Creating Jira {dataSet.QueryConfig.Type} with id {connectedSystemItem["id"]}");
			var id = connectedSystemItem["id"]?.ToString();
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ConfigurationException($"Cannot delete ServiceNow item with sysId: '{id}'");
			}
			await _jiraClient
				.Issues.DeleteIssueAsync(id)
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		internal async override Task UpdateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			SyncAction syncAction,
			CancellationToken cancellationToken
			)
		{
			if (syncAction.ConnectedSystemItem == null)
			{
				throw new InvalidOperationException($"{nameof(syncAction.ConnectedSystemItem)} must not be null when Updating Outwards.");
			}

			throw new NotImplementedException();
			// Handle simple update
			//await _jiraClient
			//	.Issues.UpdateIssueAsync(
			//		new Issue() { Key = syncAction["id"].ToString()},
			//	cancellationToken)
			//	.ConfigureAwait(false);
		}

		public override async Task<object?> QueryLookupAsync(
			QueryConfig queryConfig,
			string field,
			bool valueIfZeroMatchesFoundSets,
			object? valueIfZeroMatchesFound,
			bool valueIfMultipleMatchesFoundSets,
			object? valueIfMultipleMatchesFound,
			CancellationToken cancellationToken)
		{
			//try
			//{
			//	var cacheKey = queryConfig.Query ?? throw new ConfigurationException("Query must be provided when performing lookups.");
			//	Logger.LogTrace($"Performing lookup: for field {field}\n{queryConfig.Query} in for type {queryConfig.Type}");

			//	// Is it cached?
			//	JObject connectedSystemItem;
			//	if (_cache.TryGet(cacheKey, out var @object))
			//	{
			//		// Yes. Use that
			//		connectedSystemItem = @object!;
			//	}
			//	else
			//	{
			//		// No.

			//		var result = (await _jiraClient
			//					.GetAllByQueryAsync(queryConfig.Type, queryConfig.Query)
			//					.ConfigureAwait(false))
			//					.ToList();

			//		switch (result.Count)
			//		{
			//			case 0:
			//				if (valueIfZeroMatchesFoundSets)
			//				{
			//					return valueIfZeroMatchesFound;
			//				}
			//				throw new LookupException($"Got 0 results for QueryLookup '{queryConfig.Query}' and no default value is configured.");
			//			case 1:
			//				// Convert to JObjects for easier generic manipulation
			//				connectedSystemItem = result
			//					.Select(entity => JObject.FromObject(entity))
			//					.Single();

			//				_cache.Store(cacheKey, connectedSystemItem);
			//				break;
			//			default:
			//				if (valueIfMultipleMatchesFoundSets)
			//				{
			//					return valueIfMultipleMatchesFound;
			//				}
			//				throw new LookupException($"Got {result.Count} results for QueryLookup '{queryConfig.Query}' and no default value is configured.");
			//		}

			//		// Convert to JObjects for easier generic manipulation
			//		connectedSystemItem = result
			//			.Select(entity => JObject.FromObject(entity))
			//			.Single();

			//		_cache.Store(cacheKey, connectedSystemItem);
			//	}

			//	// Determine the field value
			//	if (!connectedSystemItem.TryGetValue(field, out var fieldValue))
			//	{
			//		throw new ConfigurationException($"Field {field} not present for QueryLookup.");
			//	}
			//	return fieldValue;
			//}
			//catch (Exception e)
			//{
			//	Logger.LogError(e, "Failed to Lookup");
			//	throw;
			//}
			throw new NotImplementedException();
		}

		public override async Task PatchAsync(
			string entityClass,
			string entityId,
			Dictionary<string, object> patches,
			CancellationToken cancellationToken
			)
		{
			var patchObject = JObject.FromObject(patches);
			patchObject["sys_id"] = entityId;
			throw new NotImplementedException();
			//await _jiraClient
			//	.PatchAsync(entityClass, patchObject)
			//	.ConfigureAwait(false);
		}

		public override void Dispose()
		{
			throw new NotImplementedException();
			//=> _jiraClient?.Dispose();
		}

		public override Task ClearCacheAsync()
			=> throw new NotImplementedException();
	}
}
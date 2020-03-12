using BetterConsoleTables;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using PanoramicData.ConnectMagic.Service.Ncalc;
using PanoramicData.NCalcExtensions;
using PanoramicData.SheetMagic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal abstract class ConnectedSystemManagerBase : IConnectedSystemManager, IDisposable
	{
		/// <summary>
		/// The connected system
		/// </summary>
		public ConnectedSystem ConnectedSystem { get; }

		/// <summary>
		/// The time to wait for a state item lock
		/// </summary>
		TimeSpan StateItemListLockTimeout = TimeSpan.FromSeconds(300);

		/// <summary>
		/// The state
		/// </summary>
		protected State State { get; }

		protected readonly ILogger Logger;

		private readonly TimeSpan _maxFileAge;

		protected ConnectedSystemManagerBase(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILogger logger)
		{
			ConnectedSystem = connectedSystem ?? throw new ArgumentNullException(nameof(connectedSystem));
			State = state ?? throw new ArgumentNullException(nameof(state));
			Logger = logger;
			_maxFileAge = maxFileAge;
		}

		/// <summary>
		/// NCalc evaluation
		/// </summary>
		/// <param name="systemExpression"></param>
		/// <param name="item"></param>
		/// <param name="state"></param>
		internal static JToken EvaluateToJToken(string systemExpression, JObject item, State state)
		{
			var nCalcExpression = new ConnectMagicExpression(systemExpression);
			nCalcExpression.Parameters.Add("State", state);
			nCalcExpression.Parameters.Add("_", item);
			foreach (var property in item?.Properties() ?? Enumerable.Empty<JProperty>())
			{
				nCalcExpression.Parameters.Add(property.Name, property.Value);
			}
			try
			{
				var evaluationResult = nCalcExpression.Evaluate();
				return evaluationResult is JToken jToken
					? jToken
					: JToken.FromObject(evaluationResult ?? JValue.CreateNull());
			}
			catch (ArgumentException ex)
			{
				if (ex.Message.StartsWith("Parameter was not defined"))
				{
					throw new ArgumentException($"{ex.Message}. Available parameters: {string.Join(", ", nCalcExpression.Parameters.Keys.OrderBy(k => k))}", ex);
				}
				throw;
			}
		}

		/// <summary>
		/// NCalc evaluation
		/// </summary>
		/// <param name="systemExpression"></param>
		/// <param name="stateItem"></param>
		/// <param name="connectedSystemItem"></param>
		/// <param name="state"></param>
		internal static object EvaluateConditionalExpression(string systemExpression, StateItem? stateItem, JObject? connectedSystemItem, State state)
		{
			var nCalcExpression = new ConnectMagicExpression(systemExpression);
			nCalcExpression.Parameters.Add("State", state);
			nCalcExpression.Parameters.Add("connectMagic.stateItemLastModifiedEpochMs", stateItem?.LastModified.ToUnixTimeSeconds() * 1000);
			nCalcExpression.Parameters.Add("connectMagic.stateItem", stateItem);
			nCalcExpression.Parameters.Add("connectMagic.systemItem", connectedSystemItem);

			foreach (var property in stateItem?.Properties() ?? Enumerable.Empty<JProperty>())
			{
				nCalcExpression.Parameters.Add($"connectMagic.stateItem.{property.Name}", property.Value.ToObject<object>());
			}

			foreach (var property in connectedSystemItem?.Properties() ?? Enumerable.Empty<JProperty>())
			{
				nCalcExpression.Parameters.Add($"connectMagic.systemItem.{property.Name}", property.Value.ToObject<object>());
			}

			try
			{
				return nCalcExpression.Evaluate();
			}
			catch (ArgumentException ex)
			{
				if (ex.Message.StartsWith("Parameter was not defined"))
				{
					throw new ArgumentException($"{ex.Message}. Available parameters: {string.Join(", ", nCalcExpression.Parameters.Keys.OrderBy(k => k))}", ex);
				}
				throw;
			}
		}

		/// <summary>
		/// Simple pause function
		/// </summary>
		/// <param name="seconds"></param>
		/// <returns></returns>
		protected async Task PauseSecondsAsync(int seconds)
		{
			Logger.LogInformation($"Pausing for {seconds}s...");
			await Task
				.Delay(TimeSpan.FromSeconds(seconds))
				.ConfigureAwait(false);
			Logger.LogInformation($"Pausing for {seconds}s complete.");
		}

		/// <summary>
		/// Process connected systems
		/// </summary>
		/// <param name="dataSet">The DataSet to process</param>
		/// <param name="connectedSystemItems">The ConnectedSystemItems</param>
		/// <param name="fileInfo">The file to log the action tiems out to</param>
		protected async Task<List<SyncAction>?> ProcessConnectedSystemItemsAsync(
			ConnectedSystemDataSet dataSet,
			List<JObject> connectedSystemItems,
			FileInfo fileInfo,
			CancellationToken cancellationToken)
		{
			// Make sure arguments meet minimum requirements
			if (dataSet == null)
			{
				throw new ArgumentNullException(nameof(dataSet));
			}
			dataSet.Validate();
			dataSet.SubstituteConstants();

			if (connectedSystemItems == null)
			{
				throw new ArgumentNullException(nameof(connectedSystemItems));
			}

			// Filter
			if (dataSet.QueryConfig.Filter != null)
			{
				var ncalcExpression = new ExtendedExpression(dataSet.QueryConfig.Filter);
				connectedSystemItems = connectedSystemItems.Where(csi =>
				{
					ncalcExpression.Parameters = csi.ToObject<Dictionary<string, object>>();
					var isMatchObject = ncalcExpression.Evaluate();
					if (!(isMatchObject is bool isMatch))
					{
						throw new ConfigurationException($"QueryConfig Filter '{dataSet.QueryConfig.Filter}' did not evaluate to a bool.");
					}
					return isMatch;
				}).ToList();
			}

			var syncActions = new List<SyncAction>();
			try
			{
				Logger.LogDebug($"Syncing DataSet {dataSet.Name} with {dataSet.StateDataSetName}");

				var joinMappings = dataSet
					.Mappings
					.Where(m => m.Direction == MappingType.Join)
					.ToList();

				// Inward mappings
				var inwardMappings = dataSet
					.Mappings
					.Where(m => m.Direction == MappingType.In)
					.ToList();

				// Outward mappings
				var outwardMappings = dataSet
					.Mappings
					.Where(m => m.Direction == MappingType.Out)
					.ToList();

				// Get the DataSet from state or create it if it doesn't exist
				if (!State.ItemLists.TryGetValue(dataSet.StateDataSetName, out var stateItemList))
				{
					Logger.LogDebug($"Observed request to access State DataSet {dataSet.StateDataSetName} for the first time - creating...");
					stateItemList = State.ItemLists[dataSet.StateDataSetName] = new StateItemList();
				}

				// Get a lock on the state item list
				var gotLock = await stateItemList
					.Lock
					.WaitAsync(StateItemListLockTimeout, cancellationToken)
					.ConfigureAwait(false);
				if (!gotLock)
				{
					// We didn't get a lock due to timeout - try again next time
					Logger.LogInformation($"Timed out waiting to lock StateDataSet {dataSet.StateDataSetName}");
					return null;
				}
				Logger.LogInformation($"Acquired lock on {dataSet.StateDataSetName} for connect system manager {ConnectedSystem.Name}");
				// We got a lock

				// Clone the DataSet, so we can remove items that we have seen in the ConnectedSystem
				var unseenStateItems = new List<StateItem>(stateItemList);

				try
				{
					AnalyseConnectedSystemItems(
						dataSet,
						connectedSystemItems,
						syncActions,
						stateItemList,
						unseenStateItems,
						joinMappings,
						State,
						Logger,
						cancellationToken);

					AnalyseUnseenItems(
						dataSet,
						syncActions,
						unseenStateItems,
						Logger,
						cancellationToken);

					// _logger.LogInformation(GetLogTable(dataSet, syncActions));

					await ProcessActionListAsync(
						dataSet,
						syncActions,
						stateItemList,
						inwardMappings,
						outwardMappings,
						cancellationToken).ConfigureAwait(false);

					Logger.LogInformation(GetLogTable(dataSet, syncActions));
				}
				catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
				{
					Logger.LogInformation("Task cancelled");
				}
				finally
				{
					// Release the lock
					Logger.LogInformation($"Releasing lock on {dataSet.StateDataSetName} for connect system manager {ConnectedSystem.Name}");
					stateItemList
						.Lock
						.Release();

					WriteSyncActionOutput(
						fileInfo,
						syncActions,
						_maxFileAge,
						Logger);
				}
				// Pass the SyncActions out for logging/examining
				return syncActions;
			}
			catch (Exception e)
			{
				Logger.LogError(e, $"Unhandled exception in ProcessConnectedSystemItems {e.Message}");
				throw;
			}
		}

		private string GetLogTable(ConnectedSystemDataSet dataSet, List<SyncAction> syncActions)
		{
			var stringBuilder = new StringBuilder();
			var value = $"DataSet '{dataSet.Name}'";
			stringBuilder.AppendLine(value);

			var syncActionTypes = Enum.GetValues(typeof(SyncActionType)).Cast<SyncActionType>().ToList();
			var permissions = Enum.GetValues(typeof(DataSetPermission)).Cast<DataSetPermission>().ToList();

			var headers = syncActionTypes.Select(ra => new ColumnHeader(ra.ToString(), Alignment.Right)).Prepend(new ColumnHeader(string.Empty)).ToArray();
			var table = new Table(headers) { Config = TableConfiguration.UnicodeAlt() };
			foreach (var permission in permissions)
			{
				table.AddRow(syncActionTypes
					 .Select(syncAction =>
					 {
						 var count = syncActions.Count(i => i.Type == syncAction && i.Permission == permission);
						 return count == 0 ? "-" : count.ToString();
					 })
					 .Prepend(permission.ToString())
					 .ToArray());
			}
			stringBuilder.AppendLine(table.ToString());
			return stringBuilder.ToString();
		}

		private static void WriteSyncActionOutput(
			FileInfo fileInfo,
			List<SyncAction> syncActions,
			TimeSpan maxFileAge,
			ILogger logger)
		{
			// Age files
			if (fileInfo.Directory.Exists)
			{
				foreach (var fileInfoToAge in fileInfo.Directory.EnumerateFiles("*.xlsx").Where(fi => fi.CreationTimeUtc.Add(maxFileAge) < DateTime.UtcNow))
				{
					logger.LogInformation($"Deleting old file {fileInfoToAge.FullName}");
					fileInfoToAge.Delete();
				}
			}

			if (syncActions == null)
			{
				throw new ArgumentNullException(nameof(syncActions));
			}

			// Dump sync actions to spreadsheet if so requested
			if (fileInfo == null)
			{
				return;
			}
			// Output is required

			// Ensure directory exists
			if (!fileInfo.Directory.Exists)
			{
				fileInfo.Directory.Create();
			}

			using var spreadsheet = new MagicSpreadsheet(fileInfo);
			spreadsheet.AddSheet(
				syncActions.Select(sa =>
				{
					var properties = new Dictionary<string, object>();
					if (sa.ConnectedSystemItem != null)
					{
						foreach (var property in sa.ConnectedSystemItem.Properties().OrderBy(p => p.Name))
						{
							properties[$"SYS.{property.Name}"] = property.Value;
						}
					}
					if (sa.StateItem != null)
					{
						foreach (var property in sa.StateItem.Properties().OrderBy(p => p.Name))
						{
							properties[$"st.{property.Name}"] = property.Value;
						}
					}
					return new Extended<SyncAction>()
					{
						Item = sa,
						Properties = properties
					};
				}).ToList(),
				addSheetOptions: new AddSheetOptions
				{
					ExcludeProperties = new HashSet<string>
					{
						nameof(SyncAction.Functions),
						nameof(SyncAction.InwardChanges),
						nameof(SyncAction.OutwardChanges)
					}
				});
			spreadsheet.Save();
		}

		protected static FileInfo GetFileInfo(ConnectedSystem connectedSystem, DataSet dataSet)
			=> new FileInfo($"Output/{connectedSystem.Name} - {dataSet.Name} - {DateTimeOffset.UtcNow:yyyy-MM-ddTHHmmssZ}.xlsx");

		private async Task ProcessActionListAsync(
			ConnectedSystemDataSet dataSet,
			List<SyncAction> actionList,
			List<StateItem> stateItemList,
			List<Mapping> inwardMappings,
			List<Mapping> outwardMappings,
			CancellationToken cancellationToken)
		{
			Logger.LogInformation($"Processing action list for dataset {dataSet.Name}");

			// Determine up front whether all systems have completed sync
			var isConnectedSystemsSyncCompletedOnce = State.IsConnectedSystemsSyncCompletedOnce();

			// Process the action list
			foreach (var action in actionList)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// Get a lock on the state item
				var gotLock = action.StateItem == null
					|| await action
					.StateItem
					.Lock
					.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken)
					.ConfigureAwait(false);
				if (!gotLock)
				{
					// We didn't get a lock
					Logger.LogInformation($"Timed out waiting to lock StateItem {action.StateItem}");
					continue;
				}
				// We got a lock

				try
				{
					var permission = DeterminePermission(ConnectedSystem, dataSet, action.Type);

					switch (action.Type)
					{
						case SyncActionType.CreateState:
							if (action.ConnectedSystemItem == null)
							{
								throw new InvalidOperationException("Cannot create into State without a ConnectedSystemItem");
							}

							// Creating an object in State
							var newStateItem = new StateItem(new JObject());
							await newStateItem.Lock.WaitAsync().ConfigureAwait(false);
							foreach (var inwardMapping in GetInwardMappingsToProcess(inwardMappings, action))
							{
								var newStateItemValue = EvaluateToJToken(inwardMapping.SystemExpression, action.ConnectedSystemItem, State);
								newStateItem[inwardMapping.StateExpression] = newStateItemValue;

								action.InwardChanges.Add(new FieldChange(inwardMapping.StateExpression, null, newStateItemValue));
							}
							// Save our new item
							action.StateItem = newStateItem;

							if (permission == DataSetPermission.Allowed)
							{
								stateItemList.Add(newStateItem);
							}
							break;
						case SyncActionType.CreateSystem:
							if (action.StateItem == null)
							{
								throw new InvalidOperationException("Cannot create into a ConnectedSystem without a StateItem");
							}

							// Create in the target system
							var newConnectedSystemItem = new JObject();
							foreach (var outwardMapping in GetOutwardMappingsToProcess(outwardMappings, action))
							{
								var newConnectedSystemItemValue = EvaluateToJToken(outwardMapping.StateExpression, action.StateItem, State);
								newConnectedSystemItem[outwardMapping.SystemExpression] = newConnectedSystemItemValue;

								action.OutwardChanges.Add(new FieldChange(outwardMapping.SystemExpression, null, newConnectedSystemItemValue));
							}
							if (isConnectedSystemsSyncCompletedOnce)
							{
								if (permission == DataSetPermission.Allowed)
								{
									await InternalCreateOutwardsAsync(dataSet, newConnectedSystemItem, cancellationToken).ConfigureAwait(false);
									// TODO Create should return created object, call update state afterwards
								}
							}
							else
							{
								permission = DataSetPermission.DeniedAllConnectedSystemsNotYetLoaded;
							}
							break;
						case SyncActionType.DeleteState:
							if (action.StateItem == null)
							{
								throw new InvalidOperationException("Cannot delete from State without a StateItem");
							}

							// Delete from State:
							if (permission == DataSetPermission.Allowed)
							{
								stateItemList.Remove(action.StateItem);
							}
							break;
						case SyncActionType.DeleteSystem:
							// Delete from ConnectedSystem
							if (isConnectedSystemsSyncCompletedOnce)
							{
								if (permission == DataSetPermission.Allowed)
								{
									await InternalDeleteOutwardAsync(dataSet, action, cancellationToken).ConfigureAwait(false);
								}
							}
							else
							{
								permission = DataSetPermission.DeniedAllConnectedSystemsNotYetLoaded;
							}
							break;
						case SyncActionType.UpdateBoth:
							if (action.StateItem == null)
							{
								throw new InvalidOperationException($"Cannot perform a {SyncActionType.UpdateBoth} operation without a {nameof(action.StateItem)}");
							}
							if (action.ConnectedSystemItem == null)
							{
								throw new InvalidOperationException($"Cannot perform a {SyncActionType.UpdateBoth} operation without a {nameof(action.ConnectedSystemItem)}");
							}

							var stateItemClone = StateItem.FromObject(action.StateItem);
							foreach (var inwardMapping in GetInwardMappingsToProcess(inwardMappings, action))
							{
								var newStateItemValue = EvaluateToJToken(inwardMapping.SystemExpression, action.ConnectedSystemItem, State);
								var existingStateItemValue = action.StateItem[inwardMapping.StateExpression];

								//if (existingStateItemValue?.ToString() != newStateItemValue?.ToString())
								if (!JToken.DeepEquals(existingStateItemValue, newStateItemValue))

								{
									stateItemClone[inwardMapping.StateExpression] = newStateItemValue;

									action.InwardChanges.Add(new FieldChange(inwardMapping.StateExpression, existingStateItemValue, newStateItemValue));
								}
							}
							if (action.InwardChanges.Count != 0 && permission == DataSetPermission.Allowed)
							{
								// Add the new one so there is at least 2 versions of the truth and accidental deletions on a parallel dataset processing will not occur
								stateItemList.Add(stateItemClone);
								// Remove the old one
								stateItemList.Remove(action.StateItem);
							}

							foreach (var outwardMapping in GetOutwardMappingsToProcess(outwardMappings, action))
							{
								// Is a system function defined?
								if (outwardMapping.SystemFunction != null)
								{
									// YES - Evaluate both expressions and if he results don't match, add the function to the list
									var newConnectedSystemValue = EvaluateToJToken(outwardMapping.StateExpression, action.StateItem, State);
									var existingConnectedSystemValue = EvaluateToJToken(outwardMapping.SystemExpression, action.ConnectedSystemItem, State);
									if (!JToken.DeepEquals(existingConnectedSystemValue, newConnectedSystemValue))
									{
										action.Functions.Add(outwardMapping.SystemFunction);
										action.OutwardChanges.Add(new FunctionChange(outwardMapping.SystemFunction));
									}
								}
								else
								{
									// NO - Do a simple expression to existing value compare and update if required
									var newConnectedSystemValue = EvaluateToJToken(outwardMapping.StateExpression, action.StateItem, State);
									var existingConnectedSystemValue = outwardMapping.SystemOutField != null
										? EvaluateToJToken(outwardMapping.SystemExpression, action.ConnectedSystemItem, State)
										: action.ConnectedSystemItem[outwardMapping.SystemExpression];
									if (!JToken.DeepEquals(existingConnectedSystemValue, newConnectedSystemValue))
									{
										var targetField = outwardMapping.SystemOutField ?? outwardMapping.SystemExpression;
										action.ConnectedSystemItem[targetField] = newConnectedSystemValue;
										action.OutwardChanges.Add(new FieldChange(targetField, existingConnectedSystemValue, newConnectedSystemValue));
									}
								}
							}
							if (action.OutwardChanges.Count != 0)
							{
								if (isConnectedSystemsSyncCompletedOnce)
								{
									// We are making a change
									if (permission == DataSetPermission.Allowed)
									{
										await InternalUpdateOutwardAsync(dataSet, action, cancellationToken).ConfigureAwait(false);
									}
								}
								else
								{
									permission = DataSetPermission.DeniedAllConnectedSystemsNotYetLoaded;
								}
							}

							// If nothing was done then we're in sync
							if (action.InwardChanges.Count == 0 && action.OutwardChanges.Count == 0)
							{
								action.Type = SyncActionType.AlreadyInSync;
							}
							break;
					}
					action.Permission = permission;
				}
				catch (Exception e)
				{
					action.Type = SyncActionType.RemedyErrorDuringProcessing;
					action.Comment = e.ToString();
				}
				finally
				{
					// Release the lock (if we have one), on the state item (if we have one)
					action
					.StateItem?
					.Lock?
					.Release();
				}
			}
		}

		private List<Mapping> GetOutwardMappingsToProcess(List<Mapping> outwardMappings, SyncAction action)
		{
			var outwardMappingsToProcess = outwardMappings
				.Where(m => m.ConditionExpression == null || (EvaluateConditionalExpression(m.ConditionExpression, action.StateItem, action.ConnectedSystemItem, State) as bool?) == true)
				.ToList();
			Logger.LogTrace($"Processing {outwardMappingsToProcess.Count} of {outwardMappings.Count} outward mappings.");
			return outwardMappingsToProcess;
		}

		private List<Mapping> GetInwardMappingsToProcess(List<Mapping> inwardMappings, SyncAction action)
		{
			var inwardMappingsToProcess = inwardMappings
				.Where(m => m.ConditionExpression == null || (EvaluateConditionalExpression(m.ConditionExpression, action.StateItem, action.ConnectedSystemItem, State) as bool?) == true)
				.ToList();
			Logger.LogTrace($"Processing {inwardMappingsToProcess.Count} of {inwardMappings.Count} inward mappings.");
			return inwardMappingsToProcess;
		}

		private async Task InternalCreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject newConnectedSystemItem, CancellationToken cancellationToken)
		{
			Logger.LogInformation($"Creating item for dataset {dataSet.Name}...");
			await CreateOutwardsAsync(dataSet, newConnectedSystemItem, cancellationToken).ConfigureAwait(false);
		}

		private async Task InternalUpdateOutwardAsync(ConnectedSystemDataSet dataSet, SyncAction action, CancellationToken cancellationToken)
		{
			Logger.LogInformation($"Updating item for dataset {dataSet.Name}...");
			await UpdateOutwardsAsync(dataSet, action, cancellationToken).ConfigureAwait(false);
		}

		private async Task InternalDeleteOutwardAsync(ConnectedSystemDataSet dataSet, SyncAction action, CancellationToken cancellationToken)
		{
			Logger.LogInformation($"Deleting item for dataset {dataSet.Name}...");
			await DeleteOutwardsAsync(
				dataSet,
				action.ConnectedSystemItem ?? throw new InvalidOperationException("Cannot delete from then ConnectedSystem without a ConnectedSystemItem"),
				cancellationToken
			).ConfigureAwait(false);
		}

		private static void AnalyseUnseenItems(
			ConnectedSystemDataSet dataSet,
			List<SyncAction> actionList,
			List<StateItem> unseenStateItems,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			logger.LogInformation($"Analysing unseen items for dataset {dataSet.Name}");

			// Process the unseen items that were found in state but not matched correctly in the ConnectedSystem
			foreach (var unseenStateItem in unseenStateItems)
			{
				cancellationToken.ThrowIfCancellationRequested();

				switch (dataSet.CreateDeleteDirection)
				{
					case CreateDeleteDirection.None:
						// Don't do anything (record that)
						actionList.Add(new SyncAction
						{
							Type = SyncActionType.None,
							StateItem = unseenStateItem
						});
						break;
					case CreateDeleteDirection.In:
						// Remove it from State
						actionList.Add(new SyncAction
						{
							Type = SyncActionType.DeleteState,
							StateItem = unseenStateItem
						});
						break;
					case CreateDeleteDirection.Out:
					case CreateDeleteDirection.CreateBoth:
						// Add it to the ConnectedSystem
						actionList.Add(new SyncAction
						{
							Type = SyncActionType.CreateSystem,
							StateItem = unseenStateItem
						});
						break;
					default:
						// This should never happen due to configuration validation, but we should fail if it does
						throw new InvalidOperationException($"Unexpected dataSet.CreateDeleteDirection {dataSet.CreateDeleteDirection} in DataSet {dataSet.Name}");
				}
			}
		}

		private static void AnalyseConnectedSystemItems(
			ConnectedSystemDataSet dataSet,
			List<JObject> connectedSystemItems,
			List<SyncAction> actionList,
			List<StateItem> stateItemList,
			List<StateItem> unseenStateItems,
			List<Mapping> joinMappings,
			State state,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			logger.LogInformation($"Analysing ConnectedSystem items for dataset {dataSet.Name}");
			var stateItemJoinDictionary = BuildStateItemJoinDictionary(stateItemList, joinMappings, state);

			var connectedSystemItemsSeenJoinValues = new HashSet<string>();

			// Go through each ConnectedSystem item and see if it joins to an item in the State
			foreach (var connectedSystemItem in connectedSystemItems)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var joinValues = joinMappings
					.Select(joinMapping => new { expression = joinMapping.SystemExpression, value = EvaluateToJToken(joinMapping.SystemExpression, connectedSystemItem, state)?.ToString() })
					.Where(a => !string.IsNullOrWhiteSpace(a.value))
					.Select(a => $"{a.expression}={a.value}")
					.ToList();

				// Is this a duplicate WITHIN the ConnectedSystemItems?
				List<StateItem>? matchingStateItems = null;
				string? matchedJoinValue = null;
				foreach (var joinValue in joinValues)
				{
					if (!connectedSystemItemsSeenJoinValues.Add(joinValue))
					{
						// Couldn't add to the hashset (i.e. it was already present)
						// Duplicate found.  What do we do?
						switch (dataSet.DuplicateHandling)
						{
							case DuplicateHandling.RemoveFromConnectedSystem:
								// Remove the duplicate
								actionList.Add(new SyncAction
								{
									Type = SyncActionType.DeleteSystem,
									ConnectedSystemItem = connectedSystemItem,
									Comment = $"Removing duplicate already observed with join value {joinValue}"
								});
								break;
							case DuplicateHandling.Discard:
								// Do nothing
								break;
							default:
								// Flag as a remedy
								actionList.Add(new SyncAction
								{
									Type = SyncActionType.RemedyMultipleConnectedSystemItemsWithSameJoinValue,
									ConnectedSystemItem = connectedSystemItem,
									Comment = $"Found duplicate already observed with join value {joinValue}"
								});
								break;
						}
						// Action determined.  Move to next ConnectedSystemItem.
						continue;
					}
					if (stateItemJoinDictionary.TryGetValue(joinValue, out matchingStateItems))
					{
						// Found a match - don't check any more.
						matchedJoinValue = joinValue;
						break;
					}
				}

				// There should be zero or one matches
				// Any more and there is a matching issue
				switch (matchingStateItems?.Count ?? 0)
				{
					case 0:
						// No match found in state for the existing ConnectedSystemItem
						switch (dataSet.CreateDeleteDirection)
						{
							case CreateDeleteDirection.None:
								// Don't do anything (record that)
								actionList.Add(new SyncAction
								{
									Type = SyncActionType.None,
									ConnectedSystemItem = connectedSystemItem
								});
								break;
							case CreateDeleteDirection.In:
							case CreateDeleteDirection.CreateBoth:
								// Create it in State
								actionList.Add(new SyncAction
								{
									Type = SyncActionType.CreateState,
									ConnectedSystemItem = connectedSystemItem,
								});

								break;
							case CreateDeleteDirection.Out:
								// Remove it from ConnectedSystem
								actionList.Add(new SyncAction
								{
									Type = SyncActionType.DeleteSystem,
									ConnectedSystemItem = connectedSystemItem,
								});
								break;
							default:
								// This should never happen due to configuration validation, but we should fail if it does
								throw new InvalidOperationException($"Unexpected dataSet.CreateDeleteDirection {dataSet.CreateDeleteDirection} in DataSet {dataSet.Name}");
						}
						break;
					case 1:
						// There was a match between State and the ConnectedSystem
						var stateItem = matchingStateItems![0];

						// Remove the stateItem from the unseen list
						unseenStateItems.Remove(stateItem);

						actionList.Add(new SyncAction
						{
							Type = SyncActionType.UpdateBoth,
							StateItem = stateItem,
							ConnectedSystemItem = connectedSystemItem,
						});
						break;
					default:
						// Remove all items we found to be matching from state; as there is more than one match we shouldn't try to add or delete from a ConnectedSystem
						foreach (var matchingStateItem in matchingStateItems!)
						{
							unseenStateItems.Remove(matchingStateItem);

							actionList.Add(new SyncAction
							{
								Type = SyncActionType.RemedyMultipleStateItemsMatchedAConnectedSystemItem,
								ConnectedSystemItem = connectedSystemItem,
								StateItem = matchingStateItem,
								Comment = $"Found duplicate state items with join value {matchedJoinValue}"
							});
						}
						break;
				}
			}
		}

		private static Dictionary<string, List<StateItem>> BuildStateItemJoinDictionary(List<StateItem> stateItemList, List<Mapping> joinMappings, State state)
		{
			// Construct a dictionary of the evaluated join value for each state item to a list of matching items
			var stateItemJoinDictionary = new Dictionary<string, List<StateItem>>();
			foreach (var stateItem in stateItemList.ToList())
			{
				var joinValues = joinMappings
					.Select(joinMapping => new { expression = joinMapping.SystemExpression, value = EvaluateToJToken(joinMapping.StateExpression, stateItem, state)?.ToString() })
					.Where(a => !string.IsNullOrWhiteSpace(a.value))
					.Select(a => $"{a.expression}={a.value}")
					.ToList();

				foreach (var joinValue in joinValues)
				{
					if (!stateItemJoinDictionary.TryGetValue(joinValue, out var itemList))
					{
						itemList = stateItemJoinDictionary[joinValue] = new List<StateItem>();
					}
					itemList.Add(stateItem);
				}
			}

			return stateItemJoinDictionary;
		}

		/// <summary>
		/// Determines permissions
		/// </summary>
		internal static DataSetPermission DeterminePermission(
			ConnectedSystem connectedSystem,
			ConnectedSystemDataSet dataSet,
			SyncActionType type
			)
		{
			if (!connectedSystem.Permissions.CanWrite)
			{
				return DataSetPermission.DeniedAtConnectedSystem;
			}
			if (!dataSet.Permissions.CanWrite)
			{
				return DataSetPermission.DeniedAtConnectedSystemDataSet;
			}

			switch (type)
			{
				case SyncActionType.CreateState:
					return DataSetPermission.Allowed;
				case SyncActionType.CreateSystem:
					return connectedSystem.Permissions.CanCreate && dataSet.Permissions.CanCreate
						? DataSetPermission.Allowed
						: !connectedSystem.Permissions.CanCreate
							? DataSetPermission.DeniedAtConnectedSystem
							: DataSetPermission.DeniedAtConnectedSystemDataSet;
				case SyncActionType.DeleteSystem:
					return connectedSystem.Permissions.CanDelete && dataSet.Permissions.CanDelete
						? DataSetPermission.Allowed
						: !connectedSystem.Permissions.CanDelete
							? DataSetPermission.DeniedAtConnectedSystem
							: DataSetPermission.DeniedAtConnectedSystemDataSet;
				case SyncActionType.UpdateBoth:
					return connectedSystem.Permissions.CanUpdate && dataSet.Permissions.CanUpdate
						? DataSetPermission.Allowed
						: !connectedSystem.Permissions.CanUpdate
							? DataSetPermission.DeniedAtConnectedSystem
							: DataSetPermission.DeniedAtConnectedSystemDataSet;
				case SyncActionType.RemedyMultipleStateItemsMatchedAConnectedSystemItem:
				case SyncActionType.RemedyMultipleConnectedSystemItemsWithSameJoinValue:
				case SyncActionType.AlreadyInSync:
					return DataSetPermission.Allowed;
				default:
					throw new ArgumentOutOfRangeException($"{nameof(SyncActionType)} {type} not allowed.");
			}
		}

		/// <summary>
		/// One or more mapping must be of type "Join"
		/// If multiple Join mappings are specified, any of them may match for a join to be found
		/// The join evaluation is always on the connected system
		/// </summary>
		/// <param name="dataSet"></param>
		/// <returns></returns>

		internal static void SetPropertiesFromJObject(object existing, JObject connectedSystemItem)
		{
			var objectType = existing.GetType();
			foreach (var connectedSystemItemProperty in connectedSystemItem.Properties())
			{
				var propertyInfo = objectType.GetProperty(connectedSystemItemProperty.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				switch (propertyInfo.PropertyType.Name)
				{
					case nameof(String):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<string>(connectedSystemItemProperty.Name));
						break;
					case nameof(Int32):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<int>(connectedSystemItemProperty.Name));
						break;
					case nameof(Int64):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<long>(connectedSystemItemProperty.Name));
						break;
					case nameof(UInt32):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<uint>(connectedSystemItemProperty.Name));
						break;
					case nameof(UInt64):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<ulong>(connectedSystemItemProperty.Name));
						break;
					case nameof(Boolean):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<bool>(connectedSystemItemProperty.Name));
						break;
					case nameof(Single):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<float>(connectedSystemItemProperty.Name));
						break;
					case nameof(Double):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<double>(connectedSystemItemProperty.Name));
						break;
					case nameof(Guid):
						propertyInfo.SetValue(existing, connectedSystemItem.Value<Guid>(connectedSystemItemProperty.Name));
						break;
					default:
						throw new NotSupportedException();
				}
			}
		}

		/// <summary>
		/// Deletes a specific item
		/// </summary>
		/// <param name="dataSet">The DataSet</param>
		/// <param name="connectedSystemItem">The item, to be deleted from the ConnectedSystem.</param>
		internal abstract Task DeleteOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			);

		/// <summary>
		/// Updates a specific item
		/// </summary>
		/// <param name="dataSet">The DataSet</param>
		/// <param name="connectedSystemItem">The item, as updated to be pushed to the ConnectedSystem.</param>
		internal abstract Task UpdateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			SyncAction syncAction,
			CancellationToken cancellationToken
			);

		/// <summary>
		/// Updates a specific item
		/// </summary>
		/// <param name="dataSet">The DataSet</param>
		/// <param name="connectedSystemItem">The item, to be created in the ConnectedSystem.</param>
		internal abstract Task CreateOutwardsAsync(
			ConnectedSystemDataSet dataSet,
			JObject connectedSystemItem,
			CancellationToken cancellationToken
			);

		/// <summary>
		/// Refresh DataSet
		/// </summary>
		/// <param name="dataSet"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public abstract Task RefreshDataSetAsync(ConnectedSystemDataSet dataSet, CancellationToken cancellationToken);

		/// <summary>
		/// Clear the cache
		/// </summary>
		/// <returns></returns>
		public abstract Task ClearCacheAsync();

		/// <summary>
		/// Query Lookup
		/// </summary>
		/// <param name="queryConfig"></param>
		/// <param name="field"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public abstract Task<object> QueryLookupAsync(
			QueryConfig queryConfig,
			string field,
			CancellationToken cancellationToken);

		public abstract void Dispose();

		/// <summary>
		/// Stats
		/// </summary
		public ConnectedSystemStats Stats { get; } = new ConnectedSystemStats();
	}
}
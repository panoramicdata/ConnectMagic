using BetterConsoleTables;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using PanoramicData.ConnectMagic.Service.Ncalc;
using PanoramicData.NCalcExtensions;
using PanoramicData.NCalcExtensions.Exceptions;
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
		/// The state
		/// </summary>
		protected State State { get; }

		private readonly ILogger _logger;

		private readonly TimeSpan _maxFileAge;

		protected ConnectedSystemManagerBase(
			ConnectedSystem connectedSystem,
			State state,
			TimeSpan maxFileAge,
			ILogger logger)
		{
			ConnectedSystem = connectedSystem ?? throw new ArgumentNullException(nameof(connectedSystem));
			State = state ?? throw new ArgumentNullException(nameof(state));
			_logger = logger;
			_maxFileAge = maxFileAge;
		}

		/// <summary>
		/// NCalc? evaluation
		/// </summary>
		/// <param name="systemExpression"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		internal static object Evaluate(string systemExpression, JObject item, State state)
		{
			var parameters = item.ToObject<Dictionary<string, object>>();
			parameters.Add("State", state);
			var nCalcExpression = new ConnectMagicExpression(systemExpression)
			{
				Parameters = parameters
			};
			try
			{
				return nCalcExpression.Evaluate();
			}
			catch (NCalcExtensionsException ex)
			{
				throw;
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
		/// Process connected systems
		/// </summary>
		/// <param name="dataSet">The DataSet to process</param>
		/// <param name="connectedSystemItems">The ConnectedSystemItems</param>
		/// <param name="fileInfo">The file to log the action tiems out to</param>
		/// <returns></returns>
		protected async Task<List<SyncAction>> ProcessConnectedSystemItemsAsync(
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
					var dictionary = csi.ToObject<Dictionary<string, object>>();
					ncalcExpression.Parameters = dictionary;
					var isMatch = (bool)ncalcExpression.Evaluate();
					return isMatch;
				}).ToList();
			}

			var syncActions = new List<SyncAction>();
			try
			{
				_logger.LogDebug($"Syncing DataSet {dataSet.Name} with {dataSet.StateDataSetName}");

				// Get the DataSet from state or create it if it doesn't exist
				if (!State.ItemLists.TryGetValue(dataSet.StateDataSetName, out var stateItemList))
				{
					_logger.LogDebug($"Observed request to access State DataSet {dataSet.StateDataSetName} for the first time - creating...");
					stateItemList = State.ItemLists[dataSet.StateDataSetName] = new ItemList();
				}

				// Clone the DataSet, so we can remove items that we have seen in the ConnectedSystem
				var unseenStateItems = new List<JObject>(stateItemList);

				var joinMapping = GetJoinMapping(dataSet);

				// Inward mappings
				var inwardMappings = dataSet
					.Mappings
					.Where(m => m.Direction == SyncDirection.In)
					.ToList();

				// Outward mappings
				var outwardMappings = dataSet
					.Mappings
					.Where(m => m.Direction == SyncDirection.Out)
					.ToList();

				try
				{
					AnalyseConnectedSystemItems(
						dataSet,
						connectedSystemItems,
						syncActions,
						stateItemList,
						unseenStateItems,
						joinMapping,
						State,
						_logger,
						cancellationToken);

					AnalyseUnseenItems(
						dataSet,
						syncActions,
						unseenStateItems,
						_logger,
						cancellationToken);

					// _logger.LogInformation(GetLogTable(dataSet, syncActions));

					await ProcessActionList(
						dataSet,
						syncActions,
						stateItemList,
						inwardMappings,
						outwardMappings,
						cancellationToken).ConfigureAwait(false);

					_logger.LogInformation(GetLogTable(dataSet, syncActions));
				}
				catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
				{
					_logger.LogInformation("Task cancelled");
				}
				finally
				{
					WriteSyncActionOutput(
						fileInfo,
						syncActions,
						_maxFileAge,
						_logger);
				}
				// Pass the SyncActions out for logging/examining
				return syncActions;
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Unhandled exception in ProcessConnectedSystemItems {e.Message}");
				throw;
			}
		}

		private string GetLogTable(ConnectedSystemDataSet dataSet, List<SyncAction> syncActions)
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"DataSet '{dataSet.Name}'");

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
			spreadsheet.AddSheet(syncActions);
			spreadsheet.Save();
		}

		protected static FileInfo GetFileInfo(ConnectedSystem connectedSystem, DataSet dataSet)
			=> new FileInfo($"Output/{connectedSystem.Name} - {dataSet.Name} - {DateTimeOffset.UtcNow:yyyy-MM-ddTHHmmssZ}.xlsx");

		private async Task ProcessActionList(
			ConnectedSystemDataSet dataSet,
			List<SyncAction> actionList,
			ItemList stateItemList,
			List<Mapping> inwardMappings,
			List<Mapping> outwardMappings,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Processing action list for dataset {dataSet.Name}");

			// Determine up front whether all systems have completed sync
			var isConnectedSystemsSyncCompletedOnce = State.IsConnectedSystemsSyncCompletedOnce();

			// Process the action list
			foreach (var action in actionList)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					var permission = DeterminePermission(ConnectedSystem, dataSet, action.Type);

					switch (action.Type)
					{
						case SyncActionType.Create:
							switch (dataSet.CreateDeleteDirection)
							{
								case SyncDirection.In:
									// Creating an object in State
									var newStateItem = new JObject();
									foreach (var inwardMapping in inwardMappings)
									{
										var inwardValue = Evaluate(inwardMapping.SystemExpression, action.ConnectedSystemItem, State);
										newStateItem[inwardMapping.StateExpression] = inwardValue == null ? null : JToken.FromObject(inwardValue);
									}
									// Save our new item
									action.StateItem = newStateItem;

									if (permission == DataSetPermission.Allowed)
									{
										stateItemList.Add(newStateItem);
									}
									break;
								case SyncDirection.Out:
									// Create in the target system
									var newConnectedSystemItem = new JObject();
									foreach (var outwardMapping in outwardMappings)
									{
										var outwardValue = Evaluate(outwardMapping.StateExpression, action.StateItem, State);
										newConnectedSystemItem[outwardMapping.SystemExpression] = JToken.FromObject(outwardValue);
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
								default:
									throw new InvalidOperationException($"Cannot perform {action.Type} when {nameof(dataSet.CreateDeleteDirection)} is {dataSet.CreateDeleteDirection}");
							}
							break;
						case SyncActionType.Delete:
							switch (dataSet.CreateDeleteDirection)
							{
								case SyncDirection.In:
									// Delete from State:
									if (permission == DataSetPermission.Allowed)
									{
										stateItemList.Remove(action.StateItem);
									}
									break;
								case SyncDirection.Out:
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
								default:
									throw new InvalidOperationException($"Cannot perform {action.Type} when {nameof(dataSet.CreateDeleteDirection)} is {dataSet.CreateDeleteDirection}");
							}
							break;
						case SyncActionType.Update:

							// TODO - later, might want to log/count that a change has happened. For now, just overwrite in each direction.
							var updateCount = 0;
							var inwardUpdateRequired = false;
							var stateItemClone = JObject.FromObject(action.StateItem);
							foreach (var inwardMapping in inwardMappings)
							{
								var evaluationResult = Evaluate(inwardMapping.SystemExpression, action.ConnectedSystemItem, State);
								var newValue = evaluationResult == null
									? null
									: JToken.FromObject(evaluationResult);
								var existingValue = action.StateItem[inwardMapping.StateExpression];
								if (existingValue?.Type == JTokenType.Null)
								{
									existingValue = null;
								}
								if (existingValue?.ToString() != newValue?.ToString())
								{
									inwardUpdateRequired = true;
									stateItemClone[inwardMapping.StateExpression] = newValue;
									updateCount++;
								}
							}
							if (inwardUpdateRequired)
							{
								if (permission == DataSetPermission.Allowed)
								{
									// Add the new one so there is at least 2 versions of the truth and accidental deletions on a parallel dataset processing will not occur
									stateItemList.Add(stateItemClone);
									// Remove the old one
									stateItemList.Remove(action.StateItem);
								}
							}

							var outwardUpdateRequired = false;
							foreach (var outwardMapping in outwardMappings)
							{
								var newEvaluatedValue = JToken.FromObject(Evaluate(outwardMapping.StateExpression, action.StateItem, State));
								var existingConnectedSystemValue = action.ConnectedSystemItem[outwardMapping.SystemExpression];
								var areEqual = false;
								// Are they both decimals?
								if (decimal.TryParse(existingConnectedSystemValue.ToString(), out var existingValueDecimal) && decimal.TryParse(newEvaluatedValue.ToString(), out var newEvaluatedValueDecimal))
								{
									// Yes.  Are the equal?
									areEqual = existingValueDecimal == newEvaluatedValueDecimal;
								}
								else if (existingConnectedSystemValue.ToString() == newEvaluatedValue.ToString())
								{
									areEqual = true;
								}
								if (!areEqual)
								{
									action.ConnectedSystemItem[outwardMapping.SystemExpression] = JToken.FromObject(newEvaluatedValue);
									outwardUpdateRequired = true;
								}
							}
							if (outwardUpdateRequired)
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
							if (!inwardUpdateRequired && !outwardUpdateRequired)
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
			}
		}

		private async Task InternalDeleteOutwardAsync(ConnectedSystemDataSet dataSet, SyncAction action, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Deleting item for dataset {dataSet.Name}...");
			await DeleteOutwardsAsync(dataSet, action.ConnectedSystemItem, cancellationToken).ConfigureAwait(false);
		}

		private async Task InternalUpdateOutwardAsync(ConnectedSystemDataSet dataSet, SyncAction action, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Updating item for dataset {dataSet.Name}...");
			await UpdateOutwardsAsync(dataSet, action.ConnectedSystemItem, cancellationToken).ConfigureAwait(false);
		}

		private async Task InternalCreateOutwardsAsync(ConnectedSystemDataSet dataSet, JObject newConnectedSystemItem, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Creating item for dataset {dataSet.Name}...");
			await CreateOutwardsAsync(dataSet, newConnectedSystemItem, cancellationToken).ConfigureAwait(false);
		}

		private static void AnalyseUnseenItems(
			ConnectedSystemDataSet dataSet,
			List<SyncAction> actionList,
			List<JObject> unseenStateItems,
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
					case SyncDirection.None:
						// Don't do anything (record that)
						actionList.Add(new SyncAction
						{
							Type = SyncActionType.None,
							StateItem = unseenStateItem
						});
						break;
					case SyncDirection.In:
						// Remove it from State
						actionList.Add(new SyncAction
						{
							Type = SyncActionType.Delete,
							StateItem = unseenStateItem
						});
						break;
					case SyncDirection.Out:
						// Add it to the ConnectedSystem
						actionList.Add(new SyncAction
						{
							Type = SyncActionType.Create,
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
			ItemList stateItemList,
			List<JObject> unseenStateItems,
			Mapping joinMapping,
			State state,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			logger.LogInformation($"Analysing ConnectedSystem items for dataset {dataSet.Name}");
			var stateItemJoinDictionary = BuildStateItemJoinDictionary(stateItemList, joinMapping, state);

			var connectedSystemItemsSeenJoinValues = new HashSet<string>();

			// Go through each ConnectedSystem item and see if it is present in the State FieldSet
			foreach (var connectedSystemItem in connectedSystemItems)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var joinValue = Evaluate(joinMapping.SystemExpression, connectedSystemItem, state).ToString();

				// Is this a duplicate WITHIN the ConnectedSystemItems?
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
								Type = SyncActionType.Delete,
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

				stateItemJoinDictionary.TryGetValue(joinValue, out var matchingStateItems);

				// There should be zero or one matches
				// Any more and there is a matching issue
				switch (matchingStateItems?.Count ?? 0)
				{
					case 0:
						// No match found in state for the existing ConnectedSystemItem
						switch (dataSet.CreateDeleteDirection)
						{
							case SyncDirection.None:
								// Don't do anything (record that)
								actionList.Add(new SyncAction
								{
									Type = SyncActionType.None,
									ConnectedSystemItem = connectedSystemItem
								});
								break;
							case SyncDirection.In:
								// Create it in State
								actionList.Add(new SyncAction
								{
									Type = SyncActionType.Create,
									ConnectedSystemItem = connectedSystemItem,
								});

								break;
							case SyncDirection.Out:
								// Remove it from ConnectedSystem
								actionList.Add(new SyncAction
								{
									Type = SyncActionType.Delete,
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
						var stateItem = matchingStateItems[0];

						// Remove the stateItem from the unseen list
						unseenStateItems.Remove(stateItem);

						actionList.Add(new SyncAction
						{
							Type = SyncActionType.Update,
							StateItem = stateItem,
							ConnectedSystemItem = connectedSystemItem,
						});
						break;
					default:
						// Remove all items we found to be matching from state; as there is more than one match we shouldn't try to add or delete from a ConnectedSystem
						foreach (var matchingStateItem in matchingStateItems)
						{
							unseenStateItems.Remove(matchingStateItem);

							actionList.Add(new SyncAction
							{
								Type = SyncActionType.RemedyMultipleStateItemsMatchedAConnectedSystemItem,
								ConnectedSystemItem = connectedSystemItem,
								StateItem = matchingStateItem,
								Comment = $"Found duplicate state items with join value {joinValue}"
							});
						}
						break;
				}
			}
		}

		private static Dictionary<string, ItemList> BuildStateItemJoinDictionary(ItemList stateItemList, Mapping joinMapping, State state)
		{
			// Construct a dictionary of the evaluated join value for each state item to a list of matching items
			var stateItemJoinDictionary = new Dictionary<string, ItemList>();
			foreach (var stateItem in stateItemList.ToList())
			{
				var key = Evaluate(joinMapping.StateExpression, stateItem, state).ToString();
				if (!stateItemJoinDictionary.TryGetValue(key, out var itemList))
				{
					itemList = stateItemJoinDictionary[key] = new ItemList();
				}
				itemList.Add(stateItem);
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
				case SyncActionType.Create:
					return connectedSystem.Permissions.CanCreate && dataSet.Permissions.CanCreate
						? DataSetPermission.Allowed
						: !connectedSystem.Permissions.CanCreate
							? DataSetPermission.DeniedAtConnectedSystem
							: DataSetPermission.DeniedAtConnectedSystemDataSet;
				case SyncActionType.Delete:
					return connectedSystem.Permissions.CanDelete && dataSet.Permissions.CanDelete
						? DataSetPermission.Allowed
						: !connectedSystem.Permissions.CanDelete
							? DataSetPermission.DeniedAtConnectedSystem
							: DataSetPermission.DeniedAtConnectedSystemDataSet;
				case SyncActionType.Update:
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

		private static Mapping GetJoinMapping(ConnectedSystemDataSet dataSet)
		{
			// Only a single mapping may be of type "Join"
			// The join evaluation is always on the connected system
			var joinMapping = dataSet.Mappings.SingleOrDefault(m => m.Direction == SyncDirection.Join);
			if (joinMapping == null)
			{
				throw new ConfigurationException($"DataSet {dataSet.Name} does not have exactly one mapping of type Join.");
			}
			// We have a single Join mapping

			if (string.IsNullOrWhiteSpace(joinMapping.StateExpression))
			{
				throw new ConfigurationException($"DataSet {dataSet.Name} Join mapping does not have a non-empty {nameof(joinMapping.StateExpression)} defined.");
			}

			if (string.IsNullOrWhiteSpace(joinMapping.SystemExpression))
			{
				throw new ConfigurationException($"DataSet {dataSet.Name} Join mapping does not have a non-empty {nameof(joinMapping.SystemExpression)} defined.");
			}

			return joinMapping;
		}

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
			JObject connectedSystemItem,
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
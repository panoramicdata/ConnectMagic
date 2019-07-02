using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using PanoramicData.ConnectMagic.Service.Ncalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal abstract class ConnectedSystemManagerBase : IConnectedSystemManager
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

		protected ConnectedSystemManagerBase(ConnectedSystem connectedSystem, State state, ILogger logger)
		{
			ConnectedSystem = connectedSystem ?? throw new ArgumentNullException(nameof(connectedSystem));
			State = state ?? throw new ArgumentNullException(nameof(state));
			_logger = logger;
		}

		/// <summary>
		/// NCalc? evaluation
		/// </summary>
		/// <param name="systemExpression"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		internal string Evaluate(string systemExpression, JObject item)
		{
			var parameters = item.ToObject<Dictionary<string, object>>();
			parameters.Add("State", State);
			var nCalcExpression = new ConnectMagicExpression(systemExpression)
			{
				Parameters = parameters
			};
			try
			{
				return nCalcExpression.Evaluate()?.ToString();
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

		protected List<SyncAction> ProcessConnectedSystemItems(ConnectedSystemDataSet dataSet, List<JObject> connectedSystemItems)
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

			var actionList = new List<SyncAction>();
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

				// Go through each ConnectedSystem item and see if it is present in the State FieldSet
				foreach (var connectedSystemItem in connectedSystemItems)
				{
					var systemJoinValue = Evaluate(joinMapping.SystemExpression, connectedSystemItem);

					var matchingStateItems = stateItemList
						.Where(fs => Evaluate(joinMapping.StateExpression, fs) == systemJoinValue)
						.ToList();

					// There should be zero or one matches
					// Any more and there is a matching issue
					switch (matchingStateItems.Count)
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
									StateItem = matchingStateItem
								});
							}
							break;
					}
				}

				// Process the unseen items that were found in state but not matched correctly in the ConnectedSystem
				foreach (var unseenStateItem in unseenStateItems)
				{
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

				// Process the actionlist
				foreach (var action in actionList)
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
										newStateItem[inwardMapping.StateExpression] = Evaluate(inwardMapping.SystemExpression, action.ConnectedSystemItem);
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
										newConnectedSystemItem[outwardMapping.SystemExpression] = Evaluate(outwardMapping.StateExpression, action.StateItem);
									}
									if (State.IsConnectedSystemsSyncCompletedOnce())
									{
										if (permission == DataSetPermission.Allowed)
										{
											CreateOutwards(dataSet, newConnectedSystemItem);
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
									if (State.IsConnectedSystemsSyncCompletedOnce())
									{
										if (permission == DataSetPermission.Allowed)
										{
											DeleteOutwards(dataSet, action.StateItem);
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
								var newValue = Evaluate(inwardMapping.SystemExpression, action.ConnectedSystemItem);
								var existingValue = action.StateItem.Value<string>(inwardMapping.StateExpression);
								if (existingValue != newValue)
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
								var evaluatedValue = Evaluate(outwardMapping.SystemExpression, action.StateItem);
								if (action.ConnectedSystemItem[outwardMapping.StateExpression].ToString() != evaluatedValue)
								{
									action.ConnectedSystemItem[outwardMapping.StateExpression] = evaluatedValue;
									outwardUpdateRequired = true;
								}
							}
							if (outwardUpdateRequired)
							{
								if (State.IsConnectedSystemsSyncCompletedOnce())
								{
									// We are making a change
									if (permission == DataSetPermission.Allowed)
									{
										UpdateOutwards(dataSet, action.ConnectedSystemItem);
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

				// Pass the SyncActions out for logging/examining
				return actionList;
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Unhandled exception in ProcessConnectedSystemItems {e.Message}");
				throw;
			}
		}

		/// <summary>
		/// Determines permissions
		/// </summary>
		internal static DataSetPermission DeterminePermission(ConnectedSystem connectedSystem, ConnectedSystemDataSet dataSet, SyncActionType type)
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

		/// <summary>
		/// Deletes a specific item
		/// </summary>
		/// <param name="dataSet">The DataSet</param>
		/// <param name="connectedSystemItem">The item, to be deleted from the ConnectedSystem.</param>
		internal abstract void DeleteOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem);

		/// <summary>
		/// Updates a specific item
		/// </summary>
		/// <param name="dataSet">The DataSet</param>
		/// <param name="connectedSystemItem">The item, as updated to be pushed to the ConnectedSystem.</param>
		internal abstract void UpdateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem);

		/// <summary>
		/// Updates a specific item
		/// </summary>
		/// <param name="dataSet">The DataSet</param>
		/// <param name="connectedSystemItem">The item, to be created in the ConnectedSystem.</param>
		internal abstract void CreateOutwards(ConnectedSystemDataSet dataSet, JObject connectedSystemItem);

		/// <summary>
		/// Refresh DataSets
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public abstract Task RefreshDataSetsAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Query Lookup
		/// </summary>
		/// <param name="query"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		public abstract Task<object> QueryLookupAsync(string query, string field);

		/// <summary>
		/// Stats
		/// </summary
		public ConnectedSystemStats Stats { get; } = new ConnectedSystemStats();
	}
}
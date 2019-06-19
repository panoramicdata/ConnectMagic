using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using PanoramicData.NCalcExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal abstract class ConnectedSystemManagerBase
	{
		/// <summary>
		/// The connected system
		/// </summary>
		protected ConnectedSystem ConnectedSystem { get; }

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
			var nCalcExpression = new ExtendedExpression(systemExpression);
			nCalcExpression.Parameters = item.ToObject<Dictionary<string, object>>();
			try
			{
				return nCalcExpression.Evaluate().ToString();
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

		protected void ProcessConnectedSystemItems(ConnectedSystemDataSet dataSet, List<JObject> connectedSystemItems)
		{
			_logger.LogDebug($"Syncing DataSet {dataSet.Name} with {dataSet.StateDataSetName}");

			// Get the fieldSet
			if (!State.ItemLists.TryGetValue(dataSet.StateDataSetName, out var stateItemList))
			{
				stateItemList = State.ItemLists[dataSet.StateDataSetName] = new ItemList();
			}

			// Get the list of items present in both
			// Get the list of items present in the ConnectedSystem, but not present in the FieldSet
			// Get the list of items present in the FieldSet, but not present in the ConnectedSystem

			// Clone the fieldSet, removing items that we have seen
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

				var stateMatches = stateItemList
					.Where(fs => fs[joinMapping.StateExpression].ToString() == systemJoinValue)
					.ToList();

				// There should be zero or one matches.
				// Any more and there is a matching issue
				switch (stateMatches.Count)
				{
					case 0:
						switch (dataSet.CreateDeleteDirection)
						{
							case SyncDirection.None:
								break;
							case SyncDirection.In:
								// Add it to State
								var newItem = new JObject();
								foreach (var inwardMapping in inwardMappings)
								{
									newItem[inwardMapping.StateExpression] = Evaluate(inwardMapping.SystemExpression, connectedSystemItem);
								}
								// Need to add the join field also so we can compare as part of the check to see whether it exists above
								newItem[joinMapping.StateExpression] = Evaluate(joinMapping.SystemExpression, connectedSystemItem);
								stateItemList.Add(newItem);
								break;
							case SyncDirection.Out:
								// Remove it from ConnectedSystem
								if (State.IsConnectedSystemsSyncCompletedOnce)
								{
									DeleteOutwards(dataSet, connectedSystemItem);
								}
								else
								{
									_logger.LogInformation("Delaying deletes until all ConnectedSystems have retrieved data");
								}
								break;
							default:
								throw new NotSupportedException($"Unexpected dataSet.CreateDeleteDirection {dataSet.CreateDeleteDirection} in DataSet {dataSet.Name}");
						}
						break;
					case 1:
						var stateItem = stateMatches.Single();

						// Remove the stateItem from the unseen list
						unseenStateItems.Remove(stateItem);

						// TODO - later, might want to log/count that a change has happened.
						// For now, just overwrite in each direction.
						var updateCount = 0;
						foreach (var inwardMapping in inwardMappings)
						{
							var newValue = Evaluate(inwardMapping.SystemExpression, connectedSystemItem);
							var existing = stateItem[inwardMapping.StateExpression];
							if (existing.ToString() != newValue)
							{
								_logger.LogDebug($"Updated entry with {joinMapping.StateExpression} {systemJoinValue}. {inwardMapping.StateExpression} changed from '{existing}' to '{newValue}'");
								stateItem[inwardMapping.StateExpression] = newValue;
								updateCount++;
							}
						}

						_logger.LogTrace($"{updateCount} field updates for entry with {joinMapping.StateExpression} {systemJoinValue}");

						var outwardUpdateRequired = false;
						foreach (var outwardMapping in outwardMappings)
						{
							var evaluatedValue = Evaluate(outwardMapping.SystemExpression, stateItem);
							if (connectedSystemItem[outwardMapping.StateExpression].ToString() != evaluatedValue)
							{
								connectedSystemItem[outwardMapping.StateExpression] = evaluatedValue;
								outwardUpdateRequired = true;
							}
						}
						if (outwardUpdateRequired)
						{
							if (State.IsConnectedSystemsSyncCompletedOnce)
							{
								UpdateOutwards(dataSet, connectedSystemItem);
							}
							else
							{
								_logger.LogInformation("Delaying updates until all ConnectedSystems have retrieved data");
							}
						}
						break;
					default:
						// TODO - handle this
						_logger.LogWarning($"Got {stateMatches.Count} matches, expected 0 or 1");
						break;
				}
			}

			// Push the unseen items
			foreach (var unseenStateItem in unseenStateItems)
			{
				switch (dataSet.CreateDeleteDirection)
				{
					case SyncDirection.None:
						break;
					case SyncDirection.In:
						// Remove it from State
						stateItemList.Remove(unseenStateItem);
						break;
					case SyncDirection.Out:
						// Add it to the ConnectedSystem
						var newItem = new JObject();
						foreach (var outwardMapping in outwardMappings)
						{
							newItem[outwardMapping.StateExpression] = Evaluate(outwardMapping.SystemExpression, unseenStateItem);
						}
						if (State.IsConnectedSystemsSyncCompletedOnce)
						{
							CreateOutwards(dataSet, newItem);
						}
						else
						{
							_logger.LogInformation("Delaying creates until all ConnectedSystems have retrieved data");
						}
						break;
					default:
						throw new NotSupportedException($"Unexpected dataSet.CreateDeleteDirection {dataSet.CreateDeleteDirection} in DataSet {dataSet.Name}");
				}
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
	}
}
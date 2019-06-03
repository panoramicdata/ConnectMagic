using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
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

		protected ConnectedSystemManagerBase(ConnectedSystem connectedSystem, State state)
		{
			ConnectedSystem = connectedSystem ?? throw new ArgumentNullException(nameof(connectedSystem));
			State = state ?? throw new ArgumentNullException(nameof(state));
		}

		internal string Evaluate(string systemExpression, JObject item)
			=> throw new NotImplementedException();

		protected void ProcessConnectedSystemItems(ConnectedSystemDataSet dataSet, List<JObject> connectedSystemItems)
		{
			// Get the fieldSet
			if (!State.ItemLists.TryGetValue(dataSet.Name, out var stateItemList))
			{
				stateItemList = State.ItemLists[dataSet.Name] = new ItemList();
			}

			// Get the list of items present in both
			// Get the list of items present in the ConnectedSystem, but not present in the FieldSet
			// Get the list of items present in the FieldSet, but not present in the ConnectedSystem

			// Clone the fieldSet, removing items that we have seen
			var unseenStateItems = new List<JObject>(stateItemList);

			// Only a single mapping may be of type "Join"
			// The join evaluation is always on the connected system
			var joinMapping = dataSet.Mappings.SingleOrDefault(m => m.Direction == SyncDirection.Join);
			if (joinMapping == null)
			{
				throw new ConfigurationException($"DataSet {dataSet.Name} does not have exactly one mapping of type Join.");
			}
			// We have a single mapping
			var stateFieldName = joinMapping.FieldName;

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

			// Go through each ConnectedSyatem item and see if it is present in the State FieldSet
			foreach (var connectedSystemItem in connectedSystemItems)
			{
				var systemValue = Evaluate(joinMapping.Expression, connectedSystemItem);

				var stateMatches = stateItemList
					.Where(fs => fs[stateFieldName].ToString() == systemValue)
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
									newItem[inwardMapping.FieldName] = Evaluate(inwardMapping.Expression, connectedSystemItem);
								}
								stateItemList.Add(newItem);
								break;
							case SyncDirection.Out:
								// Remove it from ConnectedSystem
								DeleteOutwards(dataSet, connectedSystemItem);
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
						foreach (var inwardMapping in inwardMappings)
						{
							stateItem[inwardMapping.FieldName] = Evaluate(inwardMapping.Expression, connectedSystemItem);
						}

						var outwardUpdateRequired = false;
						foreach (var outwardMapping in outwardMappings)
						{
							var evaluatedValue = Evaluate(outwardMapping.Expression, stateItem);
							if (connectedSystemItem[outwardMapping.FieldName].ToString() != evaluatedValue)
							{
								connectedSystemItem[outwardMapping.FieldName] = evaluatedValue;
								outwardUpdateRequired = true;
							}
						}
						if (outwardUpdateRequired)
						{
							UpdateOutwards(dataSet, connectedSystemItem);
						}
						break;
					default:
						// TODO - handle this
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
							newItem[outwardMapping.FieldName] = Evaluate(outwardMapping.Expression, unseenStateItem);
						}
						CreateOutwards(dataSet, newItem);
						break;
					default:
						throw new NotSupportedException($"Unexpected dataSet.CreateDeleteDirection {dataSet.CreateDeleteDirection} in DataSet {dataSet.Name}");
				}
			}
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
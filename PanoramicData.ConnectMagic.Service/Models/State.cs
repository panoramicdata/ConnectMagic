using Newtonsoft.Json;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// System state
	/// </summary>
	[DataContract]
	public class State
	{
		private bool _allHaveSyncedOnce;

		/// <summary>
		/// The cache filename
		/// </summary>
		[DataMember(Name = "CacheFileName")]

		public string? CacheFileName { get; set; }

		/// <summary>
		/// The actual data is stored here
		/// </summary>
		public ConcurrentDictionary<string, StateItemList> ItemLists { get; set; } = new ConcurrentDictionary<string, StateItemList>();

		public static State FromFile(FileInfo fileInfo)
		{
			// On first start-up, there will be no file
			// In this case, just return a new State.
			if (!fileInfo.Exists)
			{
				return new State { CacheFileName = fileInfo.FullName };
			}

			// Deserialize JSON directly from a file
			using var file = File.OpenText(fileInfo.FullName);
			var serializer = new JsonSerializer();
			var state = (State?)serializer.Deserialize(file, typeof(State));

			if (state is null)
			{
				throw new InvalidOperationException($"Cache load failed - no state object present in '{fileInfo.FullName}'");
			}

			// Set the name of the file that state was loaded from
			state.CacheFileName = fileInfo.FullName;
			return state;
		}

		internal async Task<object?> QueryConnectedSystemAsync(
			string connectedSystemName,
			QueryConfig queryConfig,
			string queryLookupField,
			bool valueIfZeroMatchesFoundSets,
			object? valueIfZeroMatchesFound,
			bool valueIfMultipleMatchesFoundSets,
			object? valueIfMultipleMatchesFound,
			CancellationToken cancellationToken)
		{
			if (!ConnectedSystemManagers.TryGetValue(connectedSystemName, out var connectedSystemManager))
			{
				throw new ConfigurationException($"Could not find queryLookup() connected system manager for connected system {connectedSystemName}");
			}
			return await connectedSystemManager.QueryLookupAsync(
				queryConfig,
				queryLookupField,
				valueIfZeroMatchesFoundSets,
				valueIfZeroMatchesFound,
				valueIfMultipleMatchesFoundSets,
				valueIfMultipleMatchesFound,
				cancellationToken
				).ConfigureAwait(false);
		}

		internal async Task PatchConnectedSystemAsync(
			string connectedSystemName,
			string entityClass,
			string entityId,
			Dictionary<string, object> patchObject,
			CancellationToken cancellationToken)
		{
			if (!ConnectedSystemManagers.TryGetValue(connectedSystemName, out var connectedSystemManager))
			{
				throw new ConfigurationException($"Could not find Patch connected system manager for connected system {connectedSystemName}");
			}

			await connectedSystemManager.PatchAsync(
				entityClass,
				entityId,
				patchObject,
				cancellationToken
				).ConfigureAwait(false);
		}

		public void Save(FileInfo fileInfo)
		{
			// Serialize JSON directly to a file
			using var file = File.CreateText(fileInfo.FullName);
			var serializer = new JsonSerializer();
			serializer.Serialize(file, this);
		}

		/// <summary>
		/// Determines whether all ConnectedSystems have finished sync at least once
		/// </summary>
		/// <returns>True if all ConnectedSystems have finished sync at least once</returns>
		public bool IsConnectedSystemsSyncCompletedOnce()
			=> _allHaveSyncedOnce || (_allHaveSyncedOnce = ConnectedSystemManagers.Values.All(csm => csm.Stats.LastSyncCompleted > DateTimeOffset.MinValue));

		/// <summary>
		/// The ConnectedSystemManagers
		/// </summary>
		public Dictionary<string, IConnectedSystemManager> ConnectedSystemManagers { get; set; } = new Dictionary<string, IConnectedSystemManager>();
	}
}
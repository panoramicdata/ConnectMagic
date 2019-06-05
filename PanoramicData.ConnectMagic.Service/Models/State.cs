using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Models
{
	/// <summary>
	/// System state
	/// </summary>
	[DataContract]
	public class State
	{
		/// <summary>
		/// The cache filename
		/// </summary>
		[DataMember(Name = "CacheFileName")]

		public string CacheFileName { get; set; }

		/// <summary>
		/// The DataSets
		/// </summary>
		[DataMember(Name = "DataSets")]
		public List<StateDataSet> DataSets { get; set; }

		/// <summary>
		/// The actual data is stored here
		/// </summary>
		public ConcurrentDictionary<string, ItemList> ItemLists { get; set; } = new ConcurrentDictionary<string, ItemList>();

		/// <summary>
		/// Used internally to centrally store stats for systems
		/// </summary>
		[IgnoreDataMember]
		internal ConcurrentDictionary<string, ConnectedSystemStats> ConnectedSystemStats { get; set; } = new ConcurrentDictionary<string, ConnectedSystemStats>();

		public static State FromFile(FileInfo fileInfo)
		{
			// On first start-up, there will be no file
			// In this case, just return a new State.
			if (!fileInfo.Exists)
			{
				return new State { CacheFileName = fileInfo.FullName };
			}

			// Deserialize JSON directly from a file
			using (var file = File.OpenText(fileInfo.FullName))
			{
				var serializer = new JsonSerializer();
				var state = (State)serializer.Deserialize(file, typeof(State));
				// Set the name of the file that state was loaded from
				state.CacheFileName = fileInfo.FullName;
				return state;
			}
		}

		public void Save(FileInfo fileInfo)
		{
			// Serialize JSON directly to a file
			using (var file = File.CreateText(fileInfo.FullName))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(file, this);
			}
		}

		public void MarkSyncStarted(ConnectedSystem connectedSystem)
		{
			var stats = GetStats(connectedSystem);
			stats.LastSyncStarted = DateTimeOffset.UtcNow;
		}

		public void MarkSyncCompleted(ConnectedSystem connectedSystem)
		{
			var stats = GetStats(connectedSystem);
			stats.LastSyncCompleted = DateTimeOffset.UtcNow;
		}

		/// <summary>
		/// Determines whether all ConnectedSystems have finished sync at least once
		/// </summary>
		/// <returns>True if all ConnectedSystems have finished sync at least once</returns>
		public bool IsConnectedSystemsSyncCompletedOnce
			// TODO - Cache this as true once all have completed
			=> ConnectedSystemStats.All(css => css.Value.LastSyncCompleted > DateTimeOffset.MinValue);

		private ConnectedSystemStats GetStats(ConnectedSystem connectedSystem)
			=> ConnectedSystemStats.TryGetValue(connectedSystem.Name, out var stats)
				? stats
				: ConnectedSystemStats[connectedSystem.Name] = new ConnectedSystemStats();
	}
}
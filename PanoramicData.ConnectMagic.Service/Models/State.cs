using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
			using (StreamWriter file = File.CreateText(fileInfo.FullName))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(file, this);
			}
		}
	}
}
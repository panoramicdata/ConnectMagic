using Newtonsoft.Json.Linq;
using System;
using System.Threading;

namespace PanoramicData.ConnectMagic.Service.Models
{
	public class StateItem : JObject
	{
		public StateItem(JObject @object) : base(@object)
		{
		}

		public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);

		/// <summary>
		/// When it was created in state
		/// </summary>
		public DateTimeOffset Created { get; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// When it was last modified in state
		/// </summary>
		public DateTimeOffset LastModified { get; private set; } = DateTimeOffset.UtcNow;

		internal static StateItem FromObject(StateItem stateItem)
			=> new(JObject.FromObject(stateItem));

		public new JToken? this[string key]
		{
			get => base[key];
			set
			{
				LastModified = DateTimeOffset.UtcNow;
				base[key] = value;
			}
		}
	}
}
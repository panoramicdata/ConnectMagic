using PanoramicData.ConnectMagic.Service.Interfaces;
using System;
using System.Collections.Concurrent;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class QueryCache<T> : ICache<T> where T : class
	{
		private readonly TimeSpan _timeSpan;

		private readonly ConcurrentDictionary<string, CacheRecord<T>> _dictionary
			= new();

		public QueryCache(TimeSpan timeSpan)
		{
			_timeSpan = timeSpan;
		}

		public void Clear()
			=> _dictionary.Clear();

		public void Store(string key, T @object)
			=> _dictionary[key] = new CacheRecord<T>
			{
				ExpiryDateTimeOffset = DateTimeOffset.UtcNow + _timeSpan,
				Object = @object
			};

		public bool TryGet(string key, out T? @object)
		{
			// Is it in the cache?
			if (_dictionary.TryGetValue(key, out var cacheRecord))
			{
				// Yes.  Has it expired?
				if (cacheRecord.ExpiryDateTimeOffset < DateTimeOffset.UtcNow)
				{
					// Yes.
					// Remove from cache.
					_dictionary.TryRemove(key, out var _);
					@object = default;
					// Not found
					return false;
				}
				// No.  Found.
				@object = cacheRecord.Object;
				return true;
			}

			// No.  Not found.
			@object = default;
			return false;
		}
	}
}
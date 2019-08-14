using System;
using System.Collections.Concurrent;
using PanoramicData.ConnectMagic.Service.Interfaces;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class QueryCache<T> : ICache<T>
	{
		private TimeSpan _timeSpan;
		private ConcurrentDictionary<string, CacheRecord<T>> _dictionary = new ConcurrentDictionary<string, CacheRecord<T>>();

		public QueryCache(TimeSpan timeSpan)
		{
			_timeSpan = timeSpan;
		}

		public void Clear()
		{
			_dictionary.Clear();
		}

		public void Store(string key, T @object)
		{
			_dictionary[key] = new CacheRecord<T>
			{
				ExpiryDateTimeOffset = DateTimeOffset.UtcNow + _timeSpan,
				Object = @object
			};
		}

		public bool TryGet(string key, out T @object)
		{
			// Is it in the cache?
			if (_dictionary.TryGetValue(key, out var cacheRecord))
			{
				// Yes.  Has it expired?
				if(cacheRecord.ExpiryDateTimeOffset < DateTimeOffset.UtcNow)
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
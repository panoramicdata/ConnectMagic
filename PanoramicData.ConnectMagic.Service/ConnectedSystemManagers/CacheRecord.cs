using System;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public class CacheRecord<T>
	{
		public DateTimeOffset ExpiryDateTimeOffset { get; set; }
		public T Object { get; set; }
	}
}
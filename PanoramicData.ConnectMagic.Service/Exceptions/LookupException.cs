using System;

namespace PanoramicData.ConnectMagic.Service.Exceptions
{
	public class LookupException : Exception
	{
		public LookupException() : base()
		{
		}

		public LookupException(string message) : base(message)
		{
		}

		public LookupException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}

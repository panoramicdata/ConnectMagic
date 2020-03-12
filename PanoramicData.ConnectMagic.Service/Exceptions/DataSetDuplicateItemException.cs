using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Models;
using System;

namespace PanoramicData.ConnectMagic.Service.Exceptions
{
	public class DataSetDuplicateItemException : Exception
	{
		public ConnectedSystemDataSet? DataSet { get; }
		public Mapping? Mapping { get; }
		public JObject? Item { get; }

		public DataSetDuplicateItemException(ConnectedSystemDataSet dataSet, Mapping mapping, JObject item)
		{
			DataSet = dataSet;
			Mapping = mapping;
			Item = item;
		}

		public DataSetDuplicateItemException() : base()
		{
		}

		public DataSetDuplicateItemException(string message) : base(message)
		{
		}

		public DataSetDuplicateItemException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}

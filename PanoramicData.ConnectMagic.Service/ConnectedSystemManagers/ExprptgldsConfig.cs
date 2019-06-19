using PanoramicData.ConnectMagic.Service.Exceptions;
using System.Collections.Generic;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class ExprptgldsConfig
	{
		public ExprptgldsConfig(List<string> configItems)
		{
			if (configItems.Count < 2 || !uint.TryParse(configItems[1], out var index))
			{
				throw new ConfigurationException($"An exprptglds query should be in the form: exprptglds/<indexUint>, e.g. exprptglds/3.");
			}

			Index = index;
		}

		public uint Index { get; }
	}
}
using System.Collections.Generic;
using System.Threading;

namespace PanoramicData.ConnectMagic.Service.Models
{
	public class StateItemList : List<StateItem>
	{
		public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);
	}
}
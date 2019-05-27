using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service.Interfaces
{
	public interface IConnectedSystemManager
	{
		Task RefreshDataSetsAsync(CancellationToken cancellationToken);
	}
}

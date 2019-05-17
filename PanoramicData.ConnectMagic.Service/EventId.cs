using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service
{
	internal enum EventId
	{
		UnhandledException = 1,
		Starting = 2,
		Started = 3,
		Stopping = 4,
		Stopped = 5
	}
}

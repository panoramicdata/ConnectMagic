using Microsoft.Extensions.Logging;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicSystems;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service
{
	internal class ConnectedSystemPeriodLoop : LoopInterval
	{
		private readonly IConnectedSystemManager _connectedSystemManager;

		public ConnectedSystemPeriodLoop(
			IConnectedSystemManager connectedSystemManager,
			ILogger<ConnectedSystemPeriodLoop> logger) : base(connectedSystemManager.ConnectedSystem.Name, logger)
		{
			_connectedSystemManager = connectedSystemManager;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			Logger.LogInformation($"{_connectedSystemManager.ConnectedSystem.Type}: Refreshing DataSets");
			_connectedSystemManager.Stats.LastSyncStarted = DateTimeOffset.UtcNow;
			await _connectedSystemManager
				.RefreshDataSetsAsync(cancellationToken)
				.ConfigureAwait(false);
			_connectedSystemManager.Stats.LastSyncCompleted = DateTimeOffset.UtcNow;
		}
	}
}

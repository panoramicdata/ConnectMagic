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
			ILogger<ConnectedSystemPeriodLoop> logger)
			: base(
				  connectedSystemManager.ConnectedSystem.Name,
				  TimeSpan.FromSeconds(connectedSystemManager.ConnectedSystem.LoopPeriodicitySeconds),
				  logger)
		{
			_connectedSystemManager = connectedSystemManager;
		}

		public string ConnectedSystemName => _connectedSystemManager.ConnectedSystem.Name;

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			Logger.LogInformation($"{_connectedSystemManager.ConnectedSystem.Type}: Refreshing DataSets");

			// Note when we last started
			_connectedSystemManager.Stats.LastSyncStarted = DateTimeOffset.UtcNow;

			// Do the work
			await _connectedSystemManager
				.RefreshDataSetsAsync(cancellationToken)
				.ConfigureAwait(false);



			// This should only be updated if the above went as planned
			_connectedSystemManager.Stats.LastSyncCompleted = DateTimeOffset.UtcNow;
		}
	}
}

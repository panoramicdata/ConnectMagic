using Microsoft.Extensions.Logging;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using PanoramicSystems;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service
{
	[DebuggerDisplay("{_connectedSystemManager.ConnectedSystem.Name} ({_connectedSystemManager.ConnectedSystem.LoopPeriodicitySeconds}s)")]
	internal class ConnectedSystemPeriodLoop : LoopInterval
	{
		private readonly IConnectedSystemManager _connectedSystemManager;
		private readonly TimeSpan _hangProtectionTimeout = TimeSpan.FromMinutes(30);
		private ConnectedSystemDataSet? _hangProtectionCurrentDataSet;
		private bool _hangProtectionTimeoutTimeIsElapsed;

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
			Logger.LogInformation("Refreshing DataSets");

			// Note when we last started
			_connectedSystemManager.Stats.LastSyncStarted = DateTimeOffset.UtcNow;

			await _connectedSystemManager
				.ClearCacheAsync()
				.ConfigureAwait(false);

			foreach (var dataSet in _connectedSystemManager.ConnectedSystem.EnabledDatasets)
			{
				_hangProtectionTimeoutTimeIsElapsed = false;
				var stopwatch = Stopwatch.StartNew();
				var timer = new System.Timers.Timer(_hangProtectionTimeout.TotalMilliseconds)
				{
					AutoReset = false,
				};
				timer.Elapsed += Timer_Elapsed;
				timer.Start();
				_hangProtectionCurrentDataSet = dataSet;

				try
				{
					await _connectedSystemManager
						.RefreshDataSetAsync(dataSet, cancellationToken)
						.ConfigureAwait(false);
					if (_hangProtectionTimeoutTimeIsElapsed)
					{
						Logger.LogWarning($"DataSet '{dataSet.Name}' completed after {stopwatch.Elapsed.TotalSeconds:N0}s.");
					}
					else
					{
						Logger.LogDebug($"DataSet '{dataSet.Name}' completed after {stopwatch.Elapsed.TotalSeconds:N0}s.");
					}
				}
				catch
				{
					Logger.LogError($"DataSet '{dataSet.Name}' errored after {stopwatch.Elapsed.TotalSeconds:N0}s.");
					throw;
				}
				finally
				{
					timer.Stop();
				}
			}

			// This should only be updated if the above went as planned
			_connectedSystemManager.Stats.LastSyncCompleted = DateTimeOffset.UtcNow;

			Logger.LogInformation("Refreshing DataSets complete");
		}

		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Logger.LogWarning($"DataSet '{_hangProtectionCurrentDataSet?.Name ?? "?"}' waited more than {_hangProtectionTimeout.TotalSeconds:N0}s to process.");
			_hangProtectionTimeoutTimeIsElapsed = true;
		}
	}
}

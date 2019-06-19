using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PanoramicData.ConnectMagic.Service.Config;
using PanoramicData.ConnectMagic.Service.ConnectedSystemManagers;
using PanoramicData.ConnectMagic.Service.Interfaces;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service
{
	/// <summary>
	/// A ConnectMagic Service
	/// </summary>
	internal partial class ConnectMagicService : ServiceBase, IHostedService
	{
		private const string EventLogSourceName = Program.ProductName;
		private readonly EventLogClient _eventLogClient;
		private readonly ILogger _logger;
		private readonly Configuration _configuration;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private State _state;
		private readonly List<Task> _connectedSystemTasks;
		private readonly FileInfo _stateFileInfo;
		private readonly ILoggerFactory _loggerFactory;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="loggerFactory">The logger factory</param>
		/// <param name="options">The options</param>
		public ConnectMagicService(
			ILoggerFactory loggerFactory,
			IOptions<Configuration> options
			)
		{
			InitializeComponent();

			_logger = loggerFactory.CreateLogger<ConnectMagicService>();
			_configuration = options.Value;
			_cancellationTokenSource = new CancellationTokenSource();

			// Create State object
			_state = _configuration.State;

			// Create task list
			_connectedSystemTasks = new List<Task>();

			// Set up the Event Log
			try
			{
				if (!EventLog.SourceExists(EventLogSourceName))
				{
					EventLog.CreateEventSource(EventLogSourceName, "Application");
				}
				if (EventLog.SourceExists(EventLogSourceName))
				{
					_eventLogClient = new EventLogClient(EventLogSourceName);
				}
			}
			catch
			{
				// This is OK, we just don't have access to the event log
			}

			_stateFileInfo = new FileInfo(_configuration.State.CacheFileName);
			_loggerFactory = loggerFactory;
		}

		/// <summary>
		/// Tasks to perform on start
		/// </summary>
		protected override void OnStart(string[] args)
		{
			_eventLogClient?.WriteToEventLog($"Starting Service Version {ThisAssembly.AssemblyFileVersion}", EventLogEntryType.Information, (int)EventId.Starting);
			StartAsync(default)
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();
			_eventLogClient?.WriteToEventLog("Service Started", EventLogEntryType.Information, (int)EventId.Started);
		}

		/// <summary>
		/// Tasks to perform on stop
		/// </summary>
		protected override void OnStop()
		{
			_eventLogClient?.WriteToEventLog("Service Stopping", EventLogEntryType.Information, (int)EventId.Stopping);
			StopAsync(default)
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();
			_eventLogClient?.WriteToEventLog("Service Stopped", EventLogEntryType.Information, (int)EventId.Stopped);
		}

		/// <summary>
		/// Tasks to perform on shutdown
		/// </summary>
		protected override void OnShutdown()
		{
			_eventLogClient?.WriteToEventLog("Service Stopping due to shutdown", EventLogEntryType.Information, (int)EventId.Stopping);
			StopAsync(default)
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();
			_eventLogClient?.WriteToEventLog("Service Stopped due to shutdown", EventLogEntryType.Information, (int)EventId.Stopped);
		}

		/// <summary>
		/// Starts work
		/// </summary>
		/// <param name="cancellationToken"></param>
		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Starting {Program.ProductName} {ThisAssembly.AssemblyFileVersion}...");

			// Add an unhandled exception handler
			var currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += CurrentDomain_UnhandledException;

			_configuration.Validate();

			_state = _configuration.State;

			// Load FieldSets
			try
			{
				var loadedState = State.FromFile(_stateFileInfo);
				_state.ItemLists = loadedState.ItemLists;
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Could not load state from file: '{e.Message}'");
			}

			_state.ConnectedSystemManagers = _configuration
				.ConnectedSystems
				.Where(cs => cs.IsEnabled)
				.Select(cs => CreateConnectedSystemManager(cs, _state))
				.ToDictionary(csm => csm.ConnectedSystem.Name);

			// Create RemoteSystemTasks
			foreach (var connectedSystemManager in _state.ConnectedSystemManagers.Values)
			{
				// TODO - DA: What to do if one of the connected systems faults? Restart all, or continue to attempt to restart that system?
				_connectedSystemTasks.Add(
					ConnectedSystemTask(connectedSystemManager, _cancellationTokenSource.Token)
					.ContinueWith(faultingTask =>
					{
						var sb = new StringBuilder();
						if (faultingTask.Exception != null)
						{
							foreach (var e in faultingTask.Exception.Flatten().InnerExceptions)
							{
								sb.AppendLine(e.ToString());
							}
						}
						else
						{
							sb.AppendLine("The exception was not set");
						}
						_logger.LogError($"Exception in system task for connected system {connectedSystemManager.ConnectedSystem.Name}: {sb}");
					}, TaskContinuationOptions.OnlyOnFaulted)
				);
			}

			_logger.LogDebug($"Started {Program.ProductName}.");

			return Task.CompletedTask;
		}

		/// <summary>
		/// Performs all activity relating to one system
		/// </summary>
		/// <param name="connectedSystemManager">The connected system manager</param>
		/// <param name="cancellationToken">The cancellation token</param>
		public async Task ConnectedSystemTask(
			IConnectedSystemManager connectedSystemManager,
			CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					_logger.LogInformation($"{connectedSystemManager.ConnectedSystem.Type}: Refreshing DataSets");
					connectedSystemManager.Stats.LastSyncStarted = DateTimeOffset.UtcNow;

					try
					{
						await connectedSystemManager
							.RefreshDataSetsAsync(cancellationToken)
							.ConfigureAwait(false);
					}
					catch (Exception e)
					{
						_logger.LogError(e, $"Unexpected exception in ConnectedSystemTask: {e.Message}");
					}

					connectedSystemManager.Stats.LastSyncCompleted = DateTimeOffset.UtcNow;

					var lastDurationMs = (connectedSystemManager.Stats.LastSyncCompleted - connectedSystemManager.Stats.LastSyncStarted).TotalMilliseconds;

					var desiredDelayMs = (connectedSystemManager.ConnectedSystem.LoopPeriodicitySeconds * 1000) - lastDurationMs;

					var delayMs = (int)Math.Max(1000, desiredDelayMs);

					_logger.LogDebug($"Delaying {delayMs}ms before next sync.");
					await Task
						.Delay(delayMs, cancellationToken)
						.ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException e)
			{
				// This is OK - happens when a termination message is sent via the CancellationToken
				_logger.LogTrace(e, $"Task for connected system '{connectedSystemManager.ConnectedSystem.Name}' canceled.");
			}
		}

		private IConnectedSystemManager CreateConnectedSystemManager(ConnectedSystem connectedSystem, State state)
		{
			IConnectedSystemManager connectedSystemManager;
			switch (connectedSystem.Type)
			{
				case SystemType.AutoTask:
					connectedSystemManager = new AutoTaskConnectedSystemManager(connectedSystem, state, _loggerFactory.CreateLogger<AutoTaskConnectedSystemManager>());
					break;
				case SystemType.Certify:
					connectedSystemManager = new CertifyConnectedSystemManager(connectedSystem, state, _loggerFactory.CreateLogger<CertifyConnectedSystemManager>());
					break;
				case SystemType.SalesForce:
					connectedSystemManager = new SalesForceConnectedSystemManager(connectedSystem, state, _loggerFactory.CreateLogger<SalesForceConnectedSystemManager>());
					break;
				default:
					throw new NotSupportedException($"Unsupported ConnectedSystem type: '{connectedSystem.Type}'");
			}

			return connectedSystemManager;
		}

		/// <summary>
		/// Tasks to perform on stop
		/// </summary>
		/// <param name="cancellationToken"></param>
		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogDebug($"Stopping {Program.ProductName}...");

			// Stop Remote System Tasks
			_cancellationTokenSource.Cancel();

			_logger.LogDebug("Waiting for ConnectedSystemTasks to complete...");
			Task.WaitAll(_connectedSystemTasks.ToArray());

			// Save lastKnownState
			try
			{
				_state.Save(_stateFileInfo);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
			}
			finally
			{
				_logger.LogInformation($"Stopped {Program.ProductName}.");
			}
			return Task.CompletedTask;
		}

		/// <summary>
		///    Writes the nature of the problem to the Windows event log
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
			=> _eventLogClient?.WriteToEventLog(
				e.ExceptionObject.ToString(),
				EventLogEntryType.Error,
				(int)EventId.UnhandledException);
	}
}

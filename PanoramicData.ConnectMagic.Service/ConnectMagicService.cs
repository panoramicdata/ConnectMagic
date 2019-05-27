using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PanoramicData.ConnectMagic.Service.Config;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service
{
	/// <summary>
	/// A ConnectMagic Service
	/// </summary>
	partial class ConnectMagicService : ServiceBase, IHostedService
	{
		private const string EventLogSourceName = Program.ProductName;
		private readonly EventLogClient _eventLogClient;
		private readonly ManualResetEvent _waitManualResetEvent = new ManualResetEvent(false);
		private readonly ILogger _logger;
		private readonly Configuration _configuration;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private State _state;
		private readonly List<Task> _connectedSystemTasks;
		private readonly FileInfo _stateFileInfo;

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
		///     This will block until the event is fired indicated that the engine has stopped
		/// </summary>
		public void WaitUntilStopped() => _waitManualResetEvent.WaitOne();

		/// <summary>
		/// Starts work
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Starting {Program.ProductName}...");
			_waitManualResetEvent.Reset();

			// Add an unhandled exception handler
			var currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += CurrentDomain_UnhandledException;

			_state = _configuration.State;

			// Load lastKnownState
			try
			{
				var loadedState = State.FromFile(_stateFileInfo);
				_state.FieldSets = loadedState.FieldSets;
			}
			catch(Exception e)
			{
				_logger.LogError(e, $"Could not load state from file: '{e.Message}'");
			}

			// Create RemoteSystemTasks
			foreach (var connectedSystem in _configuration.ConnectedSystems)
			{
				_connectedSystemTasks.Add(ConnectedSystemTask(connectedSystem, _cancellationTokenSource.Token));
			}

			_logger.LogDebug($"Started {Program.ProductName}...");
		}

		/// <summary>
		/// Performs all activity relating to one system
		/// </summary>
		/// <param name="connectedSystem">The connected system</param>
		/// <param name="cancellationToken">The cancellation token</param>
		/// <returns></returns>
		public async Task ConnectedSystemTask(
			ConnectedSystem connectedSystem,
			CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					// TODO - work here

					await Task
						.Delay(1000, cancellationToken)
						.ConfigureAwait(false);
				}
			}
			catch(OperationCanceledException e)
			{
				// This is OK - happens when a termination message is sent via the CancellationToken
				_logger.LogTrace(e, $"Task for connected system '{connectedSystem.Name}' canceled.");
			}
		}

		/// <summary>
		/// Tasks to perform on stop
		/// </summary>
		/// <param name="cancellationToken"></param>
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogDebug($"Stopping {Program.ProductName}...");

			// Stop Remote System Tasks
			_cancellationTokenSource.Cancel();

			// Wait for them all to finish
			Task.WaitAll(_connectedSystemTasks.ToArray());

			// Save lastKnownState
			_state
				.Save(_stateFileInfo);

			// Confirm we are finished
			_waitManualResetEvent.Set();

			_logger.LogInformation($"Stopped {nameof(ConnectMagicService)}.");
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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service
{
	partial class ConnectMagicService : ServiceBase, IHostedService
	{
		private const string EventLogSourceName = Program.ProductName;
		private readonly EventLogClient _eventLogClient;
		private readonly ManualResetEvent _waitManualResetEvent = new ManualResetEvent(false);
		private readonly ILogger _logger; 

		public ConnectMagicService(
			ILoggerFactory loggerFactory
			)
		{
			InitializeComponent();

			_logger = loggerFactory.CreateLogger<ConnectMagicService>();

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
		}

		protected override void OnStart(string[] args)
		{
			_eventLogClient?.WriteToEventLog($"Starting Service Version {ThisAssembly.AssemblyFileVersion}", EventLogEntryType.Information, (int)EventId.Starting);
			StartAsync(default)
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();
			_eventLogClient?.WriteToEventLog("Service Started", EventLogEntryType.Information, (int)EventId.Started);
		}

		protected override void OnStop()
		{
			_eventLogClient?.WriteToEventLog("Service Stopping", EventLogEntryType.Information, (int)EventId.Stopping);
			StopAsync(default)
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();
			_eventLogClient?.WriteToEventLog("Service Stopped", EventLogEntryType.Information, (int)EventId.Stopped);
		}

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

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogTrace($"Starting {Program.ProductName}...");
			_waitManualResetEvent.Reset();

			// Add an unhandled exception handler
			var currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += CurrentDomain_UnhandledException;

			// TODO Start engines here

			_logger.LogTrace($"Started {Program.ProductName}...");
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogTrace($"Stopping {Program.ProductName}...");


			// TODO - stop engines here

			_waitManualResetEvent.Set();

			_logger.LogTrace($"Stopped {nameof(ConnectMagicService)}.");
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

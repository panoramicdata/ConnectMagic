using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace PanoramicData.ConnectMagic.Service
{
	public class EventLogClient
	{
		private readonly string _getTempFileName = Path.Combine(Path.GetTempPath(), $"{Program.ProductName} Emergency Log.log");
		private readonly string _sourceName;

		public EventLogClient(
			string sourceName
			)
		{
			_sourceName = sourceName;
		}

		public void WriteToEventLog(string message, EventLogEntryType level, int eventId)
		{
			try
			{
				try
				{
					// Ensure that the source exists
					if (!EventLog.SourceExists(_sourceName))
					{
						EventLog.CreateEventSource(_sourceName, "Application");
					}

					// Write to the event log
					EventLog.WriteEntry(_sourceName, message, level, eventId);
				}
				catch (SecurityException securityException)
				{
					// If this throws, the outer catch will write it to a text file.
					Log.Logger.Information($"Usual security exception on start-up: {securityException.Message}");
				}
				catch (Exception exception)
				{
					// If this throws, the outer catch will write it to a text file.
					Log.Logger.Error($"Could not write to Event Log: {exception}");
				}

				// Write to the trace listener
				try
				{
					var extendedMessage = $"EventLog[{level}/{eventId}] written: {message}";
					switch (level)
					{
						case EventLogEntryType.Error:
							Log.Logger.Error(extendedMessage);
							break;
						case EventLogEntryType.Warning:
							Log.Logger.Warning(extendedMessage);
							break;
						case EventLogEntryType.Information:
							Log.Logger.Information(extendedMessage);
							break;
						case EventLogEntryType.SuccessAudit:
						case EventLogEntryType.FailureAudit:
							Log.Logger.Warning(extendedMessage);
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(level), level, null);
					}
				}
				catch (Exception exception)
				{
					// Could not write to the TraceListener.
					// Log that to the event log.
					EventLog.WriteEntry(_sourceName, $"Could not write to the TraceListener.\r\n{exception}", EventLogEntryType.Error, eventId);
				}
			}
			catch (Exception exception)
			{
				// Could not write to either the trace listener OR the event log
				File.AppendAllText(_getTempFileName, $"{DateTime.Now}: {exception}\r\n");
				File.AppendAllText(_getTempFileName, "__________________________________________________________________________________\r\n");
			}
		}
	}
}

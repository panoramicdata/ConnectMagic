using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PanoramicData.ConnectMagic.Service.Models;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PanoramicData.ConnectMagic.Service
{
	/// <summary>
	/// Main program
	/// </summary>
	public static class Program
	{
		public const string ProductName = "ConnectMagic";
		private static bool IsRunningAsService => Console.IsInputRedirected;

		public async static Task<int> Main()
		{
			try
			{
				var host = new HostBuilder()
					.ConfigureAppConfiguration(BuildConfig)
					.ConfigureLogging(ConfigureLogging)
					.ConfigureServices(ConfigureServices);

				// User interactive?
				if (IsRunningAsService)
				{
					// Update paths to refer to where the service is loaded from for loading config files
					var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
					var pathToContentRoot = Path.GetDirectoryName(pathToExe);
					Directory.SetCurrentDirectory(pathToContentRoot);
					try
					{
						await host
							.ConfigureServices((_, services) => services.AddSingleton<IHostLifetime, ServiceBaseLifetime>())
							.Build()
							.RunAsync()
							.ConfigureAwait(false);
					}
					catch (InvalidOperationException)
					{
						Console.WriteLine("Use --console to run interactively from a command prompt");
						throw;
					}
				}
				else
				{
					// Yes.  Run as a console app
					await host.RunConsoleAsync().ConfigureAwait(false);
				}

				return (int)ExitCode.Ok;
			}
			catch (OperationCanceledException)
			{
				// This is normal for using CTRL+C to exit the run
				Console.WriteLine("** Execution run cancelled - exiting **");
				return (int)ExitCode.RunCancelled;
			}
			catch (Exception ex)
			{
				var dumpPath = Path.GetTempPath();
				var dumpFile = $"{ProductName}-Service-Error-{Guid.NewGuid()}.txt";
				File.WriteAllText(Path.Combine(dumpPath, dumpFile), ex.ToString());
				Console.WriteLine(ex.Message);
				return (int)ExitCode.UnexpectedException;
			}
		}

		private static void BuildConfig(HostBuilderContext context, IConfigurationBuilder configurationBuilder)
		{
			var currentDirectory = Directory.GetCurrentDirectory();
#if DEBUG
			// Use the project folder
			var jsonFilePath = "../../../appsettings.json";
#else
			// Use binaries folder
			var jsonFilePath = "appsettings.json";
#endif
			var jsonFileInfo = new FileInfo(Path.Combine(currentDirectory, jsonFilePath));
			configurationBuilder
			.SetBasePath(currentDirectory)
				.AddJsonFile(jsonFileInfo.FullName)
				.Build();
		}

		private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder loggingBuilder)
		{
			// Change this value to include the source context or not
			const bool IncludeSourceContextAtConsole = true;

			Serilog.Debugging.SelfLog.Enable(msg =>
			{
				Debug.WriteLine(msg);
				Console.Error.WriteLine(msg);
			});

			// Set up SeriLog
			LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
				.WriteTo.Console(
					outputTemplate: IncludeSourceContextAtConsole
					? "[{Timestamp:HH:mm:ss} {Level:u3}] ({SourceContext}){NewLine}                {Message:lj}{NewLine}{Exception}"
					: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
					)
#if DEBUG
				.MinimumLevel.Is(LogEventLevel.Debug)
				.WriteTo.Debug()
#else
				.MinimumLevel.Is(LogEventLevel.Information)
#endif
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);

			// TODO - get from config
			const string slackUrl = null;
			var slackMinimumLevel = LogEventLevel.Warning;
			if(slackUrl != null)
			{
				loggerConfiguration.WriteTo.Slack(
					slackUrl,
					null,
					slackMinimumLevel,
					null);
			}

			Log.Logger = loggerConfiguration.CreateLogger();

			// Enable using Serilog for the ILogger Microsoft extensions
			loggingBuilder
				.AddSerilog(dispose: true);
		}

		private static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
		{
			serviceCollection
				// The Service itself
				.AddSingleton<ConnectMagicService>()
				.AddHostedService<ConnectMagicService>()
				;
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exitCode = 0;
			try
			{
				if (e?.ExceptionObject != null)
				{
					File.WriteAllText($"%TEMP%\\{ProductName}-Service-CrashLog.txt", e.ExceptionObject.ToString());
				}
			}
			catch
			{
				exitCode |= 1;
			}

			Environment.Exit(exitCode);
		}
	}
}
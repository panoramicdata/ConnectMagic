using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PanoramicData.ConnectMagic.Service.Config;
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
		private static string JsonFilePath = "appsettings.json";

		public const string ProductName = "ConnectMagic";
		private static bool IsRunningAsService => Console.IsInputRedirected;

		/// <summary>
		/// Either provide the path to the appsettings.json file as the sole parameter, or 'appsettings.json' the binary folder will be used.
		/// </summary>
		/// <param name="args"></param>
		public async static Task<int> Main(string[] args)
		{
			try
			{
				if (args.Length == 1)
				{
					JsonFilePath = args[0];
				}

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
			catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
			{
				// This is normal for using CTRL+C to exit the run
				Console.WriteLine($"** Execution run canceled - exiting ** : '{e.Message}'");
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
			finally
			{
				// Flush Serilog Sinks
				Log.CloseAndFlush();
			}
		}

		private static void BuildConfig(HostBuilderContext context, IConfigurationBuilder configurationBuilder)
		{
			var currentDirectory = Directory.GetCurrentDirectory();
			var jsonFileInfo = new FileInfo(Path.Combine(currentDirectory, JsonFilePath));
			configurationBuilder
			.SetBasePath(currentDirectory)
				.AddJsonFile(jsonFileInfo.FullName)
				.Build();
		}

		private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder loggingBuilder)
		{
			Serilog.Debugging.SelfLog.Enable(msg =>
				{
					Debug.WriteLine(msg);
					Console.Error.WriteLine(msg);
				}
			);

			// Set up SeriLog
			var config = context.Configuration.GetSection("Logging");
			var loggerConfiguration = new LoggerConfiguration()
				.ReadFrom.Configuration(config)
#if DEBUG
				.WriteTo.Debug()
#endif
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);

			Log.Logger = loggerConfiguration.CreateLogger();

			// Enable using Serilog for the ILogger Microsoft extensions
			loggingBuilder
				.AddSerilog(dispose: true);
		}

		private static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
			=> serviceCollection
				.Configure<Configuration>(hostBuilderContext.Configuration.GetSection("Configuration"))
				// The Service itself
				.AddSingleton<ConnectMagicService>()
				.AddHostedService<ConnectMagicService>()
				;
	}
}
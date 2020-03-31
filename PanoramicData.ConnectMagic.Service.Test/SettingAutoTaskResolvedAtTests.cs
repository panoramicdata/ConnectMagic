using AutoTask.Api;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace PanoramicData.ConnectMagic.Service.Test
{
	public class SettingAutoTaskResolvedAtTests
	{
		private readonly ILogger _logger;

		public SettingAutoTaskResolvedAtTests(ITestOutputHelper testOutputHelper)
		{
			var loggerFactory = LogFactory.Create(testOutputHelper);
			_logger = loggerFactory.CreateLogger(nameof(AutoTaskUdfTests));
		}

		// Uncomment this to run
		[Fact()]
		public async void SetResolvedDateTime_Fails()
		{
			var testCredentials = LoadCredentials();

			const long autoTaskTicketId = 149843;
			const string serviceNowSysId = "8f20e71cdbfa08504d1c16f35b961996";

			var autoTaskClient = new Client(testCredentials.AutoTaskPublicText, testCredentials.AutoTaskPrivateText, testCredentials.AutoTaskIntegrationCode, _logger);
			var autoTaskTicketResponse = await autoTaskClient.GetAllAsync($"<queryxml><entity>Ticket</entity><query><condition operator=\"and\"><field>id<expression op=\"equals\">{autoTaskTicketId}</expression></field></condition></query></queryxml>").ConfigureAwait(false);
			autoTaskTicketResponse.Should().NotBeNull();
			autoTaskTicketResponse.Should().ContainSingle();
			var autoTaskTicket = autoTaskTicketResponse.Cast<Ticket>().First();
			autoTaskTicket.id.Should().Be(autoTaskTicketId);
			//autoTaskTicket.Title.Should().Be("Cisco AP wf-52310-xx2 down");
			var udf = autoTaskTicket.UserDefinedFields.SingleOrDefault(udf => udf.Name == "Customer System Sync Id");
			udf.Should().NotBeNull();
			udf.Value.Should().Be(serviceNowSysId);

			var oldLastActivity = autoTaskTicket.LastActivityDate;

			//autoTaskTicket.ResolvedDateTime = "2020-03-26 13:24:49";
			autoTaskTicket.Status = 29;
			var updateResponse = await autoTaskClient.UpdateAsync(autoTaskTicket).ConfigureAwait(false);

			// Go and get the ticket again and see if ResolvedDateTime is set
			autoTaskTicketResponse = await autoTaskClient.GetAllAsync($"<queryxml><entity>Ticket</entity><query><condition operator=\"and\"><field>id<expression op=\"equals\">{autoTaskTicketId}</expression></field></condition></query></queryxml>").ConfigureAwait(false);
			autoTaskTicket = autoTaskTicketResponse.Cast<Ticket>().First();

			// Problem is that AutoTask IS updating LastActivityDate but NOT setting the ResolvedDateTime
		}

		private static TestCredentials LoadCredentials()
		{
			var jsonString = File.ReadAllText("../../../TestCredentials.json");
			var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip };
			var testCredentials = JsonSerializer.Deserialize<TestCredentials>(jsonString, options);
			testCredentials.Validate();
			return testCredentials;
		}
	}
}

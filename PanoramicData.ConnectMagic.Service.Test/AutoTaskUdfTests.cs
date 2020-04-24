using AutoTask.Api;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit.Abstractions;

namespace PanoramicData.ConnectMagic.Service.Test
{
	public class AutoTaskUdfTests
	{
		private readonly ILogger _logger;

		public AutoTaskUdfTests(ITestOutputHelper testOutputHelper)
		{
			var loggerFactory = LogFactory.Create(testOutputHelper);
			_logger = loggerFactory.CreateLogger(nameof(AutoTaskUdfTests));
		}

		// Uncomment this to set AutoTask UDFs to a serviceNowSysId
		// [Theory]
		// [InlineData(141658, "05716009dbbec8504d1c16f35b9619ae")]
		// [InlineData(141868, "c1a94982db764490d5ed3ce339961913")]
		public async void SetCustomerSystemSyncId(long autoTaskTicketId, string serviceNowSysId)
		{
			var testCredentials = LoadCredentials();

			var autoTaskClient = new Client(testCredentials.AutoTaskPublicText, testCredentials.AutoTaskPrivateText, testCredentials.AutoTaskIntegrationCode, _logger);
			var autoTaskTicketResponse = await autoTaskClient.GetAllAsync($"<queryxml><entity>Ticket</entity><query><condition operator=\"and\"><field>id<expression op=\"equals\">{autoTaskTicketId}</expression></field></condition></query></queryxml>").ConfigureAwait(false);
			autoTaskTicketResponse.Should().NotBeNull();
			autoTaskTicketResponse.Should().ContainSingle();
			var autoTaskTicket = autoTaskTicketResponse.Cast<Ticket>().First();
			autoTaskTicket.id.Should().Be(autoTaskTicketId);
			//autoTaskTicket.Title.Should().Be("Cisco AP wf-52310-xx2 down");
			var udf = autoTaskTicket.UserDefinedFields.SingleOrDefault(udf => udf.Name == "Customer System Sync Id");
			udf.Should().NotBeNull();
			udf.Value = serviceNowSysId;
			await autoTaskClient.UpdateAsync(autoTaskTicket).ConfigureAwait(false);
		}

		private static TestCredentials LoadCredentials()
		{
			var jsonString = File.ReadAllText("../../../TestCredentials.json");
			var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip };
			var testCredentials = JsonSerializer.Deserialize<TestCredentials>(jsonString, options);
			testCredentials.Validate();
			return testCredentials;
		}

		//[Fact]
		public async void DateTest()
		{
			var autoTaskTicketId = 149891;

			var testCredentials = LoadCredentials();

			var autoTaskClient = new Client(testCredentials.AutoTaskPublicText, testCredentials.AutoTaskPrivateText, testCredentials.AutoTaskIntegrationCode, _logger);
			var autoTaskTicketResponse = await autoTaskClient.GetAllAsync($"<queryxml><entity>Ticket</entity><query><condition operator=\"and\"><field>id<expression op=\"equals\">{autoTaskTicketId}</expression></field></condition></query></queryxml>").ConfigureAwait(false);
		}

		//[Fact]
		public async void DateTest2()
		{
			var autoTaskTicketNoteId = 31936570;

			var testCredentials = LoadCredentials();

			var autoTaskClient = new Client(testCredentials.AutoTaskPublicText, testCredentials.AutoTaskPrivateText, testCredentials.AutoTaskIntegrationCode, _logger);
			var autoTaskTicketNoteResponse = await autoTaskClient.GetAllAsync($"<queryxml><entity>TicketNote</entity><query><condition operator=\"and\"><field>id<expression op=\"equals\">{autoTaskTicketNoteId}</expression></field></condition></query></queryxml>").ConfigureAwait(false);
		}
	}
}

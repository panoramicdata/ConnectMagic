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

		// [Fact(Skip = "Uncomment this to set AutoTask UDFs to a serviceNowSysId")]
		public async void SetCustomerSystemSyncId()
		{
			var testCredentials = LoadCredentials();

			const long autoTaskTicketId = 141651;
			const string serviceNowSysId = "6dc72bb9db7a0490d5ed3ce3399619a9";

			var autoTaskClient = new Client(testCredentials.AutoTaskPublicText, testCredentials.AutoTaskPrivateText, testCredentials.AutoTaskPrivateText, _logger);
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
	}
}

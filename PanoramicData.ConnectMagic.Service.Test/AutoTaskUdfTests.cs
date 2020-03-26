using AutoTask.Api;
using AutoTask.Api.Config;
using AutoTask.Api.Filters;
using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace PanoramicData.ConnectMagic.Service.Test
{
	public class AutoTaskUdfTests
	{
		[Fact]
		public async void SetCustomerSystemSyncId()
		{
			var testCredentials = LoadCredentials();

			const long autoTaskTicketId = 141651;
			const string serviceNowSysId = "6dc72bb9db7a0490d5ed3ce3399619a9";
			var autoTaskClient = new AutoTaskClient(new AutoTaskConfiguration
			{
				Username = testCredentials.AutoTaskPublicText,
				Password = testCredentials.AutoTaskPrivateText
			});
			var autoTaskTicketResponse = await autoTaskClient.GetAsync<Ticket>(new Filter { Items = new List<FilterItem> { new FilterItem { Field = nameof(Ticket.id), Operator = Operator.Equals, Value = autoTaskTicketId.ToString() } } });
			autoTaskTicketResponse.Should().NotBeNull();
			autoTaskTicketResponse.Should().ContainSingle();
			var autoTaskTicket = autoTaskTicketResponse[0];
			autoTaskTicket.id.Should().Be(autoTaskTicketId);
			autoTaskTicket.Title.Should().Be("Cisco AP wf-52310-xx2 down");
			var udf = autoTaskTicket.UserDefinedFields.SingleOrDefault(udf => udf.Name == "Customer System Sync Id");
			udf.Should().NotBeNull();
			udf.Value = serviceNowSysId;
			//await autoTaskClient.
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

using FluentAssertions;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace PanoramicData.ConnectMagic.Service.Test.ConnectedSystemManagerBaseTests
{
	public class ProcessConnectedSystemItemTests
	{
		private readonly ITestOutputHelper _outputHelper;

		public ProcessConnectedSystemItemTests(ITestOutputHelper outputHelper)
		{
			_outputHelper = outputHelper;
		}

		[Fact]
		public void Test()
		{
			var testConnectedSystemManger = new TestConnectedSystemManager(
				new ConnectedSystem(),
				new State(),
				_outputHelper.BuildLoggerFor<TestConnectedSystemManager>()
				);

			var dataSet = new ConnectedSystemDataSet
			{
				Name = "TestDataSet",
				StateDataSetName = "TestDataSet",
				Mappings = new List<Mapping>
				{
					new Mapping
					{
						Direction = SyncDirection.Join,
						SystemExpression = "ConnectedSystemKeyA",
						StateExpression = "StateKeyA"
					}
				}
			};
			var connectedSystemItems = new List<JObject>();
			var actionList = testConnectedSystemManger.TestProcessConnectedSystemItems(dataSet, connectedSystemItems);
			actionList.Should().NotBeNull();
			actionList.Should().BeEmpty();
		}
	}
}

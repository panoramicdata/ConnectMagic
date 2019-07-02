using FluentAssertions;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;
using System.Linq;
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
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanCreate = true, CanDelete = true, CanUpdate = true, CanWrite = true }
			};
			var state = new State();
			state.ItemLists["TestDataSet"] = new ItemList
			{
				new JObject(new JProperty("Id", "ExistingKey1"), new JProperty("FullName", "Sarah Jane"), new JProperty("Description", "Is lovely")),
				new JObject(new JProperty("Id", "Key1"), new JProperty("FullName", "Bob1 Smith1"),  new JProperty("Description", "OldDescription1"))
			};

			var dataSet = new ConnectedSystemDataSet
			{
				CreateDeleteDirection = SyncDirection.In,
				Name = "DataSet1",
				StateDataSetName = "TestDataSet",
				Mappings = new List<Mapping>
				{
					new Mapping
					{
						Direction = SyncDirection.Join,
						SystemExpression = "ConnectedSystemKey",
						StateExpression = "Id"
					},
					new Mapping
					{
						Direction = SyncDirection.In,
						SystemExpression = "ConnectedSystemKey",
						StateExpression = "Id"
					},
					new Mapping
					{
						Direction = SyncDirection.In,
						SystemExpression = "FirstName +' ' + LastName",
						StateExpression = "FullName"
					},
					new Mapping
					{
						Direction = SyncDirection.In,
						SystemExpression = "Description",
						StateExpression = "Description"
					}
				},
				Permissions = new Permissions { CanCreate = true, CanDelete = true, CanUpdate = true, CanWrite = true }
			};

			var testConnectedSystemManger = new TestConnectedSystemManager(
				connectedSystem,
				state,
				_outputHelper.BuildLoggerFor<TestConnectedSystemManager>()
			);

			var actionList = testConnectedSystemManger.TestProcessConnectedSystemItems(dataSet, testConnectedSystemManger.Items[dataSet.Name]);
			actionList.Should().NotBeNullOrEmpty();
			actionList.Should().HaveCount(6);
			actionList.All(a => a.Permission == ConnectedSystemManagers.DataSetPermission.Allowed).Should().BeTrue();
			actionList.Where(a => a.Type == ConnectedSystemManagers.SyncActionType.Create).Should().HaveCount(4);
			actionList.Where(a => a.Type == ConnectedSystemManagers.SyncActionType.Update).Should().HaveCount(1);
			actionList.Where(a => a.Type == ConnectedSystemManagers.SyncActionType.Delete).Should().HaveCount(1);

			// Process a second time - should be in stable state
			actionList = testConnectedSystemManger.TestProcessConnectedSystemItems(dataSet, testConnectedSystemManger.Items[dataSet.Name]);
			actionList.Should().NotBeNullOrEmpty();
			actionList.Should().HaveCount(5);
			actionList.All(a => a.Permission == ConnectedSystemManagers.DataSetPermission.Allowed).Should().BeTrue();
			actionList.All(a => a.Type == ConnectedSystemManagers.SyncActionType.AlreadyInSync).Should().BeTrue();
		}
	}
}

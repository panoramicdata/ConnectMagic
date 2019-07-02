using FluentAssertions;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.ConnectedSystemManagers;
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
			// All actions should have been creates
			foreach (var action in actionList)
			{
				action.Type.Should().Be(SyncActionType.Create);
				action.Permission.Should().Be(DataSetPermission.Allowed);
			}

			actionList = testConnectedSystemManger.TestProcessConnectedSystemItems(dataSet, testConnectedSystemManger.Items[dataSet.Name]);
			actionList.Should().NotBeNullOrEmpty();
			// All actions should have been none
			foreach (var action in actionList)
			{
				action.Type.Should().Be(SyncActionType.AlreadyInSync);
				action.Permission.Should().Be(DataSetPermission.Allowed);
			}
		}
	}
}

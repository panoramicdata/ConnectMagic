using FluentAssertions;
using PanoramicData.ConnectMagic.Service.ConnectedSystemManagers;
using PanoramicData.ConnectMagic.Service.Models;
using Xunit;

namespace PanoramicData.ConnectMagic.Service.Test.ConnectedSystemManagerBaseTests
{
	public class DeterminePermissionTests
	{
		[Fact]
		public void DeterminePermission_AllAllowed_Allowed()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.Allowed);
		}

		// ******* ConnectedSystem **********

		[Fact]
		public void DeterminePermission_ConnectedSystem_Create_WriteFalse_DeniedAtConnectedSystem()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = false, CanCreate = true, CanUpdate = true, CanDelete = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.DeniedAtConnectedSystem);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.DeniedAtConnectedSystem);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.DeniedAtConnectedSystem);
		}

		[Fact]
		public void DeterminePermission_ConnectedSystem_Create_WriteFalse_DeniedAtConnectedSystemDataSet()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = false, CanCreate = true, CanUpdate = true, CanDelete = true }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.DeniedAtConnectedSystemDataSet);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.DeniedAtConnectedSystemDataSet);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.DeniedAtConnectedSystemDataSet);
		}

		[Fact]
		public void DeterminePermission_ConnectedSystem_Create_False_DeniedAtConnectedSystem()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = false, CanDelete = false }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.DeniedAtConnectedSystem);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.DeniedAtConnectedSystem);
		}

		[Fact]
		public void DeterminePermission_ConnectedSystem_CreateUpdateAllowed()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = false }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.DeniedAtConnectedSystem);
		}

		[Fact]
		public void DeterminePermission_ConnectedSystem_CreateDeleteAllowed()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = false, CanDelete = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.DeniedAtConnectedSystem);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.Allowed);
		}

		[Fact]
		public void DeterminePermission_ConnectedSystemDataSet_Create_False_DeniedAtConnectedSystem()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = false, CanDelete = false }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.DeniedAtConnectedSystemDataSet);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.DeniedAtConnectedSystemDataSet);
		}

		[Fact]
		public void DeterminePermission_ConnectedSystemDataSet_CreateUpdateAllowed()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = false }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.DeniedAtConnectedSystemDataSet);
		}

		[Fact]
		public void DeterminePermission_ConnectedSystemDataSet_CreateDeleteAllowed()
		{
			var connectedSystem = new ConnectedSystem
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = true, CanDelete = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreate = true, CanUpdate = false, CanDelete = true }
			};

			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Create).Should().Be(DataSetPermission.Allowed);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Update).Should().Be(DataSetPermission.DeniedAtConnectedSystemDataSet);
			ConnectedSystemManagerBase.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.Delete).Should().Be(DataSetPermission.Allowed);
		}
	}
}

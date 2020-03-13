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
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));
		}

		// ******* ConnectedSystem **********

		[Fact]
		public void DeterminePermission_ConnectedSystem_Create_WriteFalse_DeniedAtConnectedSystem()
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = false, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.WriteDisabledAtConnectedSystem));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.WriteDisabledAtConnectedSystem, DataSetPermission.WriteDisabledAtConnectedSystem));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.WriteDisabledAtConnectedSystem));
		}

		[Fact]
		public void DeterminePermission_ConnectedSystem_Create_WriteFalse_DeniedAtConnectedSystemDataSet()
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = false, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.WriteDisabledAtConnectedSystemDataSet));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.WriteDisabledAtConnectedSystemDataSet, DataSetPermission.WriteDisabledAtConnectedSystemDataSet));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.WriteDisabledAtConnectedSystemDataSet));
		}

		[Fact]
		public void DeterminePermission_ConnectedSystem_Create_False_DeniedAtConnectedSystem()
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = false, CanDeleteIn = false, CanCreateOut = true, CanUpdateOut = false, CanDeleteOut = false }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystem, DataSetPermission.DeniedAtConnectedSystem));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.DeniedAtConnectedSystem));
		}

		[Fact]
		public void DeterminePermission_ConnectedSystem_CreateUpdateAllowed()
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = false, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = false }
			};

			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.DeniedAtConnectedSystem));
		}

		[Fact]
		public void DeterminePermission_ConnectedSystem_CreateDeleteAllowed()
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = false, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = false, CanDeleteOut = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystem, DataSetPermission.DeniedAtConnectedSystem));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));
		}

		[Fact]
		public void DeterminePermission_ConnectedSystemDataSet_Create_False_DeniedAtConnectedSystem()
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = false, CanDeleteIn = false, CanCreateOut = true, CanUpdateOut = false, CanDeleteOut = false }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystemDataSet, DataSetPermission.DeniedAtConnectedSystemDataSet));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.DeniedAtConnectedSystemDataSet));
		}

		[Fact]
		public void DeterminePermission_ConnectedSystemDataSet_CreateUpdateAllowed()
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = false, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = false }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.DeniedAtConnectedSystemDataSet));
		}

		[Fact]
		public void DeterminePermission_ConnectedSystemDataSet_CreateDeleteAllowed()
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test")
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true }
			};
			var connectedSystemDataSet = new ConnectedSystemDataSet
			{
				Permissions = new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = false, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = false, CanDeleteOut = true }
			};

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.CreateSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.UpdateBoth)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystemDataSet, DataSetPermission.DeniedAtConnectedSystemDataSet));

			ConnectedSystemManagerBase
				.DeterminePermission(connectedSystem, connectedSystemDataSet, SyncActionType.DeleteSystem)
				.Should()
				.BeEquivalentTo(new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed));
		}
	}
}

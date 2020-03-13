using FluentAssertions;
using PanoramicData.ConnectMagic.Service.ConnectedSystemManagers;
using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;
using Xunit;

namespace PanoramicData.ConnectMagic.Service.Test.ConnectedSystemManagerBaseTests
{
	public class DeterminePermissionTests
	{
		private void AssertCorrectPermissions(
			Permissions connectedSystemPermissions,
			Permissions connectedSystemDataSetPermissions,
			Dictionary<SyncActionType, DirectionPermissions> expectedActionTypePermissions)
		{
			var connectedSystem = new ConnectedSystem(SystemType.Test, "Test") { Permissions = connectedSystemPermissions };
			var connectedSystemDataSet = new ConnectedSystemDataSet { Permissions = connectedSystemDataSetPermissions };

			foreach (var item in expectedActionTypePermissions)
			{
				ConnectedSystemManagerBase
					.DeterminePermission(connectedSystem, connectedSystemDataSet, item.Key)
					.Should()
					.BeEquivalentTo(item.Value);
			}
		}

		[Fact]
		public void AllAllowed()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.Allowed) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) }
					}
				);

		[Fact]
		public void WriteDisabledAtConnectedSystemLevel()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = false, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.WriteDisabledAtConnectedSystem) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.WriteDisabledAtConnectedSystem, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.WriteDisabledAtConnectedSystem, DataSetPermission.WriteDisabledAtConnectedSystem) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.WriteDisabledAtConnectedSystem) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.WriteDisabledAtConnectedSystem, DataSetPermission.InvalidOperation) }
					}
				);

		[Fact]
		public void WriteDisabledAtConnectedSystemDataSetLevel()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Permissions { CanWrite = false, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.WriteDisabledAtConnectedSystemDataSet) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.WriteDisabledAtConnectedSystemDataSet, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.WriteDisabledAtConnectedSystemDataSet, DataSetPermission.WriteDisabledAtConnectedSystemDataSet) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.WriteDisabledAtConnectedSystemDataSet) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.WriteDisabledAtConnectedSystemDataSet, DataSetPermission.InvalidOperation) }
					}
				);

		[Fact]
		public void CreateOnlyAtConnectedSystemLevel()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = false, CanDeleteIn = false, CanCreateOut = true, CanUpdateOut = false, CanDeleteOut = false },
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystem, DataSetPermission.DeniedAtConnectedSystem) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.DeniedAtConnectedSystem) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystem, DataSetPermission.InvalidOperation) }
					}
				);

		[Fact]
		public void CreateAndUpdateOnlyAtConnectedSystemLevel()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = false, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = false },
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.Allowed) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.DeniedAtConnectedSystem) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystem, DataSetPermission.InvalidOperation) }
					}
				);

		[Fact]
		public void CreateAndDeleteOnlyAtConnectedSystemLevel()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = false, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = false, CanDeleteOut = true },
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystem, DataSetPermission.DeniedAtConnectedSystem) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) }
					}
				);

		[Fact]
		public void CreateOnlyAtConnectedSystemDataSetLevel()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = false, CanDeleteIn = false, CanCreateOut = true, CanUpdateOut = false, CanDeleteOut = false },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystemDataSet, DataSetPermission.DeniedAtConnectedSystemDataSet) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.DeniedAtConnectedSystemDataSet) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystemDataSet, DataSetPermission.InvalidOperation) }
					}
				);

		[Fact]
		public void CreateAndUpdateOnlyAtConnectedSystemDataSetLevel()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = false, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = false },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.Allowed) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.DeniedAtConnectedSystemDataSet) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystemDataSet, DataSetPermission.InvalidOperation) }
					}
				);

		[Fact]
		public void CreateAndDeleteOnlyAtConnectedSystemDataSetLevel()
			=> AssertCorrectPermissions(
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = true, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = true, CanDeleteOut = true },
					new Permissions { CanWrite = true, CanCreateIn = true, CanUpdateIn = false, CanDeleteIn = true, CanCreateOut = true, CanUpdateOut = false, CanDeleteOut = true },
					new Dictionary<SyncActionType, DirectionPermissions> {
						{ SyncActionType.CreateSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.CreateState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) },
						{ SyncActionType.UpdateBoth , new DirectionPermissions(DataSetPermission.DeniedAtConnectedSystemDataSet, DataSetPermission.DeniedAtConnectedSystemDataSet) },
						{ SyncActionType.DeleteSystem , new DirectionPermissions(DataSetPermission.InvalidOperation, DataSetPermission.Allowed) },
						{ SyncActionType.DeleteState , new DirectionPermissions(DataSetPermission.Allowed, DataSetPermission.InvalidOperation) }
					}
				);
	}
}

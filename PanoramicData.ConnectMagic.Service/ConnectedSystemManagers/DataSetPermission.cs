namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public enum DataSetPermission
	{
		Unknown = 0,
		Allowed,
		DeniedAtConnectedSystem,
		DeniedAtConnectedSystemDataSet,
		DeniedAllConnectedSystemsNotYetLoaded,
		WriteDisabledAtConnectedSystemDataSet,
		WriteDisabledAtConnectedSystem,
		InvalidOperation
	}
}
namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal enum DataSetPermission
	{
		Unknown = 0,
		Allowed,
		DeniedAtConnectedSystem,
		DeniedAtConnectedSystemDataSet,
		DeniedAllConnectedSystemsNotYetLoaded
	}
}
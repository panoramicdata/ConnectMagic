namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public enum SyncActionType
	{
		None = 0,
		CreateState,
		CreateSystem,
		DeleteState,
		DeleteSystem,
		UpdateBoth,
		RemedyMultipleStateItemsMatchedAConnectedSystemItem,
		AlreadyInSync,
		RemedyMultipleConnectedSystemItemsWithSameJoinValue,
		RemedyErrorDuringProcessing,
	}
}
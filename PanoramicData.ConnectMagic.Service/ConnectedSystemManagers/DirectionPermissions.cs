namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public struct DirectionPermissions
	{
		public DirectionPermissions(DataSetPermission @in, DataSetPermission @out)
		{
			In = @in;
			Out = @out;
		}

		public DataSetPermission In { get; }

		public DataSetPermission Out { get; }

		public override string ToString()
			=> $"In:{In} Out:{Out}";
	}
}
namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public class FunctionChange : Change
	{
		public FunctionChange(string function)
		{
			Function = function;
		}

		public string Function { get; }

		public override string ToString() => $"Function: '{Function}'";
	}
}
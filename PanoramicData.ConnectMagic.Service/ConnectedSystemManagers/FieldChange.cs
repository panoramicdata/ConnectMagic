using Newtonsoft.Json.Linq;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public class FieldChange : Change
	{
		public FieldChange(string field, JToken? oldValue, JToken? newValue)
		{
			Field = field;
			OldValue = oldValue?.ToString() ?? "<NULL>";
			NewValue = newValue?.ToString() ?? "<NULL>";
		}

		public string Field { get; }

		public string OldValue { get; }

		public string NewValue { get; }

		public override string ToString() => $"'{Field}': '{OldValue}' => '{NewValue}'";
	}
}
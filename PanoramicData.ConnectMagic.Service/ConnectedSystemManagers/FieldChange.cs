using Newtonsoft.Json.Linq;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	public class FieldChange : Change
	{
		public FieldChange(string field, JToken? oldValue, JToken? newValue)
		{
			Field = field;
			OldValue = oldValue;
			NewValue = newValue;
		}

		public string Field { get; }

		public JToken? OldValue { get; }

		public JToken? NewValue { get; }

		public override string ToString() => $"'{Field}': <{OldValue?.Type}>'{OldValue?.ToString() ?? "<NULL>"}' => <{NewValue?.Type}>'{NewValue?.ToString() ?? "<NULL>"}'";
	}
}
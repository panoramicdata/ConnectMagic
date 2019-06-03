using System.Text.RegularExpressions;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class SubstitutionString
	{
		private static readonly Regex tokenRegex = new Regex("{{(.+?):(.+?)}}");

		private readonly string _inputText;

		public SubstitutionString(string inputText)
		{
			_inputText = inputText;
		}

		public override string ToString()
		{
			var result = _inputText;
			var tokenMatches = tokenRegex.Matches(_inputText);
			foreach (var tokenMatch in tokenMatches)
			{
				result = result.Replace(tokenMatch.ToString(), "XXX");
			}

			return result;
		}
	}
}
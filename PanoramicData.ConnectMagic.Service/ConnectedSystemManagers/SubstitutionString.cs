using System.Text.RegularExpressions;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class SubstitutionString
	{
		private static readonly Regex tokenRegex = new Regex("{{(.+?):(.+?)}}");

		private string inputText;

		public SubstitutionString(string inputText)
		{
			this.inputText = inputText;
		}

		public override string ToString()
		{
			var result = inputText;
			var tokenMatches = tokenRegex.Matches(inputText);
			foreach (var tokenMatch in tokenMatches)
			{
				result = result.Replace(tokenMatch.ToString(), "XXX");
			}

			return result;
		}
	}
}
using PanoramicData.ConnectMagic.Service.Ncalc;
using System;
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

		/// <summary>
		/// Always resturns a string.  If the NCalc expression returns a null, returns an empty string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var result = _inputText;
			var tokenMatches = tokenRegex.Matches(_inputText);
			foreach (Match tokenMatch in tokenMatches)
			{
				var tokenType = tokenMatch.Groups[1].ToString();
				var expressionText = tokenMatch.Groups[2].ToString();
				string? evaluationResult;
				switch (tokenType)
				{
					case "ncalc":
						var connectMagicExpression = new ConnectMagicExpression(expressionText);
						evaluationResult = connectMagicExpression.Evaluate()?.ToString() ?? string.Empty;
						break;
					default:
						throw new NotSupportedException($"Unsupported token type {tokenType}");
				}
				result = result.Replace(tokenMatch.ToString(), evaluationResult);
			}
			return result;
		}
	}
}
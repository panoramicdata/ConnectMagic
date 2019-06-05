using NCalc;
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

		public override string ToString()
		{
			var result = _inputText;
			var tokenMatches = tokenRegex.Matches(_inputText);
			foreach (Match tokenMatch in tokenMatches)
			{
				var tokenType = tokenMatch.Groups[1].ToString();
				var expressionText = tokenMatch.Groups[2].ToString();
				string evaluationResult;
				switch (tokenType)
				{
					case "ncalc":
						var nCalcExpression = new Expression(expressionText);
						nCalcExpression.EvaluateFunction += NCalcExtensions.NCalcExtensions.Extend;
						evaluationResult = nCalcExpression.Evaluate().ToString();
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
using PanoramicData.ConnectMagic.Service.Exceptions;
using System;
using System.Text.RegularExpressions;

namespace PanoramicData.ConnectMagic.Service.ConnectedSystemManagers
{
	internal class Filter
	{
		private static readonly Regex Regex = new Regex("^(?<name>.+?)(?<operator>>=|<=|!=|>|<|==)(?<value>.*?)$");

		public Filter(string filter)
		{
			var match = Regex.Match(filter);
			// Valid filter?
			if (!match.Success)
			{
				// No
				throw new ConfigurationException($"Invalid filter '{filter}'");
			}
			// Yes

			Name = match.Groups["name"].Value;
			Operator = GetOperator(match.Groups["operator"].Value);
			Value = match.Groups["value"].Value;
		}

		private Operator GetOperator(string operatorString)
			=> operatorString switch
			{
				"<" => Operator.LessThan,
				"<=" => Operator.LessThanOrEquals,
				">" => Operator.GreaterThan,
				">=" => Operator.GreaterThanOrEquals,
				"!=" => Operator.NotEquals,
				"==" => Operator.Equals,
				_ => throw new NotSupportedException($"Operator '{operatorString}' not supported."),
			};

		public string Name { get; }

		public string Value { get; }

		public Operator Operator { get; }
	}
}
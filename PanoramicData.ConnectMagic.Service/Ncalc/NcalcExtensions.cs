using NCalc;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Text;

namespace PanoramicData.ConnectMagic.Service.Ncalc
{
	public static class NcalcExtensions
	{
#pragma warning disable RCS1224 // Make method an extension method.
		public static void Extend(string functionName, FunctionArgs functionArgs)
#pragma warning restore RCS1224 // Make method an extension method.
		{
			switch (functionName)
			{
				case "upperCaseEmail":
				{
					const int upperCaseEmailParameterCount = 1;
					if (functionArgs.Parameters.Length != upperCaseEmailParameterCount)
					{
						throw new ArgumentException($"Expected {upperCaseEmailParameterCount} arguments");
					}
					if (!(functionArgs.Parameters[0].Evaluate() is string inputEmailAddress))
					{
						throw new ArgumentException("Expected first argument to be an email address.");
					}

					// This function uppercases the user's first and last names
					var stringBuilder = new StringBuilder();
					var lastCharacterIndicatesUpperCasing = true;
					var upperCasingIsActive = true; // Until the @ symbol is found
					foreach (var @char in inputEmailAddress)
					{
						stringBuilder.Append(upperCasingIsActive && lastCharacterIndicatesUpperCasing
							? @char.ToString().ToUpperInvariant()
							: @char.ToString()
							);

						// When we see a '@', disable capitalization
						switch (@char)
						{
							case '@':
								lastCharacterIndicatesUpperCasing = false;
								upperCasingIsActive = false;
								break;
							case '.':
							case '-':
								lastCharacterIndicatesUpperCasing = true;
								break;
							default:
								lastCharacterIndicatesUpperCasing = false;
								break;
						}
					}

					functionArgs.Result = stringBuilder.ToString();
					return;
				}
				case "cast":
				{
					const int castParameterCount = 2;
					if (functionArgs.Parameters.Length != castParameterCount)
					{
						throw new ArgumentException($"Expected {castParameterCount} arguments");
					}
					var inputObject = functionArgs.Parameters[0].Evaluate();
					if (!(functionArgs.Parameters[1].Evaluate() is string castTypeString))
					{
						throw new ArgumentException("Expected second argument to be a string.");
					}
					var castType = Type.GetType(castTypeString);
					if (castType == null)
					{
						throw new ArgumentException("Expected second argument to be a valid .NET type e.g. System.Decimal.");
					}
					var result = Convert.ChangeType(inputObject, castType);
					functionArgs.Result = result;
					return;
				}
				//case "objectToDateTime":
				//{
				//	const int toDateTimeParameterCount = 2;
				//	if (functionArgs.Parameters.Length != toDateTimeParameterCount)
				//	{
				//		throw new ArgumentException($"Expected {toDateTimeParameterCount} arguments");
				//	}

				//	var inputToString = functionArgs.Parameters[0].Evaluate()?.ToString() ?? string.Empty;

				//	if (!(functionArgs.Parameters[1].Evaluate() is string formatString))
				//	{
				//		throw new ArgumentException("Expected second argument to be a string.");
				//	}
				//	if (!DateTime.TryParseExact(inputToString, formatString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var outputDateTime))
				//	{
				//		throw new ArgumentException("Input string did not match expected format.");
				//	}
				//	functionArgs.Result = outputDateTime;
				//	return;
				//}
				case "jobject":
				{
					const int jobjectParameterCount = 1;
					if (functionArgs.Parameters.Length != jobjectParameterCount)
					{
						throw new ArgumentException($"Expected {jobjectParameterCount} arguments");
					}
					if (!(functionArgs.Parameters[0].Evaluate() is string inputJobjectString))
					{
						throw new ArgumentException("Expected first argument to be a string.");
					}
					JObject jObject;
					try
					{
						jObject = JObject.Parse(inputJobjectString);
					}
					catch (Exception e)
					{
						throw new ArgumentException($"Could not parse string into a JSON object: '{e.Message}'.");
					}

					functionArgs.Result = jObject;
					return;
				}
				case "queryLookup":
				{
					const int parameterCount = 5;
					if (functionArgs.Parameters.Length != parameterCount)
					{
						throw new ArgumentException($"Expected {parameterCount} arguments in queryLookup.");
					}

					var argumentIndex = 0;
					if (!(functionArgs.Parameters[argumentIndex++].Evaluate() is State state))
					{
						throw new ArgumentException($"Expected argument {argumentIndex} to be the State.");
					}

					if (!(functionArgs.Parameters[argumentIndex++].Evaluate() is string queryLookupConnectedSystemName))
					{
						throw new ArgumentException($"Expected argument {argumentIndex} to be the name of a Connected System.");
					}

					if (!(functionArgs.Parameters[argumentIndex++].Evaluate() is string queryLookupType))
					{
						throw new ArgumentException($"Expected argument {argumentIndex} to be a query type.");
					}

					if (!(functionArgs.Parameters[argumentIndex++].Evaluate() is string queryLookupQuery))
					{
						throw new ArgumentException($"Expected argument {argumentIndex} to be a query string.");
					}

					if (!(functionArgs.Parameters[argumentIndex++].Evaluate() is string queryLookupField))
					{
						throw new ArgumentException($"Expected argument {argumentIndex} to be a field name.");
					}
					// We now have all three parameters

					functionArgs.Result = state.QueryLookupAsync
						(queryLookupConnectedSystemName, new QueryConfig { Type = queryLookupType, Query = queryLookupQuery }, queryLookupField, default).GetAwaiter().GetResult();
					return;
				}
			}
		}
	}
}

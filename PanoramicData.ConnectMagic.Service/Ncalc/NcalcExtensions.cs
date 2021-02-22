using NCalc;
using Newtonsoft.Json.Linq;
using PanoramicData.ConnectMagic.Service.ConnectedSystemManagers;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PanoramicData.ConnectMagic.Service.Ncalc
{
	public static class NcalcExtensions
	{
		public const string QueryLookupFunctionName = "queryLookup";
		public const string UppercaseEmailFunctionName = "upperCaseEmail";
		public const string CastFunctionName = "cast";
		public const string JObjectFunctionName = "jobject";
		public const string StateLookupFunctionName = "stateLookup";
		public const string StateContainsFunctionName = "stateContains";
		public const string PatchFunctionName = "systemPatch";

		public static void Extend(string functionName, FunctionArgs functionArgs)
		{
			switch (functionName)
			{
				case UppercaseEmailFunctionName:
					{
						const int upperCaseEmailParameterCount = 1;
						if (functionArgs.Parameters.Length != upperCaseEmailParameterCount)
						{
							throw new ArgumentException($"Expected {upperCaseEmailParameterCount} arguments");
						}
						if (functionArgs.Parameters[0].Evaluate() is not string inputEmailAddress)
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
				case CastFunctionName:
					{
						const int castParameterCount = 2;
						if (functionArgs.Parameters.Length != castParameterCount)
						{
							throw new ArgumentException($"Expected {castParameterCount} arguments");
						}
						var inputObject = functionArgs.Parameters[0].Evaluate();
						if (functionArgs.Parameters[1].Evaluate() is not string castTypeString)
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
				case JObjectFunctionName:
					{
						const int jobjectParameterCount = 1;
						if (functionArgs.Parameters.Length != jobjectParameterCount)
						{
							throw new ArgumentException($"Expected {jobjectParameterCount} arguments");
						}
						if (functionArgs.Parameters[0].Evaluate() is not string inputJobjectString)
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
				// stateLookup('EdinburghAirportServiceRequests', 'sn_sys_id', sn_element_id, 'at_id')
				case StateLookupFunctionName:
					{
						const int stateContainsParameterCount = 4;
						if (functionArgs.Parameters.Length != stateContainsParameterCount)
						{
							throw new ArgumentException($"Expected {stateContainsParameterCount} arguments");
						}
						// We have the right parameter count
						// Use the first one to obtain state.
						var state = (State)functionArgs.Parameters[0].Parameters[ConnectedSystemManagerBase.StateVariableName];

						var argumentIndex = -1;
						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string stateDataSetName)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be the State DataSet name.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string stateDataSetLookupField)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be the State DataSet lookup field.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not object stateDataSetValue)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be present.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string stateDataSetResultField)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be the State DataSet result field.");
						}

						if (!state.ItemLists.TryGetValue(stateDataSetName, out var itemList))
						{
							throw new ArgumentException($"State DataSet {stateDataSetName} not found.");
						}
						// We have the itemList

						var stateDataSetValueAsJToken = ConvertObjectToJToken(stateDataSetValue);
						var matchingItems = itemList
							.Where(i => i.TryGetValue(stateDataSetLookupField, out JToken? stateValue) && JToken.DeepEquals(stateValue, stateDataSetValueAsJToken))
							.ToList();

						switch (matchingItems.Count)
						{
							case 0:
								throw new ArgumentException($"Item {stateDataSetLookupField} in {stateDataSetName} with value {stateDataSetValue} not found.");
							case 1:
								var matchingItem = matchingItems[0];
								if (!matchingItem.TryGetValue(stateDataSetResultField, out JToken? result))
								{
									throw new ArgumentException($"Item {stateDataSetLookupField} in {stateDataSetName} with value {stateDataSetValue} does not have field {stateDataSetResultField}.");
								}
								functionArgs.Result = result;
								break;
							default:
								throw new ArgumentException($"Item {stateDataSetLookupField} in {stateDataSetName} with value {stateDataSetValue} found {matchingItems.Count} items - expected 1.");
						}
						return;
					}
				case StateContainsFunctionName:
					{
						const int stateContainsParameterCount = 3;
						if (functionArgs.Parameters.Length != stateContainsParameterCount)
						{
							throw new ArgumentException($"Expected {stateContainsParameterCount} arguments");
						}
						// We have the right parameter count
						// Use the first one to obtain state.
						var state = (State)functionArgs.Parameters[0].Parameters[ConnectedSystemManagerBase.StateVariableName];

						var argumentIndex = -1;
						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string stateDataSetName)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be the State DataSet name.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string stateDataSetField)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be the State DataSet field.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not object stateDataSetValue)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be present.");
						}

						var stateDataSetValueAsJToken = ConvertObjectToJToken(stateDataSetValue);

						functionArgs.Result = state.ItemLists.TryGetValue(stateDataSetName, out var itemList)
							&& itemList.Any(il => il.TryGetValue(stateDataSetField, out var stateValue) && JToken.DeepEquals(stateValue, stateDataSetValueAsJToken));
						return;
					}
				case QueryLookupFunctionName:
					{
						const int minParameterCount = 5;
						const int maxParameterCount = 7;
						if (functionArgs.Parameters.Length < minParameterCount || functionArgs.Parameters.Length > maxParameterCount)
						{
							throw new ArgumentException($"Expected between {minParameterCount} and {maxParameterCount} arguments in {QueryLookupFunctionName}.");
						}

						var argumentIndex = -1;
						// TODO - obtain state from first parameter, like in stateContains.
						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not State state)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be the State.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string queryLookupConnectedSystemName)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be the name of a Connected System.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string queryLookupType)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be a query type.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string queryLookupQuery)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be a query string.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string queryLookupField)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be a field name.");
						}
						// We now have all mandatory parameters

						// Get optional value to use if no results are returned
						object? valueIfZeroMatchesFound = null;
						var valueIfZeroMatchesFoundSets = false;
						if (++argumentIndex < functionArgs.Parameters.Length - 1)
						{
							valueIfZeroMatchesFound = functionArgs.Parameters[argumentIndex].Evaluate();
							valueIfZeroMatchesFoundSets = true;
						}

						object? valueIfMultipleMatchesFound = null;
						var valueIfMultipleMatchesFoundSets = false;
						if (++argumentIndex < functionArgs.Parameters.Length - 1)
						{
							valueIfMultipleMatchesFound = functionArgs.Parameters[argumentIndex].Evaluate();
							valueIfMultipleMatchesFoundSets = true;
						}

						functionArgs.Result = state.QueryConnectedSystemAsync(
							queryLookupConnectedSystemName,
							new QueryConfig { Type = queryLookupType, Query = queryLookupQuery },
							queryLookupField,
							valueIfZeroMatchesFoundSets,
							valueIfZeroMatchesFound,
							valueIfMultipleMatchesFoundSets,
							valueIfMultipleMatchesFound,
							default).GetAwaiter().GetResult();
						return;
					}
				case PatchFunctionName:
					{
						const int minParameterCount = 5;
						if (functionArgs.Parameters.Length < minParameterCount || functionArgs.Parameters.Length % 2 != 1)
						{
							throw new ArgumentException($"Expected an odd number of at least {minParameterCount} arguments in {PatchFunctionName}.");
						}
						// We have the right parameter count

						// Use the first one to obtain state.
						var state = (State)functionArgs.Parameters[0].Parameters[ConnectedSystemManagerBase.StateVariableName];

						var argumentIndex = -1;
						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string patchConnectedSystemName)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be the name of a Connected System.");
						}

						if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string patchEntityClass)
						{
							throw new ArgumentException($"Expected argument {argumentIndex} to be an entity class.");
						}

						// The source could be a string or an int.  We always send the string representation to the connected system.
						var patchEntityId = functionArgs.Parameters[++argumentIndex].Evaluate()?.ToString()
							?? throw new ArgumentException($"Expected argument {argumentIndex} to be non-null.");

						var patches = new Dictionary<string, object>();
						while (argumentIndex < functionArgs.Parameters.Length - 1)
						{
							if (functionArgs.Parameters[++argumentIndex].Evaluate() is not string patchField)
							{
								throw new ArgumentException($"Expected argument {argumentIndex} to be a field name.");
							}

							patches[patchField] = functionArgs.Parameters[++argumentIndex].Evaluate();
						}
						// We now have all parameters

						try
						{
							state.PatchConnectedSystemAsync(
							patchConnectedSystemName,
							patchEntityClass,
							patchEntityId,
							patches,
							default).GetAwaiter().GetResult();
							functionArgs.Result = true;
						}
						catch
						{
							functionArgs.Result = false;
						}

						return;
					}
			}
		}

		private static JToken ConvertObjectToJToken(object @object) => @object is JToken jToken
								? jToken
								: JToken.FromObject(@object ?? JValue.CreateNull());
	}
}

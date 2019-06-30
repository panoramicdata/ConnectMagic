using NCalc;
using PanoramicData.ConnectMagic.Service.Models;
using System;

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
				case "queryLookup":
					const int parameterCount = 4;
					if (functionArgs.Parameters.Length != parameterCount)
					{
						throw new ArgumentException($"Expected {parameterCount} arguments");
					}

					if (!(functionArgs.Parameters[0].Evaluate() is State state))
					{
						throw new ArgumentException("Expected first argument to be the State.");
					}

					if (!(functionArgs.Parameters[1].Evaluate() is string queryLookupConnectedSystemName))
					{
						throw new ArgumentException("Expected first argument to be the name of a Connected System.");
					}

					if (!(functionArgs.Parameters[2].Evaluate() is string queryLookupQuery))
					{
						throw new ArgumentException("Expected second argument to be a query string.");
					}

					if (!(functionArgs.Parameters[3].Evaluate() is string queryLookupField))
					{
						throw new ArgumentException("Expected third argument to be a field name.");
					}
					// We now have all three parameters

					functionArgs.Result = state.QueryLookupAsync(queryLookupConnectedSystemName, queryLookupQuery, queryLookupField).GetAwaiter().GetResult();
					return;
			}
		}
	}
}

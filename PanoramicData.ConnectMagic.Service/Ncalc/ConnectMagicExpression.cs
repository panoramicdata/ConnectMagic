using PanoramicData.NCalcExtensions;
using System;

namespace PanoramicData.ConnectMagic.Service.Ncalc
{
	public class ConnectMagicExpression : ExtendedExpression
	{
		public ConnectMagicExpression(string expression) : base(
			expression ??
			throw new ArgumentNullException(nameof(expression))
			)
		{
			EvaluateFunction += NcalcExtensions.Extend;
		}
	}
}

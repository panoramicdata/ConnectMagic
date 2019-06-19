using PanoramicData.NCalcExtensions;

namespace PanoramicData.ConnectMagic.Service.Ncalc
{
	public class ConnectMagicExpression : ExtendedExpression
	{
		public ConnectMagicExpression(string expression) : base(expression)
		{
			EvaluateFunction += NcalcExtensions.Extend;
		}
	}
}

using System;
using System.Globalization;
using System.IO;

namespace Kaleidoscope.Ast
{
	internal class Printer: Visitor
	{
		// The text writer to print info to
		private TextWriter _writer;

		internal Printer(TextWriter writer) => this._writer = writer;

		public VisitLiteralExpression(LiteralExpression expr)
		{
			// ...
		}

		public void VisitParameterExpression(ParameterExpression expr)
		{
			// ...
		}

		public void VisitBinaryOperatorExpression(BinaryOperatorExpression expr)
		{
			// TODO: Print something like "BinOp { op: '+', left: <...>, right: <...> }"
		}

		// ...
	}
}

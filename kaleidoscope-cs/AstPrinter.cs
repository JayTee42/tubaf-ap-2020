using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Kaleidoscope.Ast
{
	internal class Printer: Visitor
	{
		// The current tree depth
		private int _depth = 0;

		// Do we have to print a trailing comma behind the current element?
		private bool _trailingComma = false;

		// The text writer to print into
		private TextWriter _writer;

		// The current padding is derived from the depth.
		private string Padding => new String(' ', this._depth * 2);

		// A convenience string property for the trailing comma
		private string TrailingComma => this._trailingComma ? "," : "";

		// Internal constructor, only the static print method is public!
		internal Printer(TextWriter writer)
		{
			this._writer = writer;
		}

		public void VisitLiteralExpression(LiteralExpression expr)
		{
			this._writer.WriteLine($@"{ this.Padding }Literal {{ value: ""{ expr.Value.ToString(CultureInfo.InvariantCulture) }"" }}{ this.TrailingComma }");
		}

		public void VisitParameterExpression(ParameterExpression expr)
		{
			this._writer.WriteLine($@"{ this.Padding }Parameter {{ name: ""{ expr.Name }"" }}{ this.TrailingComma }");
		}

		public void VisitBinaryOperatorExpression(BinaryOperatorExpression expr)
		{
			var trailingComma = this._trailingComma;
			this._trailingComma = false;

			this._writer.WriteLine($@"{ this.Padding }Operator {{");

			this._depth++;

			this._writer.WriteLine($@"{ this.Padding }type: { expr.Operator },");
			this._writer.WriteLine($@"{ this.Padding }left:");

			this._depth++;
			expr.Left.Accept(this);
			this._depth--;

			this._writer.WriteLine($"{ this.Padding }right:");

			this._depth++;
			expr.Right.Accept(this);
			this._depth--;

			this._depth--;

			this._trailingComma = trailingComma;
			this._writer.WriteLine($@"{ this.Padding }}}{ this.TrailingComma }");
		}

		public void VisitCallExpression(CallExpression expr)
		{
			var trailingComma = this._trailingComma;
			this._trailingComma = true;

			if (expr.Parameters.Count > 0)
			{
				this._writer.WriteLine($@"{ this.Padding }Call {{ name: ""{ expr.Name }"", parameters: [");

				this._depth++;

				foreach (var parameter in expr.Parameters)
				{
					parameter.Accept(this);
				}

				this._depth--;

				this._trailingComma = trailingComma;
				this._writer.WriteLine($@"{ this.Padding }]}}{ this.TrailingComma }");
			}
			else
			{
				this._trailingComma = trailingComma;
				this._writer.WriteLine($@"{ this.Padding }Call {{ name: ""{ expr.Name }"", parameters: []{ this.TrailingComma }");
			}
		}

		public void VisitConditionalExpression(ConditionalExpression expr)
		{
			var trailingComma = this._trailingComma;
			this._trailingComma = false;

			this._writer.WriteLine($@"{ this.Padding }Conditional {{");

			this._depth++;

			this._writer.WriteLine($@"{ this.Padding }condition:");

			this._depth++;
			expr.Condition.Accept(this);
			this._depth--;

			this._writer.WriteLine($"{ this.Padding }then:");

			this._depth++;
			expr.Then.Accept(this);
			this._depth--;

			this._writer.WriteLine($"{ this.Padding }else:");

			this._depth++;
			expr.Else.Accept(this);
			this._depth--;

			this._depth--;

			this._trailingComma = trailingComma;
			this._writer.WriteLine($@"{ this.Padding }}}{ this.TrailingComma }");
		}

		public void VisitFunctionPrototype(FunctionPrototype prototype)
		{
			var joinedParameterNames = string.Join(", ", prototype.ParameterNames.Select(p => "\"" + p + "\""));
			this._writer.WriteLine($@"{ this.Padding }Prototype {{ name: ""{ prototype.Name }"", parameter_names: [{ joinedParameterNames }] }}{ this.TrailingComma }");
		}

		public void VisitFunctionDeclaration(FunctionDeclaration declaration)
		{
			var trailingComma = this._trailingComma;
			this._trailingComma = false;

			this._writer.WriteLine($@"{ this.Padding }FunctionDec {{");

			this._depth++;

			this._writer.WriteLine($@"{ this.Padding }prototype:");

			this._depth++;
			declaration.Prototype.Accept(this);
			this._depth--;

			this._depth--;

			this._trailingComma = trailingComma;
			this._writer.WriteLine($@"{ this.Padding }}}{ this.TrailingComma }");
		}

		public void VisitFunctionDefinition(FunctionDefinition definition)
		{
			var trailingComma = this._trailingComma;
			this._trailingComma = false;

			this._writer.WriteLine($@"{ this.Padding }FunctionDef {{");

			this._depth++;

			this._writer.WriteLine($@"{ this.Padding }prototype:");

			this._depth++;
			definition.Prototype.Accept(this);
			this._depth--;

			this._writer.WriteLine($"{ this.Padding }body:");

			this._depth++;
			definition.Body.Accept(this);
			this._depth--;

			this._depth--;

			this._trailingComma = trailingComma;
			this._writer.WriteLine($@"{ this.Padding }}}{ this.TrailingComma }");
		}
	}

	public static class PrinterElementExt
	{
		public static void Print(this Element ast, TextWriter writer)
		{
			// Create a new printer that wraps the writer.
			var printer = new Printer(writer);

			// Visit the AST recursively.
			ast.Accept(printer);
		}
	}
}

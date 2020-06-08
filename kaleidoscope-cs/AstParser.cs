using System;
using System.Collections.Generic;

namespace Kaleidoscope.Ast
{
	public class Parser
	{
		// The internal lexer that delivers our tokens
		private Lexer _lexer;

		// The current token that is inspected by the parser.
		private Token _token;

		// Create a new parser by wrapping a lexer.
		public Parser(Lexer lexer)
		{
			this._lexer = lexer;
			EatToken();
		}

		// Parse a top-level element and return it.
		public TopLevelElement Parse()
		{
			// TODO
			return null;
		}

		// Get the precedence of the given operator.
		private static int GetOperatorPrecedence(Operator op)
		{
			switch (op)
			{
				case Operator.Add: return 80;
				case Operator.Subtract: return 80;
				case Operator.Multiply: return 100;
				case Operator.Divide: return 100;
				case Operator.Equal: return 60;
				case Operator.LowerThan: return 60;
				case Operator.LowerThanEqual: return 60;
				case Operator.GreaterThan: return 60;
				case Operator.GreaterThanEqual: return 60;

				default: throw new InvalidOperationException("Invalid operator type.");
			}
		}

		// Consume the current token and load the next one.
		// If the current token is the end of the file, this will throw!
		private void EatToken()
		{
			// Skip comments.
			do
			{
				this._token = this._lexer.Lex();
			} while (this._token.Type == TokenType.Comment);
		}
	}
}

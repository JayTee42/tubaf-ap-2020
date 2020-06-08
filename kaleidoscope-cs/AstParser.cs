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
			switch (this._token.Type)
			{
				case TokenType.Keyword when (this._token.Keyword == Keyword.Dec): return ParseFunctionDeclaration();
				case TokenType.Keyword when (this._token.Keyword == Keyword.Def): return ParseFunctionDefinition();
				case TokenType.EndOfFile: return null;

				default: throw new FormatException($"Expected function declaration / definition or end of file, but got '{ this._token }'.");
			}
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

		// Parse a generic expression (= a primary expression + (op + primary expression)*).
		private Expression ParseExpression()
		{
			// TODO
			return new LiteralExpression(42);
		}

		// Parse a function prototype.
		private FunctionPrototype ParseFunctionPrototype()
		{
			// Parse the function name.
			if (this._token.Type != TokenType.Identifier)
			{
				throw new FormatException($"Expected identifier in function prototype, but got '{ this._token }'.");
			}

			var name = this._token.Identifier;
			EatToken();

			// Parse the opening bracket.
			if ((this._token.Type != TokenType.Bracket) || (this._token.Bracket != Bracket.RoundStart))
			{
				throw new FormatException($"Expected '(' in function prototype, but got '{ this._token }'.");
			}

			EatToken();

			// Start a list of parameter names.
			var parameterNames = new List<string>();

			// Check if this is the "no parameter" case as in "func()".
			if (this._token.Type != TokenType.Bracket)
			{
				while (true)
				{
					// Parse the current parameter name.
					if (this._token.Type != TokenType.Identifier)
					{
						throw new FormatException($"Expected identifier in function prototype, but got '{ this._token }'.");
					}

					parameterNames.Add(this._token.Identifier);
					EatToken();

					// If we now encounter a bracket, we are done.
					if (this._token.Type == TokenType.Bracket)
					{
						break;
					}

					// Otherwise, this must be a parameter separator.
					if (this._token.Type != TokenType.ParameterSeparator)
					{
						throw new FormatException($"Expected ')' or ',' in function prototype, but got '{ this._token }'.");
					}

					EatToken();
				}
			}

			// Validate and eat the closing bracket.
			if (this._token.Bracket != Bracket.RoundEnd)
			{
				throw new FormatException($"Expected ')' in function prototype, but got '{ this._token }'.");
			}

			EatToken();

			return new FunctionPrototype(name, parameterNames);
		}

		//Parse a function declaration.
		private TopLevelElement ParseFunctionDeclaration()
		{
			//Parse the "dec" keyword.
			if ((this._token.Type != TokenType.Keyword) || (this._token.Keyword != Keyword.Dec))
			{
				throw new FormatException($"Expected 'dec' in function prototype, but got '{ this._token }'.");
			}

			EatToken();

			// Parse the prototype.
			var prototype = ParseFunctionPrototype();

			// Build the final declaration.
			return new FunctionDeclaration(prototype);
		}

		// Parse a function definition.
		private TopLevelElement ParseFunctionDefinition()
		{
			//Parse the "def" keyword.
			if ((this._token.Type != TokenType.Keyword) || (this._token.Keyword != Keyword.Def))
			{
				throw new FormatException($"Expected 'def' in function prototype, but got '{ this._token }'.");
			}

			EatToken();

			// Parse the prototype.
			var prototype = ParseFunctionPrototype();

			// Parse the body expression.
			var body = ParseExpression();

			// Build the final definition.
			return new FunctionDefinition(prototype, body);
		}
	}
}

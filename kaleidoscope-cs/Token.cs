using System;

namespace Kaleidoscope
{
	// The allowed Kaleidoscope keywords
	public enum Keyword
	{
		Dec,
		Def,
		If,
		Then,
		Else,
	}

	// The allowed Kaleidoscope brackets
	public enum Bracket
	{
		RoundStart,
		RoundEnd,
	}

	// The allowed Kaleidoscope operators
	public enum Operator
	{
		Add,
		Subtract,
		Multiply,
		Divide,
		Equal,
		LowerThan,
		LowerThanEqual,
		GreaterThan,
		GreaterThanEqual,
	}

	// The type of a token
	public enum TokenType
	{
		EndOfFile,
		Keyword,
		Bracket,
		Identifier,
		Number,
		Operator,
		ParameterSeparator,
		Comment,
	}

	// A token as tagged union - hello Swift and Rust :'(
	public class Token
	{
		// The type of the token
		public TokenType Type { get; private set; }

		// TokenType.Keyword
		private Keyword _kw = Keyword.Def;

		// TokenType.Bracket
		private Bracket _br = Bracket.RoundStart;

		// TokenType.Identifier / TokenType.Comment
		private string _str = null;

		// TokenType.Number
		private double _dbl = 0;

		// TokenType.Operator
		private Operator _op = Operator.Add;

		public Keyword Keyword
		{
			get
			{
				if (this.Type != TokenType.Keyword)
				{
					throw new AccessViolationException("Token type is not Keyword.");
				}

				return this._kw;
			}
		}

		public Bracket Bracket
		{
			get
			{
				if (this.Type != TokenType.Bracket)
				{
					throw new AccessViolationException("Token type is not Bracket.");
				}

				return this._br;
			}
		}

		public string Identifier
		{
			get
			{
				if (this.Type != TokenType.Identifier)
				{
					throw new AccessViolationException("Token type is not Identifier.");
				}

				return this._str;
			}
		}

		public double Number
		{
			get
			{
				if (this.Type != TokenType.Number)
				{
					throw new AccessViolationException("Token type is not Number.");
				}

				return this._dbl;
			}
		}

		public Operator Operator
		{
			get
			{
				if (this.Type != TokenType.Operator)
				{
					throw new AccessViolationException("Token type is not Operator.");
				}

				return this._op;
			}
		}

		public string Comment
		{
			get
			{
				if (this.Type != TokenType.Comment)
				{
					throw new AccessViolationException("Token type is not Comment.");
				}

				return this._str;
			}
		}

		// Private constructor, only for internal usage
		private Token(TokenType tokenType)
		{
			this.Type = tokenType;
		}

		// Use these static constructors for token building!
		public static Token NewEndOfFile() => new Token(TokenType.EndOfFile);

		public static Token NewKeyword(Keyword keyword)
		{
			var token = new Token(TokenType.Keyword);
			token._kw = keyword;

			return token;
		}

		public static Token NewBracket(Bracket bracket)
		{
			var token = new Token(TokenType.Bracket);
			token._br = bracket;

			return token;
		}

		public static Token NewIdentifier(string identifier)
		{
			var token = new Token(TokenType.Identifier);
			token._str = identifier;

			return token;
		}

		public static Token NewNumber(double number)
		{
			var token = new Token(TokenType.Number);
			token._dbl = number;

			return token;
		}

		public static Token NewOperator(Operator op)
		{
			var token = new Token(TokenType.Operator);
			token._op = op;

			return token;
		}

		public static Token NewComment(string comment)
		{
			var token = new Token(TokenType.Comment);
			token._str = comment;

			return token;
		}

		public static Token NewParameterSeparator() => new Token(TokenType.ParameterSeparator);

		// Convert a token into a string for debug output.
		public override String ToString()
		{
			switch (this.Type)
			{
				case TokenType.EndOfFile: return "EndOfFile";
				case TokenType.Keyword: return $"Keyword({ this._kw })";
				case TokenType.Bracket: return $"Bracket({ this._br })";
				case TokenType.Identifier: return $@"Identifier(""{ this._str }"")";
				case TokenType.Number: return $"Number({ this._dbl })";
				case TokenType.Operator: return $"Operator({ this._op })";
				case TokenType.ParameterSeparator: return "ParameterSeparator";
				case TokenType.Comment: return $@"Comment(""{ this._str }"")";

				default: throw new InvalidOperationException("Invalid token type.");
			}
		}
	}
}

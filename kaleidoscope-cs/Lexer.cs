using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Kaleidoscope
{
	// The lexer reads Kaleidoscope source code from a text reader and breaks it into tokens.
	public class Lexer
	{
		// A character or EndOfFile
		private struct Character
		{
			private char? _char;

			// Determine if this is the end of the file or a normal character.
			public bool IsEndOfFile
			{
				get => !this._char.HasValue;
			}

			// Return the inner character.
			// If this is actually the end of the file, an InvalidOperationException will be thrown.
			public char Value
			{
				get
				{
					if (this.IsEndOfFile)
					{
						throw new InvalidOperationException("Character is not present because of EndOfFile.");
					}

					return this._char.Value;
				}
			}

			// Private constructor, only for internal usage
			private Character(char? c)
			{
				this._char = c;
			}

			// Use these static constructors for character building!
			public static Character NewValue(char c) => new Character(c);
			public static Character NewEndOfFile() => new Character(null);
		}

		// The internal text reader (this can be a file / the network / stdin / ...)
		private TextReader _reader;

		// The current character
		private Character _char;

		// Did we already return the EndOfFile token?
		private bool _hasReturnedEndOfFile = false;

		// Create a new lexer that wraps the given text reader.
		public Lexer(TextReader reader)
		{
			this._reader = reader;
			EatChar();
		}

		// Return the next token of the input.
		public Token Lex()
		{
			// It is an error to call this method if we have already returned EndOfFile.
			if (this._hasReturnedEndOfFile)
			{
				throw new InvalidOperationException("EndOfFile has already been returned.");
			}

			// Skip whitespaces.
			while (!this._char.IsEndOfFile && char.IsWhiteSpace(this._char.Value))
			{
				EatChar();
			}

			// Check for the end of the file.
			if (this._char.IsEndOfFile)
			{
				this._hasReturnedEndOfFile = true;
				return Token.NewEndOfFile();
			}

			var chr = this._char.Value;

			// Identifiers / Keywords
			if (char.IsLetter(chr) || (chr == '_'))
			{
				// The token starts with a letter or an underscore.
				// This is a keyword or an identifier (remember, keywords and identifiers must not start with numbers).
				// Keep lexing into a string until we hit something that is not a letter or a number.
				var word = LexWord();

				// Is this a keyword? Otherwise, we have an identifier.
				return Lexer.ConvertToKeyword(word, out var keyword) ? Token.NewKeyword(keyword) : Token.NewIdentifier(word);
			}

			// Brackets
			if ("()".IndexOf(chr) >= 0)
			{
				return Token.NewBracket(LexBracket());
			}

			// Numbers
			if (char.IsDigit(chr) || (chr == '.'))
			{
				return Token.NewNumber(LexNumber());
			}

			// Operators
			if ("+-*/<>=".IndexOf(chr) >= 0)
			{
				return Token.NewOperator(LexOperator());
			}

			// Parameter separators
			if (chr == ',')
			{
				EatChar();
				return Token.NewParameterSeparator();
			}

			// Comments
			if (chr == '#')
			{
				return Token.NewComment(LexComment());
			}

			// Everything we don't know is bad by definition.
			throw new FormatException($"Encountered invalid character at start of token: '{ chr }'");
		}

		// Try to turn the given word into a keyword.
		private static bool ConvertToKeyword(string str, out Keyword keyword)
		{
			switch (str)
			{
				case "dec": keyword = Keyword.Dec; return true;
				case "def": keyword = Keyword.Def; return true;
				case "if": keyword = Keyword.If; return true;
				case "then": keyword = Keyword.Then; return true;
				case "else": keyword = Keyword.Else; return true;

				default: keyword = Keyword.Def; return false;
			}
		}

		// Consume the current character and load the next one.
		private void EatChar()
		{
			// Read a new char and assign it.
			var newChar = this._reader.Read();
			this._char = (newChar < 0) ? Character.NewEndOfFile() : Character.NewValue((char)newChar);
		}

		// Read a word consisting of letters, numbers or underscores.
		private string LexWord()
		{
			// Check for the end of the file.
			if (this._char.IsEndOfFile)
			{
				throw new FormatException("Expected word, but found end of file.");
			}

			// Read the word while we encounter valid characters.
			var wordBuilder = new StringBuilder();

			do
			{
				// Allow letters, digits and underscores.
				var chr = this._char.Value;

				if (!char.IsLetterOrDigit(chr) && (chr != '_'))
				{
					break;
				}

				EatChar();
				wordBuilder.Append(chr);
			} while (!this._char.IsEndOfFile);

			return wordBuilder.ToString();
		}

		// Read a bracket from the given character.
		private Bracket LexBracket()
		{
			// Check for the end of the file.
			if (this._char.IsEndOfFile)
			{
				throw new FormatException("Expected bracket, but found end of file.");
			}

			switch (this._char.Value)
			{
				case '(': EatChar(); return Bracket.RoundStart;
				case ')': EatChar(); return Bracket.RoundEnd;

				default: throw new FormatException($"Expected bracket, but found '{ this._char.Value }'.");
			}
		}

		// Read a number consisting of dots and digits.
		// This will throw an FormatException if the number is malformed.
		private double LexNumber()
		{
			// Check for the end of the file.
			if (this._char.IsEndOfFile)
			{
				throw new FormatException("Expected number, but found end of file.");
			}

			// Read the number while we encounter valid characters.
			var numberBuilder = new StringBuilder();

			do
			{
				// Allow digits and dots.
				var chr = this._char.Value;

				if (!char.IsDigit(chr) && (chr != '.'))
				{
					break;
				}

				EatChar();
				numberBuilder.Append(chr);
			} while (!this._char.IsEndOfFile);

			var numberString = numberBuilder.ToString();

			// Try to parse the number as double.
			var numberStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

			if (!double.TryParse(numberString, numberStyle, NumberFormatInfo.InvariantInfo, out var number))
			{
				throw new FormatException($"Failed to parse number literal: { numberString }");
			}

			return number;
		}

		// Try to read an operator from special chars.
		private Operator LexOperator()
		{
			// Check for the end of the file.
			if (this._char.IsEndOfFile)
			{
				throw new FormatException("Expected operator, but found end of file.");
			}

			switch (this._char.Value)
			{
				case '+': EatChar(); return Operator.Add;
				case '-': EatChar(); return Operator.Subtract;
				case '*': EatChar(); return Operator.Multiply;
				case '/': EatChar(); return Operator.Divide;
				case '<':

					EatChar();

					if (!this._char.IsEndOfFile && (this._char.Value == '='))
					{
						EatChar();
						return Operator.LowerThanEqual;
					}
					else
					{
						return Operator.LowerThan;
					}

				case '>':

					EatChar();

					if (!this._char.IsEndOfFile && (this._char.Value == '='))
					{
						EatChar();
						return Operator.GreaterThanEqual;
					}
					else
					{
						return Operator.GreaterThan;
					}

				case '=':

					EatChar();

					if (this._char.IsEndOfFile || (this._char.Value != '='))
					{
						throw new FormatException("Operator = is not (yet) allowed, please use == for comparisons.");
					}

					EatChar();

					return Operator.Equal;

				default: throw new FormatException($"Expected operator, but found '{ this._char.Value }'.");
			}
		}

		// Read a comment until a new line appears.
		private string LexComment()
		{
			// Check for the end of the file.
			if (this._char.IsEndOfFile)
			{
				throw new FormatException("Expected comment, but found end of file.");
			}

			// Eat the '#'.
			if (this._char.Value != '#')
			{
				throw new FormatException($"Expected '#', but found { this._char.Value }.");
			}

			EatChar();

			// Read the comment while we encounter non-newline characters.
			var commentBuilder = new StringBuilder();

			while (!this._char.IsEndOfFile)
			{
				// Allow everything except newlines.
				var chr = this._char.Value;
				EatChar();

				if ((chr == '\r') || (chr == '\n'))
				{
					// The newline char is consumed along with the comment.
					// If there are multiple newline chars, the remaining ones will be consumed as leading whitespace.

					break;
				}

				commentBuilder.Append(chr);
			}

			return commentBuilder.ToString();
		}
	}
}

using System;
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

		// Create a new lexer that wraps the given text reader.
		public Lexer(TextReader reader)
		{
			this._reader = reader;
			EatChar();
		}

		// Return the next token of the input.
		public Token Lex()
		{
			// TODO
			return Token.NewEndOfFile();
		}

		// Consume the current character and load the next one.
		private void EatChar()
		{
			// Read a new char and assign it.
			var newChar = this._reader.Read();
			this._char = (newChar < 0) ? Character.NewEndOfFile() : Character.NewValue((char)newChar);
		}
	}
}

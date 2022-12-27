using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace nlconv
{
	public abstract partial class NetlistLexerBase
	{
		protected NetlistLexerBase() { }

		protected static LinkedListNode<LexerToken> Lex(string line)
		{
			int pos = 0;
			return Lex(line, null, 1, ref pos);
		}

		protected static LinkedListNode<LexerToken> Lex(string line, string file, int lineNum, ref int pos)
		{
			LinkedList<LexerToken> t = new LinkedList<LexerToken>();
			StringBuilder sb = new StringBuilder();
			int col = -1;
			char c = '\0';
			Position fpos;

			pos--;
		read:
			pos++;
			col++;
		next:
			fpos = new Position(file, pos, lineNum, col + 1);
			if (line.Length == col)
				return t.First;

			c = line[col];

			// The easy symbols:
			switch (c)
			{
			case '#':
				return t.First;
			case ',':
				t.AddLast(new LexerToken(fpos, LexerTokenType.Comma));
				goto read;
			case '+':
				t.AddLast(new LexerToken(fpos, LexerTokenType.Plus));
				goto read;
			case '-':
				if (col + 1 < line.Length && line[col + 1] == '>')
				{
					t.AddLast(new LexerToken(fpos, LexerTokenType.To));
					pos++;
					col++;
					goto read;
				}
				t.AddLast(new LexerToken(fpos, LexerTokenType.Minus));
				goto read;
			case ':':
				t.AddLast(new LexerToken(fpos, LexerTokenType.Colon));
				goto read;
			case ';':
				t.AddLast(new LexerToken(fpos, LexerTokenType.EOT));
				goto read;
			case '.':
				t.AddLast(new LexerToken(fpos, LexerTokenType.Dot));
				goto read;
			case '@':
				t.AddLast(new LexerToken(fpos, LexerTokenType.At));
				goto read;
			}

			// Space, Tab, ...?
			if (char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator || c == '\t')
				goto read;

			// String?
			if (c == '"')
			{
				bool escaped = false;
				sb.Clear();
				while (true)
				{
					col++;
					if (line.Length == col)
						throw new NetlistFormatException(fpos, "Unterminated string literal encountered by lexer.");
					pos++;
					c = line[col];
					if (escaped)
					{
						switch (c)
						{
							case 'n': sb.Append('\n'); break;
							case 't': sb.Append('\t'); break;
							default:  sb.Append(c);    break;
						}
						escaped = false;
						continue;
					}
					else if (c != '"')
					{
						switch (c)
						{
							case '\\': escaped = true; break;
							default:   sb.Append(c);   break;
						}
						continue;
					}
					break;
				}
				t.AddLast(new LexerToken(fpos, LexerTokenType.String, sb.ToString()));
				goto read;
			}

			// Start of name or value?
			if (char.IsLetterOrDigit(c) || c == '_' || c == '~' || c == '{' || c == '}' || c == '[' || c == ']')
			{
				bool is_name = false;
				bool is_val = false;
				sb.Clear();
				while (char.IsLetterOrDigit(c) || c == '_' || c == '~' || c == '{' || c == '}' || c == '[' || c == ']' || c == '-' || c == '.')
				{
					if (c == '.')
					{
						if (is_name)
							break;
						is_val = true;
					}
					if (is_val && (char.IsLetter(c) || c == '_' || c == '~' || c == '{' || c == '}' || c == '[' || c == ']' || c == '-'))
						break;
					sb.Append(c);
					if (!is_val && !char.IsDigit(c))
						is_name = true;
					col++;
					if (line.Length == col)
						break;
					pos++;
					c = line[col];
				}
				if (is_name)
				{
					t.AddLast(new LexerToken(fpos, LexerTokenType.Name, sb.ToString()));
					goto next;
				}
				string str = sb.ToString();
				float val;
				if (!float.TryParse(str, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out val))
					throw new NetlistFormatException(fpos, "Invalid floating point number encountered by lexer.");
				t.AddLast(new LexerToken(fpos, LexerTokenType.Value, val));
				goto next;
			}

			throw new NetlistFormatException(fpos, "Unknown input character encountered by lexer.");
		}
	}
}

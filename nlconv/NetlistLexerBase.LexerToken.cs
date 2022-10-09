namespace nlconv
{
	public abstract partial class NetlistLexerBase
	{
		protected class LexerToken
		{
			public readonly int            Pos;
			public readonly int            Line;
			public readonly int            Col;
			public readonly LexerTokenType Type;
			public readonly string         String = null;
			public readonly float          Value  = 0.0f;

			public LexerToken(int pos, int line, int col, LexerTokenType type)
			{
				Pos  = pos;
				Line = line;
				Col  = col;
				Type = type;
			}

			public LexerToken(int pos, int line, int col, LexerTokenType type, string str)
				: this(pos, line, col, type)
			{
				String = str;
			}

			public LexerToken(int pos, int line, int col, LexerTokenType type, float val)
				: this(pos, line, col, type)
			{
				Value = val;
			}

			public LexerToken(int pos, LexerTokenType type)
				: this(pos, 1, pos + 1, type)
			{ }

			public LexerToken(int pos, LexerTokenType type, string str)
				: this(pos, 1, pos + 1, type, str)
			{ }

			public LexerToken(int pos, LexerTokenType type, float val)
				: this(pos, 1, pos + 1, type, val)
			{ }

			public override string ToString()
			{
				string s = null;
				switch (Type)
				{
					case LexerTokenType.EOT:    s = "<EOT>";              break;
					case LexerTokenType.Name:   s = "'" + String + "'";   break;
					case LexerTokenType.String: s = "\"" + String + "\""; break;
					case LexerTokenType.Value:  s = Value.ToString();     break;
					case LexerTokenType.Comma:  s = "<,>";                break;
					case LexerTokenType.Plus:   s = "<+>";                break;
					case LexerTokenType.Minus:  s = "<->";                break;
					case LexerTokenType.Colon:  s = "<:>";                break;
					case LexerTokenType.Dot:    s = "<.>";                break;
					case LexerTokenType.At:     s = "<@>";                break;
					case LexerTokenType.To:     s = "<TO>";               break;
				}
				return s;
			}
		}
	}
}

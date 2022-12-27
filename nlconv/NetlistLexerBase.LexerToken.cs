namespace nlconv
{
	public abstract partial class NetlistLexerBase
	{
		protected class LexerToken
		{
			public readonly Position       Pos;
			public readonly LexerTokenType Type;
			public readonly string         String = null;
			public readonly float          Value  = 0.0f;

			public LexerToken(Position pos, LexerTokenType type)
			{
				Pos  = pos;
				Type = type;
			}

			public LexerToken(Position pos, LexerTokenType type, string str)
				: this(pos, type)
			{
				String = str;
			}

			public LexerToken(Position pos, LexerTokenType type, float val)
				: this(pos, type)
			{
				Value = val;
			}

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

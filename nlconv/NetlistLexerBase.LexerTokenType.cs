namespace nlconv
{
	public abstract partial class NetlistLexerBase
	{
		[System.Serializable]
		protected enum LexerTokenType
		{
			EOT,    // End of Text
			Name,   // TYPE, CELL, WIRE, D4, ~{RST}, ...
			String, // String literals "..."
			Value,  // 10.5, 58.34, ...
			Comma,  // ,
			Plus,   // +
			Minus,  // -
			Colon,  // :
			Dot,    // .
			At,     // @
			To,     // ->
		}
	}
}

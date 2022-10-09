using System.Collections.Generic;

namespace nlconv
{
	public class AliasDefinition : ParserToken
	{
		public readonly string       Name;
		public readonly List<string> Alias;

		public AliasDefinition(int pos, int line, int col, string name)
			: base(pos, line, col)
		{
			Name  = name;
			Alias = new List<string>();
		}
	}
}

using System.Collections.Generic;

namespace nlconv
{
	public class AliasDefinition : ParserToken
	{
		public readonly string       Name;
		public readonly List<string> Alias;
		public readonly AliasType    Type;

		public AliasDefinition(int pos, int line, int col, string name, AliasType type)
			: base(pos, line, col)
		{
			Name  = name;
			Type  = type;
			Alias = new List<string>();
		}
	}
}

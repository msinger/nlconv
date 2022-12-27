using System.Collections.Generic;

namespace nlconv
{
	public class AliasDefinition : ParserToken
	{
		public readonly string       Name;
		public readonly List<string> Alias;
		public readonly AliasType    Type;

		public AliasDefinition(Position pos, string name, AliasType type)
			: base(pos)
		{
			Name  = name;
			Type  = type;
			Alias = new List<string>();
		}
	}
}

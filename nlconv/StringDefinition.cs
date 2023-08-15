using System.Collections.Generic;

namespace nlconv
{
	public class StringDefinition : ParserToken
	{
		public readonly string Name;
		public readonly string String;

		public StringDefinition(Position pos, string name, string str)
			: base(pos)
		{
			Name   = name;
			String = str;
		}
	}
}

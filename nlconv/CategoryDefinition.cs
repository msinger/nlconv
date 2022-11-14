using System;
using System.Text;

namespace nlconv
{
	public class CategoryDefinition : ParserToken
	{
		public readonly string Name;
		public readonly string Color;

		public CategoryDefinition(int pos, int line, int col, string name, string color)
			: base(pos, line, col)
		{
			Name  = name;
			Color = color;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("category ");
			sb.Append(Name);
			if (!string.IsNullOrEmpty(Color))
			{
				sb.Append(":");
				sb.Append(Color);
			}
			sb.Append(";");
			return sb.ToString();
		}
	}
}

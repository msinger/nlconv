using System;
using System.Text;

namespace nlconv
{
	public class SignalDefinition : ParserToken
	{
		public readonly string Name;
		public readonly string Color;
		public readonly string Description;

		public SignalDefinition(Position pos, string name, string color, string desc)
			: base(pos)
		{
			Name        = name;
			Color       = color;
			Description = desc;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("signal ");
			sb.Append(Name);
			if (!string.IsNullOrEmpty(Color))
			{
				sb.Append(":");
				sb.Append(Color);
			}
			if (!string.IsNullOrEmpty(Description))
			{
				sb.Append(" \"");
				sb.Append(Description.Escape());
				sb.Append("\"");
			}
			sb.Append(";");
			return sb.ToString();
		}
	}
}

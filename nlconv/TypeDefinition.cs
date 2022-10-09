using System.Text;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace nlconv
{
	public class TypeDefinition : ParserToken
	{
		public readonly string Name;
		public readonly string Color;
		public readonly Dictionary<string, PortDefinition> Ports;

		public TypeDefinition(int pos, int line, int col, string name, string color) : base(pos, line, col)
		{
			Name  = name;
			Color = color;
			Ports = new Dictionary<string, PortDefinition>();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("type ");
			sb.Append(Name);
			if (!string.IsNullOrEmpty(Color))
			{
				sb.Append(":");
				sb.Append(Color);
			}
			foreach(var port in Ports.Values)
			{
				sb.Append(" ");
				sb.Append(port);
			}
			sb.Append(";");
			return sb.ToString();
		}

		public static string PortDirectionString(PortDirection d)
		{
			switch (d)
			{
				case PortDirection.Input:        return "in";
				case PortDirection.Output:       return "out";
				case PortDirection.Tristate:     return "tri";
				case PortDirection.Bidir:        return "inout";
				case PortDirection.OutputLow:    return "out0";
				case PortDirection.OutputHigh:   return "out1";
				case PortDirection.NotConnected: return "nc";
			}
			return "?";
		}

		public virtual void HtmlPorts(TextWriter s)
		{
			bool first = true;
			foreach (var kvp in Ports)
			{
				if (!first)
					s.Write("<br>");
				first = false;
				s.Write("[<span class=\"" + kvp.Value.CssClass + "\">");
				s.Write(PortDirectionString(kvp.Value.Direction));
				s.Write("</span>] ");
				s.Write(kvp.Value.Name.ToHtmlName());
			}
		}

		public virtual void HtmlCells(TextWriter s, IEnumerable<CellDefinition> cells)
		{
			int count = 0;
			foreach (var c in cells)
			{
				if (c.IsVirtual)
					continue;
				if (c.Type != Name)
					continue;
				if (count != 0)
					s.Write(", ");
				count++;
				s.Write("<a href=\"#c_" + c.Name.ToHtmlId() + "\">");
				s.Write(c.Name.ToHtmlName());
				s.Write("</a>");
			}
			if (count != 0)
				s.Write(" ");
			s.Write("(");
			s.Write(count.ToString(CultureInfo.InvariantCulture));
			s.Write(" total)");
		}

		public virtual void ToHtml(TextWriter s, IEnumerable<CellDefinition> cells, string doc, string cpuDoc)
		{
			s.Write("<h2 id=\"t_" + Name.ToHtmlId() + "\">Type - <span class=\"" + CssClass + "\">" + Name.ToUpperInvariant().ToHtmlName() + "</span></h2>");
			s.Write("<dl>");
			s.Write("<dt>Name</dt><dd>" + Name.ToHtmlName() + "</dd>");
			s.Write("<dt>Ports</dt><dd>");
			HtmlPorts(s);
			s.Write("</dd><dt>Cells</dt><dd>");
			HtmlCells(s, cells);
			s.Write("</dd>");
			s.Write("</dl>");
			if (Name.ToLowerInvariant() == "cpu" || Name.ToLowerInvariant() == "sm83")
				s.Write("<p><a href=\"" + cpuDoc + "\">View description of SM83 core connections</a></p>");
			else
				s.Write("<p><a href=\"" + doc + "#" + Name.ToHtmlId() + "\">View description in cell reference</a></p>");
		}

		public string CssClass
		{
			get
			{
				return "bg_" + (string.IsNullOrEmpty(Color) ? "white" : Color);
			}
		}
	}
}

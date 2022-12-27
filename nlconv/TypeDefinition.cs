using System.Text;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Drawing;

namespace nlconv
{
	public class TypeDefinition : ParserToken
	{
		public readonly string Name;
		public readonly string Color;
		public readonly string Description;
		public readonly string DocUrl;
		public readonly Dictionary<string, PortDefinition>    Ports;
		public readonly Dictionary<string, List<List<float>>> Coords;

		public TypeDefinition(Position pos,
		                      string   name,
		                      string   color,
		                      string   desc,
		                      string   doc)
			: base(pos)
		{
			Name        = name;
			Color       = color;
			Description = desc;
			DocUrl      = doc;
			Ports       = new Dictionary<string, PortDefinition>();
			Coords      = new Dictionary<string, List<List<float>>>();
		}

		public void AddCoords(string name, List<float> coords)
		{
			List<List<float>> l;
			if (!Coords.TryGetValue(name, out l))
			{
				l = new List<List<float>>();
				Coords.Add(name, l);
			}
			l.Add(coords);
		}

		public List<float> AddCoords(string name)
		{
			List<float> coords = new List<float>();
			AddCoords(name, coords);
			return coords;
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
			foreach (var kvp in Coords)
			{
				for (int i = 0; i < kvp.Value.Count; i++)
				{
					sb.Append(" ");
					sb.Append(kvp.Key);
					sb.Append("@");
					sb.Append(CoordString(kvp.Value[i]));
				}
			}
			if (!string.IsNullOrEmpty(Description))
			{
				sb.Append(" \"");
				sb.Append(Description.Escape());
				sb.Append("\"");
			}
			if (!string.IsNullOrEmpty(DocUrl))
			{
				sb.Append(" doc \"");
				sb.Append(DocUrl.Escape());
				sb.Append("\"");
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

		public virtual void HtmlCells(TextWriter s, Netlist netlist)
		{
			int count = 0;
			foreach (var c in netlist.Cells.Values)
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

		public virtual void ToHtml(TextWriter s, Netlist netlist)
		{
			s.Write("<h2 id=\"t_" + Name.ToHtmlId() + "\">Type - <span class=\"" + CssClass + "\">" + Name.ToUpperInvariant().ToHtmlName() + "</span></h2>");
			s.Write("<dl>");
			s.Write("<dt>Name</dt><dd>" + Name.ToHtmlName() + "</dd>");
			s.Write("<dt>Ports</dt><dd>");
			HtmlPorts(s);
			s.Write("</dd><dt>Cells</dt><dd>");
			HtmlCells(s, netlist);
			s.Write("</dd>");
			s.Write("</dl>");
			if (!string.IsNullOrEmpty(Description))
				s.Write("<p>" + Description.ToHtml() + "</p>");
			if (!string.IsNullOrEmpty(DocUrl))
				s.Write("<p><a href=\"" + DocUrl.Replace("%t", Name.ToHtmlId()).ToHtml() + "\">View documentation</a></p>");
		}

		public string CssClass
		{
			get
			{
				return "bg_" + (string.IsNullOrEmpty(Color) ? "white" : Color);
			}
		}

		public virtual PointF? Center
		{
			get
			{
				if (!Coords.ContainsKey(""))
					return null;
				float x = (Coords[""][0][0] + Coords[""][0][2]) / 2.0f;
				float y = (Coords[""][0][1] + Coords[""][0][3]) / 2.0f;
				return new PointF(x, y);
			}
		}
	}
}

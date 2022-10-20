using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing;

namespace nlconv
{
	public class WireDefinition : ParserToken, IIntersectable
	{
		public readonly string               Name;
		public readonly WireClass            Class;
		public readonly List<WireConnection> Sources;
		public readonly List<WireConnection> Drains;
		public readonly string               Description;
		public readonly List<List<float>>    Coords;

		public WireDefinition(int pos, int line, int col, string name, WireClass cls, string desc)
			: base(pos, line, col)
		{
			Name        = name;
			Class       = cls;
			Description = desc;
			Sources     = new List<WireConnection>();
			Drains      = new List<WireConnection>();
			Coords      = new List<List<float>>();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("wire ");
			sb.Append(Name);
			switch (Class)
			{
				case WireClass.None:                             break;
				case WireClass.Ground:  sb.Append(":gnd");       break;
				case WireClass.Power:   sb.Append(":pwr");       break;
				case WireClass.Decoded: sb.Append(":dec");       break;
				case WireClass.Control: sb.Append(":ctl");       break;
				case WireClass.Clock:   sb.Append(":clk");       break;
				case WireClass.Data:    sb.Append(":data");      break;
				case WireClass.Address: sb.Append(":adr");       break;
				case WireClass.Reset:   sb.Append(":rst");       break;
				case WireClass.Analog:  sb.Append(":analog");    break;
				default:                sb.Append(":<invalid>"); break;
			}
			foreach (var src in Sources)
			{
				sb.Append(" ");
				sb.Append(src);
			}
			sb.Append(" ->");
			foreach (var drn in Drains)
			{
				sb.Append(" ");
				sb.Append(drn);
			}
			for (int i = 0; i < Coords.Count; i++)
			{
				sb.Append(" @");
				sb.Append(CoordString(Coords[i]));
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

		public virtual void HtmlConnections(TextWriter s, IList<WireConnection> l, Netlist netlist, WireConnection skip)
		{
			if (l.Count == 0 || (l.Count == 1 && l[0] == skip))
			{
				s.Write("-");
				return;
			}
			int count = 0;
			foreach (var x in l)
			{
				if (x == skip)
					continue;
				if (count != 0)
					s.Write(", ");
				count++;
				s.Write(x.ToHtml(netlist));
			}
			if (count > 1)
				s.Write(" (" + count + " total)");
		}

		public void HtmlSources(TextWriter s, Netlist netlist, WireConnection skip)
		{
			HtmlConnections(s, Sources, netlist, skip);
		}

		public void HtmlDrains(TextWriter s, Netlist netlist, WireConnection skip)
		{
			HtmlConnections(s, Drains, netlist, skip);
		}

		public virtual void ToHtml(TextWriter s, Netlist netlist)
		{
			s.Write("<h2 id=\"w_" + Name.ToHtmlId() + "\">Wire - <span class=\"" + CssClass + "\">" + Name.ToUpperInvariant().ToHtmlName() + "</span></h2>");
			s.Write("<dl>");
			s.Write("<dt>Name</dt><dd>" + Name.ToHtmlName() + "</dd>");
			s.Write("<dt>Class</dt><dd>" + ClassString + "</dd>");
			if (Coords.Count != 0 && !string.IsNullOrEmpty(netlist.MapUrl))
				s.Write("<dt>Location</dt><dd><a href=\"" + netlist.MapUrl + "&view=line[]&" + LineCoordString(Coords) + "\">Highlight on map</a></dd>");
			else
				s.Write("<dt>Location</dt><dd>-</dd>");
			s.Write("<dt>Driven by</dt><dd>");
			HtmlSources(s, netlist, null);
			s.Write("</dd><dt>Drives</dt><dd>");
			HtmlDrains(s, netlist, null);
			s.Write("</dd></dl>");
			if (!string.IsNullOrEmpty(Description))
				s.Write("<p>" + Description.ToHtml() + "</p>");
		}

		public string ClassString
		{
			get
			{
				switch (Class)
				{
					case WireClass.Ground:  return "GND";
					case WireClass.Power:   return "VDD";
					case WireClass.Decoded: return "decoded";
					case WireClass.Control: return "control";
					case WireClass.Clock:   return "clock";
					case WireClass.Data:    return "data";
					case WireClass.Address: return "address";
					case WireClass.Reset:   return "reset";
					case WireClass.Analog:  return "analog";
				}
				return "-";
			}
		}

		public string CssClass
		{
			get
			{
				switch (Class)
				{
					case WireClass.Ground:  return "bg_black";
					case WireClass.Power:   return "bg_red";
					case WireClass.Decoded: return "bg_orange";
					case WireClass.Control: return "bg_purple";
					case WireClass.Clock:   return "bg_magenta";
					case WireClass.Data:    return "bg_blue";
					case WireClass.Address: return "bg_yellow";
					case WireClass.Reset:   return "bg_turquoise";
					case WireClass.Analog:  return "bg_lime";
				}
				return "bg_blue";
			}
		}

		public Color Color
		{
			get
			{
				switch (Class)
				{
					case WireClass.Ground:  return Color.Black;
					case WireClass.Power:   return Color.Red;
					case WireClass.Decoded: return Color.Orange;
					case WireClass.Control: return Color.Purple;
					case WireClass.Clock:   return Color.Magenta;
					case WireClass.Data:    return Color.Blue;
					case WireClass.Address: return Color.Yellow;
					case WireClass.Reset:   return Color.Turquoise;
					case WireClass.Analog:  return Color.Green;
				}
				return Color.Blue;
			}
		}

		public virtual void Draw(Graphics g, float sx, float sy)
		{
			Pen pen = new Pen(Color, 5.0f);
			foreach (var c in Coords)
			{
				PointF[] pts = new PointF[c.Count / 2];
				for (int i = 0; i < c.Count / 2; i++)
					pts[i] = new PointF(c[i * 2] * sx, c[i * 2 + 1] * sy);
				g.DrawLines(pen, pts);
			}
		}

		public virtual bool Intersects(Box b)
		{
			float w = 0.079f;

			foreach (var c in Coords)
			{
				for (int i = 1; i < c.Count / 2; i++)
				{
					Line l = new Line(new Vector(c[(i - 1) * 2], c[(i - 1) * 2 + 1]),
					                  new Vector(c[i * 2], c[i * 2 + 1]));
					if (b.SegmentIntersects(l, w / 2.0f))
						return true;
				}
			}
			return false;
		}
	}
}

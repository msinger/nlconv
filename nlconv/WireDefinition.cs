using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace nlconv
{
	public class WireDefinition : ParserToken, IIntersectable
	{
		public readonly string               Name;
		public readonly string               Signal;
		public readonly bool                 Unchecked;
		public readonly List<WireConnection> Sources;
		public readonly List<WireConnection> Drains;
		public readonly string               Description;
		public readonly float                WireWidth;
		public readonly List<List<float>>    Coords;
		public readonly List<string>         Alias;

		public WireDefinition(Position pos, string name, string sig, bool unchk, string desc, float wire_width)
			: base(pos)
		{
			Name        = name;
			Signal      = sig;
			Unchecked   = unchk;
			Description = desc;
			WireWidth   = wire_width;
			Sources     = new List<WireConnection>();
			Drains      = new List<WireConnection>();
			Coords      = new List<List<float>>();
			Alias       = new List<string>();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("wire ");
			sb.Append(Name);
			if (!string.IsNullOrEmpty(Signal))
			{
				sb.Append(":");
				sb.Append(Signal);
			}
			if (Unchecked)
				sb.Append(" unchecked");
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
			if (Alias.Count != 0)
			{
				sb.Append("\nalias wire");
				foreach (string alias in new SortedSet<string>(Alias))
					sb.Append(" " + alias);
				sb.Append(" -> ");
				sb.Append(Name);
				sb.Append(";");
			}
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
			s.Write("<h2 id=\"w_" + Name.ToHtmlId() + "\">Wire - <span class=\"" + GetCssClass(netlist) + "\">" + Name.ToUpperInvariant().ToHtmlName() + "</span>");
			if (Alias.Count != 0)
			{
				s.Write(" (alias:");
				foreach (string alias in new SortedSet<string>(Alias))
					s.Write(" " + alias.ToUpperInvariant().ToHtmlName());
				s.Write(")");
			}
			s.Write("</h2>");
			s.Write("<dl>");
			s.Write("<dt>Name</dt><dd>" + Name.ToHtmlName() + "</dd>");
			s.Write("<dt>Signal Class</dt><dd>" + GetClassString(netlist).ToHtml() + "</dd>");
			if (Coords.Count != 0 && netlist.Strings.ContainsKey("map-url"))
				s.Write("<dt>Location</dt><dd><a href=\"" + netlist.Strings["map-url"] + "&view=w:" + Name.ToUrl() + "\">Highlight on map</a></dd>");
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

		public string GetClassString(Netlist netlist)
		{
			if (string.IsNullOrEmpty(Signal))
				return "-";
			if (string.IsNullOrEmpty(netlist.Signals[Signal].Description))
				return Signal;
			return netlist.Signals[Signal].Description;
		}

		public string GetCssClass(Netlist netlist)
		{
			if (string.IsNullOrEmpty(Signal))
				return "bg_blue";
			return "bg_" + netlist.Signals[Signal].Color;
		}

		public Color GetColor(Netlist netlist)
		{
			if (string.IsNullOrEmpty(Signal))
				return Color.Blue;
			return GetColorFromString(netlist.Signals[Signal].Color);
		}

		public virtual void Draw(Netlist netlist, Graphics g, float sx, float sy)
		{
			float scale = 1.0f;
			if (netlist.Strings.ContainsKey("png-scale"))
			{
				float t;
				if (float.TryParse(netlist.Strings["png-scale"],
				                   NumberStyles.AllowDecimalPoint,
				                   NumberFormatInfo.InvariantInfo,
				                   out t))
					scale = t;
			}
			Pen pen = new Pen(GetColor(netlist), 5.0f * scale);
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
			float w = 0.079f * WireWidth;

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

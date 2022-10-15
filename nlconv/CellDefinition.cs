using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing;

namespace nlconv
{
	public class CellDefinition : ParserToken
	{
		public readonly string           Name;
		public readonly string           Type;
		public readonly CellOrientation? Orientation;
		public readonly bool?            IsFlipped;
		public readonly bool             IsSpare, IsVirtual, IsComp, IsTrivial;
		public readonly string           Description;
		public readonly Dictionary<string, List<List<float>>> Coords;
		public readonly List<string>     Alias;

		public CellDefinition(int pos, int line, int col,
		                      string           name,
		                      string           type,
		                      CellOrientation? orientation,
		                      bool?            flipped,
		                      bool             spare,
		                      bool             virt,
		                      bool             comp,
		                      bool             trivial,
		                      string           desc)
			: base(pos, line, col)
		{
			Name        = name;
			Type        = type;
			Orientation = orientation;
			IsFlipped   = flipped;
			IsSpare     = spare;
			IsVirtual   = virt;
			IsComp      = comp;
			IsTrivial   = trivial;
			Description = desc;
			Coords      = new Dictionary<string, List<List<float>>>();
			Alias       = new List<string>();
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
			sb.Append("cell ");
			sb.Append(Name);
			sb.Append(":");
			sb.Append(Type);
			if (Orientation.HasValue)
			{
				switch (Orientation)
				{
					case CellOrientation.Rot0:   sb.Append(" rot0");      break;
					case CellOrientation.Rot90:  sb.Append(" rot90");     break;
					case CellOrientation.Rot180: sb.Append(" rot180");    break;
					case CellOrientation.Rot270: sb.Append(" rot270");    break;
					default:                     sb.Append(" <invalid>"); break;
				}
				if (IsFlipped.Value)
					sb.Append(", flip");
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
			if (IsSpare)   sb.Append(" spare");
			if (IsVirtual) sb.Append(" virtual");
			if (IsComp)    sb.Append(" comp");
			if (IsTrivial) sb.Append(" trivial");
			if (!string.IsNullOrEmpty(Description))
			{
				sb.Append(" \"");
				sb.Append(Description.Escape());
				sb.Append("\"");
			}
			sb.Append(";");
			if (Alias.Count != 0)
			{
				sb.Append("\nalias cell");
				foreach (string alias in new SortedSet<string>(Alias))
					sb.Append(" " + alias);
				sb.Append(" -> ");
				sb.Append(Name);
				sb.Append(";");
			}
			return sb.ToString();
		}

		public virtual void HtmlPorts(TextWriter s, Netlist netlist, bool output)
		{
			bool first = true;
			foreach (var p in netlist.Types[Type].Ports.Values)
			{
				if (output && p.Direction != PortDirection.Output && p.Direction != PortDirection.Tristate && p.Direction != PortDirection.Bidir && p.Direction != PortDirection.OutputLow && p.Direction != PortDirection.OutputHigh)
					continue;
				if (!output && p.Direction != PortDirection.Input && p.Direction != PortDirection.Bidir)
					continue;

				if (!first)
					s.Write("<br>");
				first = false;

				bool portLink = Coords.ContainsKey("") && Coords.ContainsKey(p.Name);
				s.Write("<span class=\"" + p.CssClass + "\">");
				if (portLink)
					s.Write("<a href=\"" + netlist.MapUrl + "&view=" + CoordString(Coords[""][0]) + "&" + PortCoordString(Coords[p.Name]) + "\">");
				s.Write(p.Name.ToHtmlName());
				if (portLink)
					s.Write("</a>");
				s.Write("</span>");

				if (output)
					s.Write(" &rarr; ");
				else
					s.Write(" &larr; ");

				WireConnection wc = new WireConnection(Name, p.Name);
				if (!netlist.Cons.ContainsKey(wc))
				{
					s.Write("-");
					continue;
				}

				WireDefinition w = netlist.Cons[wc];
				s.Write("<span class=\"" + w.CssClass + "\"><a href=\"#w_" + w.Name.ToHtmlId() + "\">" + w.Name.ToHtmlName() + "</a></span>");

				if (output)
				{
					s.Write(" &rarr; ");
					w.HtmlDrains(s, netlist, wc);
				}
				else
				{
					s.Write(" &larr; ");
					w.HtmlSources(s, netlist, wc);
				}
			}
			if (first)
				s.Write("-");
		}

		public virtual void HtmlOutputs(TextWriter s, Netlist netlist)
		{
			HtmlPorts(s, netlist, true);
		}

		public virtual void HtmlInputs(TextWriter s, Netlist netlist)
		{
			HtmlPorts(s, netlist, false);
		}

		public virtual void ToHtml(TextWriter s, Netlist netlist)
		{
			s.Write("<h2 id=\"c_" + Name.ToHtmlId() + "\">Cell - <span class=\"" + netlist.Types[Type].CssClass + "\">" + Name.ToUpperInvariant().ToHtmlName() + "</span>");
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
			s.Write("<dt>Type</dt><dd><a href=\"#t_" + Type.ToHtmlId() + "\">" + Type.ToHtmlName() + "</a></dd>");
			s.Write("<dt>Orientation</dt><dd>" + OrientationString + "</dd>");
			if (Coords.ContainsKey("") && !string.IsNullOrEmpty(netlist.MapUrl))
				s.Write("<dt>Location</dt><dd><a href=\"" + netlist.MapUrl + "&view=" + CoordString(Coords[""][0]) + "&" + BoxCoordString(Coords[""][0]) + "\">Highlight on map</a></dd>");
			else
				s.Write("<dt>Location</dt><dd>-</dd>");
			s.Write("<dt>Driven by</dt><dd>");
			HtmlInputs(s, netlist);
			s.Write("</dd><dt>Drives</dt><dd>");
			HtmlOutputs(s, netlist);
			s.Write("</dd></dl>");
			if (IsSpare)
				s.Write("<p>[SPARE] - This is a spare cell that has no relevant function.</p>");
			if (IsVirtual)
				s.Write("<p>[VIRTUAL] - This cell does not exist.</p>");
			if (IsComp)
				s.Write("<p>[COMP CLOCK] - This cell may not be drawn in the schematics, because it provides a complement clock for some latches or flip-flops. To simplify the schematics, complement clock connections of latches and flip-flops are not drawn.</p>");
			if (IsTrivial)
				s.Write("<p>[TRIVIAL] - This cell is not drawn in the schematics, because it is basically just wiring.</p>");
			if (!string.IsNullOrEmpty(Description))
				s.Write("<p>" + Description.ToHtml() + "</p>");
		}

		public string OrientationString
		{
			get
			{
				if (!Orientation.HasValue)
					return "-";
				StringBuilder sb = new StringBuilder();
				switch (Orientation)
				{
					case CellOrientation.Rot0:   sb.Append("not rotated");      break;
					case CellOrientation.Rot90:  sb.Append("rotated CW");       break;
					case CellOrientation.Rot180: sb.Append("rotated 180&deg;"); break;
					case CellOrientation.Rot270: sb.Append("rotated CCW");      break;
				}
				sb.Append(", ");
				if (IsFlipped.Value)
					sb.Append("flipped");
				else
					sb.Append("not flipped");
				return sb.ToString();
			}
		}

		public Color GetColor(Netlist netlist)
		{
			switch (netlist.Types[Type].Color)
			{
				case "red":       return Color.Red;
				case "lime":      return Color.Lime;
				case "blue":      return Color.Blue;
				case "yellow":    return Color.Yellow;
				case "cyan":      return Color.Cyan;
				case "magenta":   return Color.Magenta;
				case "orange":    return Color.Orange;
				case "purple":    return Color.Purple;
				case "turquoise": return Color.Turquoise;
				case "green":     return Color.Green;
				case "black":     return Color.Black;
			}
			return Color.Black;
		}

		public virtual void Draw(Netlist netlist, Graphics g, float sx, float sy)
		{
			if (IsVirtual || !Coords.ContainsKey("") || Orientation == null || IsFlipped == null)
				return;

			Pen   pen   = new Pen(GetColor(netlist), 3.0f);
			Brush brush = new SolidBrush(Color.FromArgb(20, IsSpare ? Color.Black : Color.White));

			var p = Coords[""][0];
			float x1 = System.Math.Min(p[0], p[2]);
			float y1 = System.Math.Min(p[1], p[3]);
			float x2 = System.Math.Max(p[0], p[2]);
			float y2 = System.Math.Max(p[1], p[3]);
			float x = x1 * sx + 1.0f;
			float y = y1 * sy + 1.0f;
			float w = (x2 - x1) * sx - 2.0f;
			float h = (y2 - y1) * sy - 2.0f;

			g.FillRectangle(brush, x, y, w, h);

			foreach (var port in netlist.Types[Type].Ports)
			{
				Func<float, float, (float, float)> fix = (tx, ty) => (tx, ty);

				var c = Coords.ContainsKey(port.Key) ? Coords[port.Key] : null;
				if (c == null)
				{
					if (!netlist.Types[Type].Coords.ContainsKey(""))
						continue;
					netlist.Types[Type].Coords.TryGetValue(port.Key, out c);

					var tc = netlist.Types[Type].Coords[""][0];
					float tx_mid = (tc[0] + tc[2]) / 2.0f;
					float ty_mid = (tc[1] + tc[3]) / 2.0f;
					float cx_mid = (Coords[""][0][0] + Coords[""][0][2]) / 2.0f;
					float cy_mid = (Coords[""][0][1] + Coords[""][0][3]) / 2.0f;

					switch (Orientation.Value)
					{
					case CellOrientation.Rot0:
						if (IsFlipped.Value)
							fix = (tx, ty) => ((tx - tx_mid) + cx_mid, -(ty - ty_mid) + cy_mid);
						else
							fix = (tx, ty) => ((tx - tx_mid) + cx_mid, (ty - ty_mid) + cy_mid);
						break;
					case CellOrientation.Rot90:
						if (IsFlipped.Value)
							fix = (tx, ty) => ((ty - ty_mid) + cx_mid, (tx - tx_mid) + cy_mid);
						else
							fix = (tx, ty) => (-(ty - ty_mid) + cx_mid, (tx - tx_mid) + cy_mid);
						break;
					case CellOrientation.Rot180:
						if (IsFlipped.Value)
							fix = (tx, ty) => (-(tx - tx_mid) + cx_mid, (ty - ty_mid) + cy_mid);
						else
							fix = (tx, ty) => (-(tx - tx_mid) + cx_mid, -(ty - ty_mid) + cy_mid);
						break;
					case CellOrientation.Rot270:
						if (IsFlipped.Value)
							fix = (tx, ty) => (-(ty - ty_mid) + cx_mid, -(tx - tx_mid) + cy_mid);
						else
							fix = (tx, ty) => ((ty - ty_mid) + cx_mid, -(tx - tx_mid) + cy_mid);
						break;
					}
				}
				if (c == null)
					continue;

				Color col = port.Value.Color;
				Pen small = new Pen(Color.FromArgb(100, col), 3.0f);
				Pen big   = new Pen(Color.FromArgb(100, col), 5.0f);

				foreach (var i in c)
				{
					if (i.Count == 2)
					{
						(float x, float y) pt = fix(i[0], i[1]);
						g.DrawLine(small, pt.x * sx - 10.0f, pt.y * sy - 10.0f, pt.x * sx + 10.0f, pt.y * sy + 10.0f);
						g.DrawLine(small, pt.x * sx - 10.0f, pt.y * sy + 10.0f, pt.x * sx + 10.0f, pt.y * sy - 10.0f);
					}
					else
					{
						PointF[] pts = new PointF[i.Count / 2];
						for (int j = 0; j < i.Count / 2; j++)
						{
							(float x, float y) pt = fix(i[j * 2], i[j * 2 + 1]);
							pts[j] = new PointF(pt.x * sx, pt.y * sy);
						}
						g.DrawLines(big, pts);
					}
				}
			}

			g.DrawRectangle(pen, x, y, w, h);
		}
	}
}

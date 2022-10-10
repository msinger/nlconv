using System.Text;
using System.IO;
using System.Collections.Generic;

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

		public virtual void HtmlPorts(TextWriter s, IEnumerable<PortDefinition> ports, IDictionary<string, CellDefinition> cells, IDictionary<string, TypeDefinition> types, IDictionary<WireConnection, WireDefinition> cons, string map, bool output)
		{
			bool first = true;
			foreach (var p in ports)
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
					s.Write("<a href=\"" + map + "&view=" + CoordString(Coords[""][0]) + "&" + PortCoordString(Coords[p.Name]) + "\">");
				s.Write(p.Name.ToHtmlName());
				if (portLink)
					s.Write("</a>");
				s.Write("</span>");

				if (output)
					s.Write(" &rarr; ");
				else
					s.Write(" &larr; ");

				WireConnection wc = new WireConnection(Name, p.Name);
				if (!cons.ContainsKey(wc))
				{
					s.Write("-");
					continue;
				}

				WireDefinition w = cons[wc];
				s.Write("<span class=\"" + w.CssClass + "\"><a href=\"#w_" + w.Name.ToHtmlId() + "\">" + w.Name.ToHtmlName() + "</a></span>");

				if (output)
				{
					s.Write(" &rarr; ");
					w.HtmlDrains(s, cells, types, map, wc);
				}
				else
				{
					s.Write(" &larr; ");
					w.HtmlSources(s, cells, types, map, wc);
				}
			}
			if (first)
				s.Write("-");
		}

		public virtual void HtmlOutputs(TextWriter s, IEnumerable<PortDefinition> ports, IDictionary<string, CellDefinition> cells, IDictionary<string, TypeDefinition> types, IDictionary<WireConnection, WireDefinition> cons, string map)
		{
			HtmlPorts(s, ports, cells, types, cons, map, true);
		}

		public virtual void HtmlInputs(TextWriter s, IEnumerable<PortDefinition> ports, IDictionary<string, CellDefinition> cells, IDictionary<string, TypeDefinition> types, IDictionary<WireConnection, WireDefinition> cons, string map)
		{
			HtmlPorts(s, ports, cells, types, cons, map, false);
		}

		public virtual void ToHtml(TextWriter s, IDictionary<string, TypeDefinition> types, IDictionary<string, CellDefinition> cells, IDictionary<WireConnection, WireDefinition> cons, string map)
		{
			s.Write("<h2 id=\"c_" + Name.ToHtmlId() + "\">Cell - <span class=\"" + types[Type].CssClass + "\">" + Name.ToUpperInvariant().ToHtmlName() + "</span>");
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
			if (Coords.ContainsKey(""))
				s.Write("<dt>Location</dt><dd><a href=\"" + map + "&view=" + CoordString(Coords[""][0]) + "&" + BoxCoordString(Coords[""][0]) + "\">Highlight on map</a></dd>");
			else
				s.Write("<dt>Location</dt><dd>-</dd>");
			s.Write("<dt>Driven by</dt><dd>");
			HtmlInputs(s, types[Type].Ports.Values, cells, types, cons, map);
			s.Write("</dd><dt>Drives</dt><dd>");
			HtmlOutputs(s, types[Type].Ports.Values, cells, types, cons, map);
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
	}
}

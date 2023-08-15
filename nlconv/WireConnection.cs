using System;
using System.Text;
using System.Collections.Generic;

namespace nlconv
{
	public class WireConnection : ParserToken, IEquatable<WireConnection>, IComparable<WireConnection>, IComparable
	{
		public readonly string Cell;
		public readonly string Port;

		public WireConnection(Position pos, string cell, string port) : base(pos)
		{
			Cell = cell;
			Port = port;
		}

		public WireConnection(string cell, string port) : this(new Position(), cell, port) { }

		public override string ToString()
		{
			return Cell + "." + Port;
		}

		public virtual int CompareTo(WireConnection other)
		{
			if ((object)other == null)
				return 1;
			int i = Cell.WithoutBars().CompareTo(other.Cell.WithoutBars());
			if (i != 0)
				return i;
			return Port.WithoutBars().CompareTo(other.Port.WithoutBars());
		}

		public int CompareTo(object other)
		{
			if ((object)other == null)
				return 1;
			if (!(other is WireConnection))
				throw new ArgumentException("Object is not a WireConnection.");
			return CompareTo((WireConnection)other);
		}

		public virtual bool Equals(WireConnection other)
		{
			if ((object)other == null)
				return false;
			return Cell == other.Cell && Port == other.Port;
		}

		public override bool Equals(object other)
		{
			return Equals(other as WireConnection);
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		public static bool operator ==(WireConnection x, WireConnection y)
		{
			return EqualityComparer<WireConnection>.Default.Equals(x, y);
		}

		public static bool operator !=(WireConnection x, WireConnection y)
		{
			return !EqualityComparer<WireConnection>.Default.Equals(x, y);
		}

		public virtual string ToHtml(Netlist netlist)
		{
			CellDefinition cell = netlist.Cells[Cell];
			Func<float, float, (float, float)> identity  = (x, y) => (x, y);
			Func<float, float, (float, float)> transform = cell.GetTransformation(netlist);
			var fix = identity;
			var c = (cell.CanDraw && cell.Coords.ContainsKey(Port)) ? cell.Coords[Port] : null;
			if (cell.CanDraw && c == null && netlist.Types[cell.Type].Center.HasValue)
			{
				netlist.Types[cell.Type].Coords.TryGetValue(Port, out c);
				fix = transform;
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<a href=\"#c_" + Cell.ToHtmlId() + "\">");
			sb.Append(Cell.ToHtmlName());
			sb.Append("</a>.<span class=\"" + netlist.Types[cell.Type].Ports[Port].CssClass + "\">");
			if (c != null && netlist.Strings.ContainsKey("map-url"))
				sb.Append("<a href=\"" + netlist.Strings["map-url"] + "&view=c:" + cell.Name.ToUrl() + "&" + PortCoordString(c, fix) + "\">");
			sb.Append(Port.ToHtmlName());
			if (c != null && netlist.Strings.ContainsKey("map-url"))
				sb.Append("</a>");
			sb.Append("</span>");
			return sb.ToString();
		}
	}
}

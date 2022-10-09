using System;
using System.Text;
using System.Collections.Generic;

namespace nlconv
{
	public class WireConnection : ParserToken, IEquatable<WireConnection>, IComparable<WireConnection>, IComparable
	{
		public readonly string Cell;
		public readonly string Port;

		public WireConnection(int pos, int line, int col, string cell, string port) : base(pos, line, col)
		{
			Cell = cell;
			Port = port;
		}

		public WireConnection(string cell, string port) : base()
		{
			Cell = cell;
			Port = port;
		}

		public override string ToString()
		{
			return Cell + "." + Port;
		}

		public virtual int CompareTo(WireConnection other)
		{
			if ((object)other == null)
				return 1;
			int i = Cell.ToNameWithoutBars().CompareTo(other.Cell.ToNameWithoutBars());
			if (i != 0)
				return i;
			return Port.ToNameWithoutBars().CompareTo(other.Port.ToNameWithoutBars());
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

		public virtual string ToHtml(IDictionary<string, CellDefinition> cells, IDictionary<string, TypeDefinition> types, string map)
		{
			bool portLink = cells[Cell].Coords.ContainsKey("") && cells[Cell].Coords.ContainsKey(Port);
			StringBuilder sb = new StringBuilder();
			sb.Append("<a href=\"#c_" + Cell.ToHtmlId() + "\">");
			sb.Append(Cell.ToHtmlName());
			sb.Append("</a>.<span class=\"" + types[cells[Cell].Type].Ports[Port].CssClass + "\">");
			if (portLink)
				sb.Append("<a href=\"" + map + "?view=" + CoordString(cells[Cell].Coords[""][0]) + "&" + PortCoordString(cells[Cell].Coords[Port]) + "\">");
			sb.Append(Port.ToHtmlName());
			if (portLink)
				sb.Append("</a>");
			sb.Append("</span>");
			return sb.ToString();
		}
	}
}

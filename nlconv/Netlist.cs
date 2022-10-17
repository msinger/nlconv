using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Drawing;

namespace nlconv
{
	public partial class Netlist : NetlistLexerBase
	{
		public readonly SortedDictionary<string, TypeDefinition>   Types;
		public readonly SortedDictionary<string, CellDefinition>   Cells;
		public readonly SortedDictionary<string, WireDefinition>   Wires;
		public readonly Dictionary<WireConnection, WireDefinition> Cons;

		public string DefaultDocUrl = "";
		public string MapUrl        = "";

		public Netlist() : base()
		{
			Types = new SortedDictionary<string, TypeDefinition>();
			Cells = new SortedDictionary<string, CellDefinition>();
			Wires = new SortedDictionary<string, WireDefinition>();
			Cons  = new Dictionary<WireConnection, WireDefinition>();
		}

		protected static void ParseEOT(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.EOT)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Semicolon expected.");
		}

		protected static PortDirection ParsePortDirection(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Port direction expected.");
			switch (n.Value.String.ToLowerInvariant())
			{
			case "in":
				return PortDirection.Input;
			case "out":
				return PortDirection.Output;
			case "tri":
				return PortDirection.Tristate;
			case "inout":
				return PortDirection.Bidir;
			case "out0":
				return PortDirection.OutputLow;
			case "out1":
				return PortDirection.OutputHigh;
			case "nc":
				return PortDirection.NotConnected;
			default:
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Invalid port direction.");
			}
		}

		protected static PortDefinition ParsePortDefinition(ref LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Port name expected.");

			PortDirection d = PortDirection.Input;
			n = n.Next;
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				d = ParsePortDirection(n);
				n = n.Next;
			}

			return new PortDefinition(me.Value.Pos, me.Value.Line, me.Value.Col, me.Value.String.CanonicalizeBars(), d);
		}

		protected static IList<PortDefinition> ParsePortDefinitionList(ref LinkedListNode<LexerToken> n)
		{
			List<PortDefinition> l = new List<PortDefinition>();
			if (n.Value.Type != LexerTokenType.Name)
				return l;
			l.Add(ParsePortDefinition(ref n));
			l.AddRange(ParsePortDefinitionList(ref n));
			return l;
		}

		protected TypeDefinition ParseTypeDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "type")
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Type definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Type name expected.");
			string name = n.Value.String;
			n = n.Next;

			string color = "";
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				color = ParseColor(n);
				n = n.Next;
			}

			var ports   = ParsePortDefinitionList(ref n);
			var coords  = ParseCoordList(ref n);
			string desc = "";
			string doc  = DefaultDocUrl;

			while (n.Value.Type == LexerTokenType.String)
			{
				desc += n.Value.String;
				n = n.Next;
			}

			if (n.Value.Type == LexerTokenType.Name && n.Value.String.ToLowerInvariant() == "doc")
			{
				n = n.Next;
				doc = "";
				while (n.Value.Type == LexerTokenType.String)
				{
					doc += n.Value.String;
					n = n.Next;
				}
			}

			TypeDefinition t = new TypeDefinition(me.Value.Pos, me.Value.Line, me.Value.Col, name.CanonicalizeBars(), color, desc, doc);

			foreach (var p in ports)
			{
				if (t.Ports.ContainsKey(p.Name))
					throw new NetlistFormatException(p.Pos, p.Line, p.Col, "Port name already in use.");
				t.Ports.Add(p.Name, p);
			}

			foreach (var kvp in coords)
				t.AddCoords(kvp.Key, kvp.Value);

			ParseEOT(n);
			return t;
		}

		protected static string ParseColor(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Color expected.");
			string color = n.Value.String.ToLowerInvariant();
			switch (color)
			{
			case "red":
			case "lime":
			case "blue":
			case "yellow":
			case "cyan":
			case "magenta":
			case "orange":
			case "purple":
			case "turquoise":
			case "green":
			case "black":
				return color;
			default:
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Invalid color.");
			}
		}

		protected static void ParseCellOrientation(ref LinkedListNode<LexerToken> n, out CellOrientation? o, out bool? f)
		{
			o = null;
			f = null;

			if (n.Value.Type != LexerTokenType.Name)
				return;

			switch (n.Value.String.ToLowerInvariant())
			{
			case "rot0":
				o = CellOrientation.Rot0;
				break;
			case "rot90":
				o = CellOrientation.Rot90;
				break;
			case "rot180":
				o = CellOrientation.Rot180;
				break;
			case "rot270":
				o = CellOrientation.Rot270;
				break;
			default:
				return;
			}
			n = n.Next;

			f = false;
			if (n.Value.Type == LexerTokenType.Comma)
			{
				n = n.Next;
				if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "flip")
					throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Invalid cell orientation.");
				f = true;
				n = n.Next;
			}
		}

		protected static float ParseFloat(ref LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			float f = 1.0f;
			if (n.Value.Type ==  LexerTokenType.Plus || n.Value.Type == LexerTokenType.Minus)
			{
				f = (n.Value.Type == LexerTokenType.Minus) ? -1.0f : 1.0f;
				n = n.Next;
			}

			if (n.Value.Type != LexerTokenType.Value)
				throw new NetlistFormatException(me.Value.Pos, me.Value.Line, me.Value.Col, "Float number expected.");
			f *= n.Value.Value;
			n = n.Next;

			return f;
		}

		protected static List<float> ParseFloatList(ref LinkedListNode<LexerToken> n)
		{
			List<float> l = new List<float>();
			l.Add(ParseFloat(ref n));
			if (n.Value.Type == LexerTokenType.Comma)
			{
				n = n.Next;
				l.AddRange(ParseFloatList(ref n));
			}
			return l;
		}

		protected static KeyValuePair<string, List<float>> ParseCoord(ref LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			string name = "";
			if (n.Value.Type == LexerTokenType.Name)
			{
				name = n.Value.String.CanonicalizeBars();
				n = n.Next;
			}

			if (n.Value.Type != LexerTokenType.At)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Coordinate list (@) expected.");
			n = n.Next;

			return new KeyValuePair<string, List<float>>(name, ParseFloatList(ref n));
		}

		protected static IList<KeyValuePair<string, List<float>>> ParseCoordList(ref LinkedListNode<LexerToken> n)
		{
			List<KeyValuePair<string, List<float>>> l = new List<KeyValuePair<string, List<float>>>();
			if (n.Value.Type != LexerTokenType.At && !(n.Value.Type == LexerTokenType.Name && n.Next.Value.Type == LexerTokenType.At))
				return l;
			l.Add(ParseCoord(ref n));
			l.AddRange(ParseCoordList(ref n));
			return l;
		}

		protected static CellDefinition ParseCellDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "cell")
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Cell definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Cell name expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Colon)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Colon expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Cell type expected.");
			string t = n.Value.String.CanonicalizeBars();
			n = n.Next;

			CellOrientation? o;
			bool? f;
			ParseCellOrientation(ref n, out o, out f);

			var coords = ParseCoordList(ref n);

			bool   sp   = false;
			bool   vr   = false;
			bool   cp   = false;
			bool   tr   = false;
			string desc = "";

			while (n.Value.Type == LexerTokenType.Name)
			{
				if (n.Value.String.ToLowerInvariant() == "spare")
					sp = true;
				else if (n.Value.String.ToLowerInvariant() == "virtual")
					vr = true;
				else if (n.Value.String.ToLowerInvariant() == "comp")
					cp = true;
				else if (n.Value.String.ToLowerInvariant() == "trivial")
					tr = true;
				else
					break;
				n = n.Next;
			}

			while (n.Value.Type == LexerTokenType.String)
			{
				desc += n.Value.String;
				n = n.Next;
			}

			CellDefinition c = new CellDefinition(me.Value.Pos, me.Value.Line, me.Value.Col, me.Next.Value.String.CanonicalizeBars(), t, o, f, sp, vr, cp, tr, desc);

			foreach (var kvp in coords)
				c.AddCoords(kvp.Key, kvp.Value);

			ParseEOT(n);
			return c;
		}

		protected static WireClass ParseWireClass(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Wire class expected.");
			switch (n.Value.String.ToLowerInvariant())
			{
			case "none":
				return WireClass.None;
			case "gnd":
				return WireClass.Ground;
			case "pwr":
				return WireClass.Power;
			case "dec":
				return WireClass.Decoded;
			case "ctl":
				return WireClass.Control;
			case "clk":
				return WireClass.Clock;
			case "data":
				return WireClass.Data;
			case "adr":
				return WireClass.Address;
			case "rst":
				return WireClass.Reset;
			case "analog":
				return WireClass.Analog;
			default:
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Invalid wire class.");
			}
		}

		protected static WireConnection ParseWireConnection(ref LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Cell name expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Dot)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Dot (.) expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Port name expected.");
			n = n.Next;

			return new WireConnection(me.Value.Pos, me.Value.Line, me.Value.Col, me.Value.String.CanonicalizeBars(), me.Next.Next.Value.String.CanonicalizeBars());
		}

		protected static IList<WireConnection> ParseWireConnectionList(ref LinkedListNode<LexerToken> n)
		{
			List<WireConnection> l = new List<WireConnection>();
			if (!(n.Value.Type == LexerTokenType.Name &&
			      n.Next.Value.Type == LexerTokenType.Dot &&
			      n.Next.Next.Value.Type == LexerTokenType.Name))
				return l;
			l.Add(ParseWireConnection(ref n));
			l.AddRange(ParseWireConnectionList(ref n));
			return l;
		}

		protected static WireDefinition ParseWireDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "wire")
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Wire definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Wire name expected.");
			n = n.Next;

			WireClass cls = WireClass.None;
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				cls = ParseWireClass(n);
				n = n.Next;
			}

			var sources = ParseWireConnectionList(ref n);
			var drains  = (IList<WireConnection>)null;
			if (n.Value.Type == LexerTokenType.To)
			{
				n = n.Next;
				drains = ParseWireConnectionList(ref n);
			}

			var coords  = ParseCoordList(ref n);
			string desc = "";

			while (n.Value.Type == LexerTokenType.String)
			{
				desc += n.Value.String;
				n = n.Next;
			}

			WireDefinition w = new WireDefinition(me.Value.Pos, me.Value.Line, me.Value.Col, me.Next.Value.String.CanonicalizeBars(), cls, desc);

			w.Sources.AddRange(sources);

			if (drains != null)
				w.Drains.AddRange(drains);

			foreach (var kvp in coords)
				w.Coords.Add(kvp.Value); // We just ignore the string before the @

			ParseEOT(n);
			return w;
		}

		protected static AliasDefinition ParseAliasDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "alias")
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Alias definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "cell")
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Cell indicator expected.");
			n = n.Next;

			List<string> l = new List<string>();
			while (n.Value.Type == LexerTokenType.Name)
			{
				string s = n.Value.String.CanonicalizeBars();

				if (l.Contains(s))
					throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Duplicate alias found.");

				l.Add(s);
				n = n.Next;
			}

			if (l.Count == 0)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "At least one alias expected.");

			if (n.Value.Type != LexerTokenType.To)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Arrow (->) expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Cell name expected.");
			n = n.Next;

			AliasDefinition a = new AliasDefinition(me.Value.Pos, me.Value.Line, me.Value.Col, n.Previous.Value.String.CanonicalizeBars());
			a.Alias.AddRange(l);

			ParseEOT(n);
			return a;
		}

		protected IList<ParserToken> Parse(LinkedListNode<LexerToken> n)
		{
			switch (n.Value.Type)
			{
			case LexerTokenType.EOT:
				ParseEOT(n);
				return new ParserToken[] { };
			case LexerTokenType.Name:
				switch (n.Value.String.ToLowerInvariant())
				{
				case "type":
					return new ParserToken[] { ParseTypeDefinition(n) };
				case "cell":
					return new ParserToken[] { ParseCellDefinition(n) };
				case "wire":
					return new ParserToken[] { ParseWireDefinition(n) };
				case "alias":
					return new ParserToken[] { ParseAliasDefinition(n) };
				default:
					throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Invalid statement.");
				}
			}

			throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Unexpected token encountered by top level parser.");
		}

		private int pos     = 0;
		private int lineNum = 1;
		private LinkedList<LexerToken> fifo = new LinkedList<LexerToken>();

		private LinkedListNode<LexerToken> DequeueStatement()
		{
			bool hasEOT = false;
			foreach (LexerToken t in fifo)
				if (t.Type == LexerTokenType.EOT)
					hasEOT = true;
			if (!hasEOT)
				return null;

			LinkedList<LexerToken> l = new LinkedList<LexerToken>();
			while(true)
			{
				LinkedListNode<LexerToken> n = fifo.First;
				LexerToken                 t = n.Value;
				fifo.RemoveFirst();
				l.AddLast(t);
				if (t.Type == LexerTokenType.EOT)
					break;
			}

			return l.First;
		}

		protected void CheckCell(CellDefinition cell)
		{
			if (!Types.ContainsKey(cell.Type))
				throw new NetlistFormatException(cell.Pos, cell.Line, cell.Col, "Type '" + cell.Type + "' not found.");

			TypeDefinition t = Types[cell.Type];

			foreach (var kvp in cell.Coords)
			{
				if (kvp.Key == "")
				{
					if (kvp.Value.Count != 1)
						throw new NetlistFormatException(cell.Pos, cell.Line, cell.Col, "Multiple cell coordinates.");
					if (kvp.Value[0].Count != 4)
						throw new NetlistFormatException(cell.Pos, cell.Line, cell.Col, "Cell coordinates don't describe a rectangle (need four numbers Y1,X1,Y2,X2).");
				}
				else
				{
					if (!t.Ports.ContainsKey(kvp.Key))
						throw new NetlistFormatException(cell.Pos, cell.Line, cell.Col, "Type '" + cell.Type + "' doesn't have a port named '" + kvp.Key + "'.");
					foreach (var l in kvp.Value)
					{
						if ((l.Count & 1) != 0)
							throw new NetlistFormatException(cell.Pos, cell.Line, cell.Col, "Cell port '" + kvp.Key + "' has odd number of coordinates.");
						if (l.Count == 0)
							throw new NetlistFormatException(cell.Pos, cell.Line, cell.Col, "Cell port '" + kvp.Key + "' has no coordinates.");
					}
				}
			}
		}

		protected void CheckWire(WireDefinition wire)
		{
			List<WireConnection> both = new List<WireConnection>();
			both.AddRange(wire.Sources);
			both.AddRange(wire.Drains);

			foreach (var c in both)
			{
				if (!Cells.ContainsKey(c.Cell))
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Cell '" + c.Cell + "' not found.");

				CellDefinition cell = Cells[c.Cell];
				TypeDefinition t = Types[cell.Type];

				if (!t.Ports.ContainsKey(c.Port))
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Cell '" + c.Cell + "' (type '" + cell.Type + "') doesn't have a port named '" + c.Port + "'.");

				PortDefinition p = t.Ports[c.Port];

				// Multiple identical WireConnections in list?
				if (both.IndexOf(c) != both.LastIndexOf(c))
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Connection '" + c.ToString() + "' listed more than once.");

				if (p.Direction == PortDirection.NotConnected)
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') mustn't have a connection.");
			}

			int drvTri  = 0;
			int drvOut0 = 0;
			int drvOut1 = 0;
			foreach (var c in wire.Sources)
			{
				CellDefinition cell = Cells[c.Cell];
				TypeDefinition t = Types[cell.Type];
				PortDefinition p = t.Ports[c.Port];

				if (p.Direction != PortDirection.Output && p.Direction != PortDirection.Tristate && p.Direction != PortDirection.Bidir && p.Direction != PortDirection.OutputLow && p.Direction != PortDirection.OutputHigh)
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') in source list is not an output or tri-state.");

				if (p.Direction == PortDirection.Output && wire.Sources.Count != 1)
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') in source list is an output (not tri-state), but there are multiple entries in source list.");

				drvTri  |= p.Direction == PortDirection.Tristate || p.Direction == PortDirection.Bidir ? 1 : 0;
				drvOut0 |= p.Direction == PortDirection.OutputLow  ? 1 : 0;
				drvOut1 |= p.Direction == PortDirection.OutputHigh ? 1 : 0;

				if (drvTri + drvOut0 + drvOut1 > 1)
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') has incompatible/short-circuiting drivers in source list (combination of tri-state, bidir, output-low or output-high).");
			}

			foreach (var c in wire.Drains)
			{
				CellDefinition cell = Cells[c.Cell];
				TypeDefinition t = Types[cell.Type];
				PortDefinition p = t.Ports[c.Port];

				if (p.Direction != PortDirection.Input)
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') in drain list is not an input.");
			}

			foreach (var c in wire.Sources)
			{
				CellDefinition cell = Cells[c.Cell];
				TypeDefinition t = Types[cell.Type];
				PortDefinition p = t.Ports[c.Port];
				if (p.Direction == PortDirection.Bidir)
					wire.Drains.Add(c);
			}

			foreach (var l in wire.Coords)
			{
				if ((l.Count & 1) != 0)
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Wire segment has odd number of coordinates.");
				if (l.Count < 4)
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Wire segment has not enough coordinates (<4) to describe a line.");
			}
		}

		public void WriteLine(string line)
		{
			LinkedListNode<LexerToken> lex = Lex(line, lineNum, ref pos);
			lineNum++;
			for (LinkedListNode<LexerToken> n = lex; n != null; n = n.Next)
				fifo.AddLast(n.Value);

			LinkedListNode<LexerToken> statement;
			while ((statement = DequeueStatement()) != null)
			{
				foreach (ParserToken t in Parse(statement))
				{
					if (t is TypeDefinition)
					{
						TypeDefinition td = (TypeDefinition)t;
						if (Types.ContainsKey(td.Name))
							throw new NetlistFormatException(t.Pos, t.Line, t.Col, "Type name already in use.");
						Types.Add(td.Name, td);
					}
					else if (t is CellDefinition)
					{
						CellDefinition cd = (CellDefinition)t;
						if (Cells.ContainsKey(cd.Name))
							throw new NetlistFormatException(t.Pos, t.Line, t.Col, "Cell name already in use.");
						Cells.Add(cd.Name, cd);
					}
					else if (t is WireDefinition)
					{
						WireDefinition wd = (WireDefinition)t;
						if (Wires.ContainsKey(wd.Name))
							throw new NetlistFormatException(t.Pos, t.Line, t.Col, "Wire name already in use.");
						Wires.Add(wd.Name, wd);
					}
					else if (t is AliasDefinition)
					{
						AliasDefinition ad = (AliasDefinition)t;
						if (!Cells.ContainsKey(ad.Name))
							throw new NetlistFormatException(t.Pos, t.Line, t.Col, "No matching cell definition found prior to this alias definition.");
						Cells[ad.Name].Alias.AddRange(ad.Alias);
					}
					else
					{
						throw new NetlistFormatException(t.Pos, t.Line, t.Col, "Unknown parser token.");
					}
				}
			}
		}

		public void Flush()
		{
			if (fifo.Count != 0)
			{
				LexerToken t = fifo.First.Value;
				throw new NetlistFormatException(t.Pos, t.Line, t.Col, "End of file expected.");
			}

			foreach (var kvp in Cells)
				CheckCell(kvp.Value);
			foreach (var kvp in Wires)
				CheckWire(kvp.Value);

			// Check that there is no cell with a port that has more than one connection to a wire.
			foreach(var kvp in Wires)
			{
				List<WireConnection> both = new List<WireConnection>();
				both.AddRange(kvp.Value.Sources);
				foreach (var wc in kvp.Value.Drains)
					if (!kvp.Value.Sources.Contains(wc)) // Bidirectional ports exist on both sides, add only once here
						both.Add(wc);
				foreach (var wc in both)
				{
					if (Cons.ContainsKey(wc))
						throw new NetlistFormatException(wc.Pos, wc.Line, wc.Col, "Connection '" + wc.ToString() + "' already made to wire '" + Cons[wc].Name + "'.");
					Cons.Add(wc, kvp.Value);
				}
			}

			foreach(var kvp in Wires)
			{
				kvp.Value.Sources.Sort();
				kvp.Value.Drains.Sort();
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var x in Types)
				sb.AppendLine(x.Value.ToString());
			foreach (var x in Cells)
				sb.AppendLine(x.Value.ToString());
			foreach (var x in Wires)
				sb.AppendLine(x.Value.ToString());
			return sb.ToString();
		}

		public virtual void ToHtml(TextWriter s)
		{
			List<string> types = new List<string>(Types.Keys);
			List<string> cells = new List<string>(Cells.Keys);
			List<string> wires = new List<string>(Wires.Keys);

			types.Sort(NetlistNameComparer.Default);
			cells.Sort(NetlistNameComparer.Default);
			wires.Sort(NetlistNameComparer.Default);

			foreach (string x in types)
				Types[x].ToHtml(s, this);
			foreach (string x in cells)
				Cells[x].ToHtml(s, this);
			foreach (string x in wires)
				Wires[x].ToHtml(s, this);
		}

		public virtual void DrawCells(Graphics g, float sx, float sy)
		{
			foreach (var x in Cells)
				x.Value.Draw(this, g, sx, sy);
		}

		public virtual void DrawWires(Graphics g, float sx, float sy)
		{
			foreach (var x in Wires)
				x.Value.Draw(g, sx, sy);
		}

		public virtual void DrawLabels(Graphics g, float sx, float sy)
		{
			foreach (var x in Cells)
				x.Value.DrawLabels(g, sx, sy);
		}
	}
}

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
		public readonly SortedDictionary<string, TypeDefinition>     Types;
		public readonly SortedDictionary<string, CellDefinition>     Cells;
		public readonly SortedDictionary<string, WireDefinition>     Wires;
		public readonly Dictionary<WireConnection, WireDefinition>   Cons;
		public readonly List<LabelDefinition>                        Labels;
		public readonly SortedDictionary<string, CategoryDefinition> Categories;

		public string DefaultDocUrl = "";
		public string MapUrl        = "";

		public Netlist() : base()
		{
			Types      = new SortedDictionary<string, TypeDefinition>();
			Cells      = new SortedDictionary<string, CellDefinition>();
			Wires      = new SortedDictionary<string, WireDefinition>();
			Cons       = new Dictionary<WireConnection, WireDefinition>();
			Labels     = new List<LabelDefinition>();
			Categories = new SortedDictionary<string, CategoryDefinition>();
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
			var coords  = ParseCoordList(ref n, true);
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
			case "pink":
			case "navy":
			case "yellow":
			case "cyan":
			case "magenta":
			case "orange":
			case "purple":
			case "teal":
			case "green":
			case "brown":
			case "gray":
			case "black":
			case "white":
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
			return ParseCoord(ref n, false);
		}

		protected static KeyValuePair<string, List<float>> ParseCoord(ref LinkedListNode<LexerToken> n, bool named)
		{
			LinkedListNode<LexerToken> me = n;

			string name = "";
			if (named && n.Value.Type == LexerTokenType.Name)
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
			return ParseCoordList(ref n, false);
		}

		protected static IList<KeyValuePair<string, List<float>>> ParseCoordList(ref LinkedListNode<LexerToken> n, bool named)
		{
			List<KeyValuePair<string, List<float>>> l = new List<KeyValuePair<string, List<float>>>();
			if (n.Value.Type != LexerTokenType.At && !(named && n.Value.Type == LexerTokenType.Name && n.Next.Value.Type == LexerTokenType.At))
				return l;
			l.Add(ParseCoord(ref n, named));
			l.AddRange(ParseCoordList(ref n, named));
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

			var coords = ParseCoordList(ref n, true);

			bool   sp   = false;
			bool   vr   = false;
			bool   cp   = false;
			bool   tr   = false;
			string desc = "";
			string cat  = "";

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

			if (n.Value.Type == LexerTokenType.To)
			{
				n = n.Next;
				if (n.Value.Type != LexerTokenType.Name)
					throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Category name expected.");
				cat = n.Value.String;
				n = n.Next;
			}

			while (n.Value.Type == LexerTokenType.String)
			{
				desc += n.Value.String;
				n = n.Next;
			}

			CellDefinition c = new CellDefinition(me.Value.Pos, me.Value.Line, me.Value.Col, me.Next.Value.String.CanonicalizeBars(), t, o, f, sp, vr, cp, tr, desc, cat);

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

		protected static void ParseAlignment(ref LinkedListNode<LexerToken> n, out Alignment? a)
		{
			a = null;

			if (n.Value.Type != LexerTokenType.Name)
				return;

			switch (n.Value.String.ToLowerInvariant())
			{
			case "center":
				a = Alignment.Center;
				break;
			case "top-left":
				a = Alignment.TopLeft;
				break;
			case "top-center":
				a = Alignment.TopCenter;
				break;
			case "top-right":
				a = Alignment.TopRight;
				break;
			case "center-left":
				a = Alignment.CenterLeft;
				break;
			case "center-right":
				a = Alignment.CenterRight;
				break;
			case "bottom-left":
				a = Alignment.BottomLeft;
				break;
			case "bottom-center":
				a = Alignment.BottomCenter;
				break;
			case "bottom-right":
				a = Alignment.BottomRight;
				break;
			default:
				return;
			}
			n = n.Next;
		}

		protected static LabelDefinition ParseLabelDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "label")
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Label definition expected.");
			n = n.Next;

			string text = "";
			while (n.Value.Type == LexerTokenType.String)
			{
				text += n.Value.String;
				n = n.Next;
			}

			string color = "black";
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				color = ParseColor(n);
				n = n.Next;
			}

			var n_sz = n;
			float size = ParseFloat(ref n);
			if (size < 0.0f)
				throw new NetlistFormatException(n_sz.Value.Pos, n_sz.Value.Line, n_sz.Value.Col, "Size must be positive.");

			CellOrientation? o;
			bool? f;
			ParseCellOrientation(ref n, out o, out f);
			if (!o.HasValue) o = CellOrientation.Rot0;
			if (!f.HasValue) f = false;

			var n_c = n;
			var coords = ParseCoord(ref n);
			if (coords.Value.Count != 2)
				throw new NetlistFormatException(n_c.Value.Pos, n_c.Value.Line, n_c.Value.Col, "Coordinates must be one point (X,Y).");

			Alignment? a;
			ParseAlignment(ref n, out a);
			if (!a.HasValue) a = Alignment.Center;

			LabelDefinition l = new LabelDefinition(me.Value.Pos, me.Value.Line, me.Value.Col, text, color, size, o.Value, f.Value, new Vector(coords.Value[0], coords.Value[1]), a.Value);

			ParseEOT(n);
			return l;
		}

		protected static CategoryDefinition ParseCategoryDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "category")
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Category definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, n.Value.Line, n.Value.Col, "Wire name expected.");
			string name = n.Value.String;
			n = n.Next;

			string color = "";
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				color = ParseColor(n);
				n = n.Next;
			}

			CategoryDefinition c = new CategoryDefinition(me.Value.Pos, me.Value.Line, me.Value.Col, name, color);

			ParseEOT(n);
			return c;
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
				case "label":
					return new ParserToken[] { ParseLabelDefinition(n) };
				case "category":
					return new ParserToken[] { ParseCategoryDefinition(n) };
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

			if (!string.IsNullOrEmpty(cell.Category) && !Categories.ContainsKey(cell.Category))
				throw new NetlistFormatException(cell.Pos, cell.Line, cell.Col, "Category '" + cell.Category + "' not found.");

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

		// Check if all connections in the sources list are outputs that belong to inverters (cells with exactly
		// one input and exactly one output), which have their inputs driven by the same wire.
		protected bool AreAllParallelInverters(List<WireConnection> sources)
		{
			if (sources.Count < 2)
				return false;

			CellDefinition c0  = Cells[sources[0].Cell];
			TypeDefinition t0  = Types[c0.Type];
			PortDefinition p0o = t0.Ports[sources[0].Port];
			PortDefinition p0i = null;

			if (t0.Ports.Count != 2 || p0o.Direction != PortDirection.Output)
				return false;
			foreach (PortDefinition p_other in t0.Ports.Values)
			{
				if (p_other == p0o)
					continue;
				if (p_other.Direction != PortDirection.Input)
					return false;
				p0i = p_other;
			}

			WireDefinition w0;
			Cons.TryGetValue(new WireConnection(c0.Name, p0i.Name), out w0);

			foreach (WireConnection con in sources)
			{
				CellDefinition c  = Cells[con.Cell];
				TypeDefinition t  = Types[c.Type];
				PortDefinition po = t.Ports[con.Port];
				PortDefinition pi = null;

				if (t.Ports.Count != 2 || po.Direction != PortDirection.Output)
					return false;
				foreach (PortDefinition p_other in t.Ports.Values)
				{
					if (p_other == po)
						continue;
					if (p_other.Direction != PortDirection.Input)
						return false;
					pi = p_other;
				}

				WireDefinition w;
				Cons.TryGetValue(new WireConnection(c.Name, pi.Name), out w);

				if (w0 != w)
					return false;
			}

			return true;
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

				if (p.Direction == PortDirection.Output && wire.Sources.Count != 1 && !AreAllParallelInverters(wire.Sources))
					throw new NetlistFormatException(wire.Pos, wire.Line, wire.Col, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') in source list is an output (not tri-state), but there are multiple entries in source list, which do not come from parallel inverters.");

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
					else if (t is LabelDefinition)
					{
						Labels.Add((LabelDefinition)t);
					}
					else if (t is CategoryDefinition)
					{
						CategoryDefinition cd = (CategoryDefinition)t;
						if (Categories.ContainsKey(cd.Name))
							throw new NetlistFormatException(t.Pos, t.Line, t.Col, "Category name already in use.");
						Categories.Add(cd.Name, cd);
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
			foreach (var x in Labels)
				sb.AppendLine(x.ToString());
			foreach (var x in Categories)
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
			foreach (var x in Labels)
				x.Draw(g, sx, sy);
		}

		public virtual void DrawFloorplan(Graphics g, float sx, float sy)
		{
			foreach (var x in Cells)
				x.Value.DrawFloorplan(this, g, sx, sy);
		}

		public virtual void ToJavaScript(TextWriter s)
		{
			var tree = new QuadTree(new Vector(-128.0f, 128.0f), 128.0f, 64, 6);

			var c = new Dictionary<string, List<string>>();
			s.WriteLine("var cells_cn={");
			foreach (var x in Cells)
			{
				if (!x.Value.Coords.ContainsKey(""))
					continue;
				tree.Push(x.Value);
				var n = x.Value.Name;
				var wb = n.WithoutBars().ToLowerInvariant();
				s.Write("\"");
				s.Write(n.Escape());
				s.Write("\":{h:\"");
				s.Write(n.ToUpperInvariant().ToHtml().Escape());
				s.Write("\",a:\"c_");
				s.Write(n.ToHtmlId().Escape());
				s.Write("\",l:[");
				s.Write(x.Value.Coords[""][0][0].ToString(CultureInfo.InvariantCulture));
				s.Write(",");
				s.Write(x.Value.Coords[""][0][1].ToString(CultureInfo.InvariantCulture));
				s.Write(",");
				s.Write(x.Value.Coords[""][0][2].ToString(CultureInfo.InvariantCulture));
				s.Write(",");
				s.Write(x.Value.Coords[""][0][3].ToString(CultureInfo.InvariantCulture));
				s.WriteLine("]},");
				if (!c.ContainsKey(wb))
					c[wb] = new List<string>();
				c[wb].Add(n);
				foreach (var y in x.Value.Alias)
				{
					wb = y.WithoutBars().ToLowerInvariant();
					s.Write("\"");
					s.Write(y.Escape());
					s.Write("\":{p:\"");
					s.Write(n.Escape());
					s.WriteLine("\"},");
					if (!c.ContainsKey(wb))
						c[wb] = new List<string>();
					c[wb].Add(n);
				}
			}
			s.WriteLine("};");

			var w = new Dictionary<string, List<string>>();
			s.WriteLine("var wires_cn={");
			foreach (var x in Wires)
			{
				if (x.Value.Coords.Count == 0)
					continue;
				tree.Push(x.Value);
				var n = x.Value.Name;
				var wb = n.WithoutBars().ToLowerInvariant();
				s.Write("\"");
				s.Write(n.Escape());
				s.Write("\":{h:\"");
				s.Write(n.ToUpperInvariant().ToHtml().Escape());
				s.Write("\",a:\"w_");
				s.Write(n.ToHtmlId().Escape());
				s.Write("\",l:[");
				foreach (var y in x.Value.Coords)
				{
					s.Write("[");
					foreach (var z in y)
					{
						s.Write(z.ToString(CultureInfo.InvariantCulture));
						s.Write(",");
					}
					s.Write("],");
				}
				s.WriteLine("]},");
				if (!w.ContainsKey(wb))
					w[wb] = new List<string>();
				w[wb].Add(n);
			}
			s.WriteLine("};");

			s.WriteLine("var cells_grp={");
			foreach (var x in c)
			{
				s.Write("\"");
				s.Write(x.Key);
				s.Write("\":[");
				foreach (var y in x.Value)
				{
					s.Write("\"");
					s.Write(y.Escape());
					s.Write("\",");
				}
				s.WriteLine("],");
			}
			s.WriteLine("};");

			s.WriteLine("var wires_grp={");
			foreach (var x in w)
			{
				s.Write("\"");
				s.Write(x.Key);
				s.Write("\":[");
				foreach (var y in x.Value)
				{
					s.Write("\"");
					s.Write(y.Escape());
					s.Write("\",");
				}
				s.WriteLine("],");
			}
			s.WriteLine("};");

			s.Write("var qtree=");
			tree.ToJavaScript(s);
			s.WriteLine(";");
		}
	}
}

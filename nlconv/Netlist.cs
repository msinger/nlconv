using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Drawing;
using System.Numerics;

namespace nlconv
{
	public partial class Netlist : NetlistLexerBase
	{
		public readonly SortedDictionary<string, TypeDefinition>     Types;
		public readonly SortedDictionary<string, SignalDefinition>   Signals;
		public readonly SortedDictionary<string, CellDefinition>     Cells;
		public readonly SortedDictionary<string, WireDefinition>     Wires;
		public readonly Dictionary<WireConnection, WireDefinition>   Cons;
		public readonly List<LabelDefinition>                        Labels;
		public readonly SortedDictionary<string, CategoryDefinition> Categories;
		public readonly SortedDictionary<string, string>             Strings;

		public Netlist() : base()
		{
			Types      = new SortedDictionary<string, TypeDefinition>();
			Signals    = new SortedDictionary<string, SignalDefinition>();
			Cells      = new SortedDictionary<string, CellDefinition>();
			Wires      = new SortedDictionary<string, WireDefinition>();
			Cons       = new Dictionary<WireConnection, WireDefinition>();
			Labels     = new List<LabelDefinition>();
			Categories = new SortedDictionary<string, CategoryDefinition>();
			Strings    = new SortedDictionary<string, string>();
		}

		protected static void ParseEOT(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.EOT)
				throw new NetlistFormatException(n.Value.Pos, "Semicolon expected.");
		}

		protected static PortDirection ParsePortDirection(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Port direction expected.");
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
				throw new NetlistFormatException(n.Value.Pos, "Invalid port direction.");
			}
		}

		protected static PortDefinition ParsePortDefinition(ref LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Port name expected.");

			PortDirection d = PortDirection.Input;
			n = n.Next;
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				d = ParsePortDirection(n);
				n = n.Next;
			}

			return new PortDefinition(me.Value.Pos, me.Value.String.CanonicalizeBars(), d);
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
				throw new NetlistFormatException(n.Value.Pos, "Type definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Type name expected.");
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
			string doc  = "";

			if (Strings.ContainsKey("default-doc-url"))
				doc = Strings["default-doc-url"];

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

			TypeDefinition t = new TypeDefinition(me.Value.Pos, name.CanonicalizeBars(), color, desc, doc);

			foreach (var p in ports)
			{
				if (t.Ports.ContainsKey(p.Name))
					throw new NetlistFormatException(p.Pos, "Port name already in use.");
				t.Ports.Add(p.Name, p);
			}

			foreach (var kvp in coords)
				t.AddCoords(kvp.Key, kvp.Value);

			ParseEOT(n);
			return t;
		}

		protected static SignalDefinition ParseSignalDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "signal")
				throw new NetlistFormatException(n.Value.Pos, "Signal definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Signal name expected.");
			string name = n.Value.String;
			n = n.Next;

			string color = "";
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				color = ParseColor(n);
				n = n.Next;
			}

			string desc = "";
			while (n.Value.Type == LexerTokenType.String)
			{
				desc += n.Value.String;
				n = n.Next;
			}

			SignalDefinition s = new SignalDefinition(me.Value.Pos, name.CanonicalizeBars(), color, desc);

			ParseEOT(n);
			return s;
		}

		protected static string ParseColor(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Color expected.");
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
				throw new NetlistFormatException(n.Value.Pos, "Invalid color.");
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
					throw new NetlistFormatException(n.Value.Pos, "Invalid cell orientation.");
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
				throw new NetlistFormatException(me.Value.Pos, "Float number expected.");
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
			string name = "";
			if (named && n.Value.Type == LexerTokenType.Name)
			{
				name = n.Value.String.CanonicalizeBars();
				n = n.Next;
			}

			if (n.Value.Type != LexerTokenType.At)
				throw new NetlistFormatException(n.Value.Pos, "Coordinate list (@) expected.");
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
				throw new NetlistFormatException(n.Value.Pos, "Cell definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Cell name expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Colon)
				throw new NetlistFormatException(n.Value.Pos, "Colon expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Cell type expected.");
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
					throw new NetlistFormatException(n.Value.Pos, "Category name expected.");
				cat = n.Value.String;
				n = n.Next;
			}

			while (n.Value.Type == LexerTokenType.String)
			{
				desc += n.Value.String;
				n = n.Next;
			}

			CellDefinition c = new CellDefinition(me.Value.Pos, me.Next.Value.String.CanonicalizeBars(), t, o, f, sp, vr, cp, tr, desc, cat.CanonicalizeBars());

			foreach (var kvp in coords)
				c.AddCoords(kvp.Key, kvp.Value);

			ParseEOT(n);
			return c;
		}

		protected static WireConnection ParseWireConnection(ref LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Cell name expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Dot)
				throw new NetlistFormatException(n.Value.Pos, "Dot (.) expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Port name expected.");
			n = n.Next;

			return new WireConnection(me.Value.Pos, me.Value.String.CanonicalizeBars(), me.Next.Next.Value.String.CanonicalizeBars());
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

		protected WireDefinition ParseWireDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "wire")
				throw new NetlistFormatException(n.Value.Pos, "Wire definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Wire name expected.");
			n = n.Next;

			string sig = "";
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				if (n.Value.Type != LexerTokenType.Name)
					throw new NetlistFormatException(n.Value.Pos, "Wire signal class expected.");
				sig = n.Value.String.CanonicalizeBars();
				n = n.Next;
			}

			bool unchk = false;

			while (n.Value.Type == LexerTokenType.Name && n.Next.Value.Type != LexerTokenType.Dot)
			{
				if (n.Value.String.ToLowerInvariant() == "unchecked")
					unchk = true;
				else
					break;
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

			float wire_width = 1.0f;
			if (Strings.ContainsKey("js-wire-scale"))
			{
				float t;
				if (float.TryParse(Strings["js-wire-scale"],
				                   NumberStyles.AllowDecimalPoint,
				                   NumberFormatInfo.InvariantInfo,
				                   out t))
					wire_width = t;
			}

			WireDefinition w = new WireDefinition(me.Value.Pos, me.Next.Value.String.CanonicalizeBars(), sig, unchk, desc, wire_width);

			w.Sources.AddRange(sources);

			if (drains != null)
				w.Drains.AddRange(drains);

			foreach (var kvp in coords)
				w.Coords.Add(kvp.Value);

			ParseEOT(n);
			return w;
		}

		protected static AliasDefinition ParseAliasDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "alias")
				throw new NetlistFormatException(n.Value.Pos, "Alias definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name || (n.Value.String.ToLowerInvariant() != "cell" &&
			                                            n.Value.String.ToLowerInvariant() != "wire"))
				throw new NetlistFormatException(n.Value.Pos, "Cell or wire indicator expected.");
			AliasType t = n.Value.String.ToLowerInvariant() == "wire" ? AliasType.Wire : AliasType.Cell;
			n = n.Next;

			List<string> l = new List<string>();
			while (n.Value.Type == LexerTokenType.Name)
			{
				string s = n.Value.String.CanonicalizeBars();

				if (l.Contains(s))
					throw new NetlistFormatException(n.Value.Pos, "Duplicate alias found.");

				l.Add(s);
				n = n.Next;
			}

			if (l.Count == 0)
				throw new NetlistFormatException(n.Value.Pos, "At least one alias expected.");

			if (n.Value.Type != LexerTokenType.To)
				throw new NetlistFormatException(n.Value.Pos, "Arrow (->) expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Cell name expected.");
			n = n.Next;

			AliasDefinition a = new AliasDefinition(me.Value.Pos, n.Previous.Value.String.CanonicalizeBars(), t);
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
				throw new NetlistFormatException(n.Value.Pos, "Label definition expected.");
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
				throw new NetlistFormatException(n_sz.Value.Pos, "Size must be positive.");

			CellOrientation? o;
			bool? f;
			ParseCellOrientation(ref n, out o, out f);
			if (!o.HasValue) o = CellOrientation.Rot0;
			if (!f.HasValue) f = false;

			var n_c = n;
			var coords = ParseCoord(ref n);
			if (coords.Value.Count != 2)
				throw new NetlistFormatException(n_c.Value.Pos, "Coordinates must be one point (X,Y).");

			Alignment? a;
			ParseAlignment(ref n, out a);
			if (!a.HasValue) a = Alignment.Center;

			LabelDefinition l = new LabelDefinition(me.Value.Pos, text, color, size, o.Value, f.Value, new Vector(coords.Value[0], coords.Value[1]), a.Value);

			ParseEOT(n);
			return l;
		}

		protected static CategoryDefinition ParseCategoryDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "category")
				throw new NetlistFormatException(n.Value.Pos, "Category definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "Category name expected.");
			string name = n.Value.String;
			n = n.Next;

			string color = "";
			if (n.Value.Type == LexerTokenType.Colon)
			{
				n = n.Next;
				color = ParseColor(n);
				n = n.Next;
			}

			string desc = "";
			while (n.Value.Type == LexerTokenType.String)
			{
				desc += n.Value.String;
				n = n.Next;
			}

			CategoryDefinition c = new CategoryDefinition(me.Value.Pos, name.CanonicalizeBars(), color, desc);

			ParseEOT(n);
			return c;
		}

		protected static StringDefinition ParseStringDefinition(LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;

			if (n.Value.Type != LexerTokenType.Name || n.Value.String.ToLowerInvariant() != "define")
				throw new NetlistFormatException(n.Value.Pos, "String definition expected.");
			n = n.Next;

			if (n.Value.Type != LexerTokenType.Name)
				throw new NetlistFormatException(n.Value.Pos, "String name expected.");
			string name = n.Value.String;
			n = n.Next;

			string s = "";
			switch (n.Value.Type)
			{
			case LexerTokenType.String:
				while (n.Value.Type == LexerTokenType.String)
				{
					s += n.Value.String;
					n = n.Next;
				}
				break;
			case LexerTokenType.Value:
				s = n.Value.Value.ToString(NumberFormatInfo.InvariantInfo);
				n = n.Next;
				break;
			}

			StringDefinition d = new StringDefinition(me.Value.Pos, name, s);

			ParseEOT(n);
			return d;
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
				case "signal":
					return new ParserToken[] { ParseSignalDefinition(n) };
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
				case "define":
					return new ParserToken[] { ParseStringDefinition(n) };
				default:
					throw new NetlistFormatException(n.Value.Pos, "Invalid statement.");
				}
			}

			throw new NetlistFormatException(n.Value.Pos, "Unexpected token encountered by top level parser.");
		}

		private string file;
		private int    pos     = 0;
		private int    lineNum = 1;
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
			while (true)
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
				throw new NetlistFormatException(cell.Pos, "Type '" + cell.Type + "' not found.");

			if (!string.IsNullOrEmpty(cell.Category) && !Categories.ContainsKey(cell.Category))
				throw new NetlistFormatException(cell.Pos, "Category '" + cell.Category + "' not found.");

			TypeDefinition t = Types[cell.Type];

			foreach (var kvp in cell.Coords)
			{
				if (kvp.Key == "")
				{
					if (kvp.Value.Count != 1)
						throw new NetlistFormatException(cell.Pos, "Multiple cell coordinates.");
					if (kvp.Value[0].Count != 4)
						throw new NetlistFormatException(cell.Pos, "Cell coordinates don't describe a rectangle (need four numbers Y1,X1,Y2,X2).");
				}
				else
				{
					if (!t.Ports.ContainsKey(kvp.Key))
						throw new NetlistFormatException(cell.Pos, "Type '" + cell.Type + "' doesn't have a port named '" + kvp.Key + "'.");
					foreach (var l in kvp.Value)
					{
						if ((l.Count & 1) != 0)
							throw new NetlistFormatException(cell.Pos, "Cell port '" + kvp.Key + "' has odd number of coordinates.");
						if (l.Count == 0)
							throw new NetlistFormatException(cell.Pos, "Cell port '" + kvp.Key + "' has no coordinates.");
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
					throw new NetlistFormatException(wire.Pos, "Cell '" + c.Cell + "' not found.");

				CellDefinition cell = Cells[c.Cell];
				TypeDefinition t = Types[cell.Type];

				if (!t.Ports.ContainsKey(c.Port))
					throw new NetlistFormatException(wire.Pos, "Cell '" + c.Cell + "' (type '" + cell.Type + "') doesn't have a port named '" + c.Port + "'.");

				PortDefinition p = t.Ports[c.Port];

				// Multiple identical WireConnections in list?
				if (both.IndexOf(c) != both.LastIndexOf(c))
					throw new NetlistFormatException(wire.Pos, "Connection '" + c.ToString() + "' listed more than once.");

				if (p.Direction == PortDirection.NotConnected)
					throw new NetlistFormatException(wire.Pos, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') mustn't have a connection.");
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
					throw new NetlistFormatException(wire.Pos, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') in source list is not an output or tri-state.");

				if (p.Direction == PortDirection.Output && wire.Sources.Count != 1 && !wire.Unchecked && !AreAllParallelInverters(wire.Sources))
					throw new NetlistFormatException(wire.Pos, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') in source list is an output (not tri-state), but there are multiple entries in source list, which do not come from parallel inverters.");

				drvTri  |= p.Direction == PortDirection.Tristate || p.Direction == PortDirection.Bidir ? 1 : 0;
				drvOut0 |= p.Direction == PortDirection.OutputLow  ? 1 : 0;
				drvOut1 |= p.Direction == PortDirection.OutputHigh ? 1 : 0;

				if (drvTri + drvOut0 + drvOut1 > 1)
					throw new NetlistFormatException(wire.Pos, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') has incompatible/short-circuiting drivers in source list (combination of tri-state, bidir, output-low or output-high).");
			}

			foreach (var c in wire.Drains)
			{
				CellDefinition cell = Cells[c.Cell];
				TypeDefinition t = Types[cell.Type];
				PortDefinition p = t.Ports[c.Port];

				if (p.Direction != PortDirection.Input)
					throw new NetlistFormatException(wire.Pos, "Port '" + c.Port + "' of cell '" + c.Cell + "' (type '" + cell.Type + "') in drain list is not an input.");
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
					throw new NetlistFormatException(wire.Pos, "Wire segment has odd number of coordinates.");
				if (l.Count < 4)
					throw new NetlistFormatException(wire.Pos, "Wire segment has not enough coordinates (<4) to describe a line.");
			}
		}

		public void WriteLine(string line)
		{
			LinkedListNode<LexerToken> lex = Lex(line, file, lineNum, ref pos);
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
							throw new NetlistFormatException(t.Pos, "Type name already in use.");
						Types.Add(td.Name, td);
					}
					else if (t is SignalDefinition)
					{
						SignalDefinition sd = (SignalDefinition)t;
						if (Signals.ContainsKey(sd.Name))
							throw new NetlistFormatException(t.Pos, "Signal name already in use.");
						Signals.Add(sd.Name, sd);
					}
					else if (t is CellDefinition)
					{
						CellDefinition cd = (CellDefinition)t;
						if (Cells.ContainsKey(cd.Name))
							throw new NetlistFormatException(t.Pos, "Cell name already in use.");
						Cells.Add(cd.Name, cd);
					}
					else if (t is WireDefinition)
					{
						WireDefinition wd = (WireDefinition)t;
						if (Wires.ContainsKey(wd.Name))
							throw new NetlistFormatException(t.Pos, "Wire name already in use.");
						Wires.Add(wd.Name, wd);
					}
					else if (t is AliasDefinition)
					{
						AliasDefinition ad = (AliasDefinition)t;
						switch (ad.Type)
						{
						case AliasType.Cell:
							if (!Cells.ContainsKey(ad.Name))
								throw new NetlistFormatException(t.Pos, "No matching cell definition found prior to this alias definition.");
							Cells[ad.Name].Alias.AddRange(ad.Alias);
							break;
						case AliasType.Wire:
							if (!Wires.ContainsKey(ad.Name))
								throw new NetlistFormatException(t.Pos, "No matching wire definition found prior to this alias definition.");
							Wires[ad.Name].Alias.AddRange(ad.Alias);
							break;
						default:
							throw new NetlistFormatException(t.Pos, "Unknown alias definition type.");
						}
					}
					else if (t is LabelDefinition)
					{
						Labels.Add((LabelDefinition)t);
					}
					else if (t is CategoryDefinition)
					{
						CategoryDefinition cd = (CategoryDefinition)t;
						if (Categories.ContainsKey(cd.Name))
							throw new NetlistFormatException(t.Pos, "Category name already in use.");
						Categories.Add(cd.Name, cd);
					}
					else if (t is StringDefinition)
					{
						StringDefinition sd = (StringDefinition)t;
						Strings[sd.Name] = sd.String;
					}
					else
					{
						throw new NetlistFormatException(t.Pos, "Unknown parser token.");
					}
				}
			}
		}

		public void NextFile(string fn)
		{
			if (fifo.Count != 0)
			{
				LexerToken t = fifo.First.Value;
				throw new NetlistFormatException(t.Pos, "End of file expected.");
			}

			file    = fn;
			pos     = 0;
			lineNum = 1;
		}

		public void Flush()
		{
			NextFile(null);

			// Check for duplicate aliases
			List<string> names = new List<string>();
			foreach (var cell in Cells.Values)
				names.Add(cell.Name);
			foreach (var cell in Cells.Values)
			{
				foreach (var aname in cell.Alias)
				{
					if (names.Contains(aname))
						throw new NetlistFormatException(cell.Pos, "Alias " + aname + " of cell " + cell.Name + " is not unique.");
					names.Add(aname);
				}
			}
			names.Clear();
			foreach (var wire in Wires.Values)
				names.Add(wire.Name);
			foreach (var wire in Wires.Values)
			{
				foreach (var aname in wire.Alias)
				{
					if (names.Contains(aname))
						throw new NetlistFormatException(wire.Pos, "Alias " + aname + " of wire " + wire.Name + " is not unique.");
					names.Add(aname);
				}
			}

			foreach (var kvp in Cells)
				CheckCell(kvp.Value);
			foreach (var kvp in Wires)
				CheckWire(kvp.Value);

			// Check that there is no cell with a port that has more than one connection to a wire.
			foreach (var kvp in Wires)
			{
				List<WireConnection> both = new List<WireConnection>();
				both.AddRange(kvp.Value.Sources);
				foreach (var wc in kvp.Value.Drains)
					if (!kvp.Value.Sources.Contains(wc)) // Bidirectional ports exist on both sides, add only once here
						both.Add(wc);
				foreach (var wc in both)
				{
					if (Cons.ContainsKey(wc))
						throw new NetlistFormatException(wc.Pos, "Connection '" + wc.ToString() + "' already made to wire '" + Cons[wc].Name + "'.");
					Cons.Add(wc, kvp.Value);
				}
			}

			foreach (var kvp in Wires)
			{
				kvp.Value.Sources.Sort();
				kvp.Value.Drains.Sort();
			}

			// Warn on not connected input ports
			int wcount = 0;
			foreach (var kvp in Cells)
			{
				TypeDefinition t = Types[kvp.Value.Type];
				foreach (var p in t.Ports)
				{
					if (p.Value.Direction != PortDirection.Input && p.Value.Direction != PortDirection.Bidir)
						continue;
					WireConnection wc = new WireConnection(kvp.Value.Name, p.Value.Name);
					if (Cons.ContainsKey(wc))
						continue;
					wcount++;
					if (wcount <= 5)
						Console.Error.WriteLine("Warning: Input/bidir port not connected: " + wc.ToString());
				}
			}
			if (wcount > 5)
				Console.Error.WriteLine("Warning: " + (wcount - 5) + " more warnings like previous one not printed.");
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var x in Types)
				sb.AppendLine(x.Value.ToString());
			foreach (var x in Signals)
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
				x.Value.Draw(this, g, sx, sy);
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
			float wire_width = 1.0f;
			if (Strings.ContainsKey("js-wire-scale"))
			{
				float t;
				if (float.TryParse(Strings["js-wire-scale"],
				                   NumberStyles.AllowDecimalPoint,
				                   NumberFormatInfo.InvariantInfo,
				                   out t))
					wire_width = t;
			}

			var tree = new QuadTree(new Vector(-128.0f, 128.0f), 128.0f, 64, 6);

			s.Write("var wire_width=");
			s.Write(wire_width.ToString(NumberFormatInfo.InvariantInfo));
			s.WriteLine(";");
			s.WriteLine("var cell_types={");
			foreach (var x in Types)
			{
				var n = x.Value.Name;
				s.Write("\"");
				s.Write(n.Escape());
				s.Write("\":{h:\"");
				s.Write(n.ToUpperInvariant().ToHtml().Escape());
				s.Write("\",a:\"t_");
				s.Write(n.ToHtmlId().Escape());
				if (!string.IsNullOrEmpty(x.Value.Description))
				{
					s.Write("\",d:\"");
					s.Write(x.Value.Description.ToHtml().Escape());
				}
				if (!string.IsNullOrEmpty(x.Value.DocUrl))
				{
					s.Write("\",u:\"");
					s.Write(x.Value.TransformedDocUrl.Escape());
				}
				s.WriteLine("\"},");
			}
			s.WriteLine("};");

			s.WriteLine("var cell_cats={");
			foreach (var x in Categories)
			{
				var n = x.Value.Name;
				s.Write("\"");
				s.Write(n.Escape());
				s.Write("\":{h:\"");
				s.Write(n.ToUpperInvariant().ToHtml().Escape());
				s.Write("\",a:\"f_");
				s.Write(n.ToHtmlId().Escape());
				if (!string.IsNullOrEmpty(x.Value.Description))
				{
					s.Write("\",d:\"");
					s.Write(x.Value.Description.ToHtml().Escape());
				}
				s.WriteLine("\"},");
			}
			s.WriteLine("};");

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
				s.Write("\",t:\"");
				s.Write(x.Value.Type.Escape());
				if (!string.IsNullOrEmpty(x.Value.Category))
				{
					s.Write("\",f:\"");
					s.Write(x.Value.Category.Escape());
				}
				if (!string.IsNullOrEmpty(x.Value.Description))
				{
					s.Write("\",d:\"");
					s.Write(x.Value.Description.ToHtml().Escape());
				}
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
				if (!string.IsNullOrEmpty(x.Value.Description))
				{
					s.Write("\",d:\"");
					s.Write(x.Value.Description.ToHtml().Escape());
				}
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
				foreach (var y in x.Value.Alias)
				{
					wb = y.WithoutBars().ToLowerInvariant();
					s.Write("\"");
					s.Write(y.Escape());
					s.Write("\":{p:\"");
					s.Write(n.Escape());
					s.WriteLine("\"},");
					if (!w.ContainsKey(wb))
						w[wb] = new List<string>();
					w[wb].Add(n);
				}
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

		public class HdlWire
		{
			public string Name;
			public string HdlName;
			public bool NeedsKeeper;
			public bool MultipleDrivers;
			public bool IsVarType;
			public bool MatchesPortName;
			public List<WireConnection> Sources;
			public List<WireConnection> Drains;
			public Dictionary<string, HdlPort> Ports = new Dictionary<string, HdlPort>();
			public bool HasPorts { get { return Ports.Count != 0; } }
			public HdlVector Vector;
			public int BitIndex;
		}

		public class HdlVector
		{
			public string Name;
			public string HdlName;
			public int MinBitIndex;
			public int MaxBitIndex;
			public Dictionary<int, HdlWire> Wires = new Dictionary<int, HdlWire>();
			public string HdlPortWireName;
			public Dictionary<int, PortDirection> Dirs = new Dictionary<int, PortDirection>();
		}

		public class HdlPort
		{
			public string Name;
			public string HdlName;
			public string Dir;
			public HdlWire Wire;
			public HdlPortVector Vector;
			public int BitIndex;
		}

		public class HdlPortVector
		{
			public string Name;
			public string HdlName;
			public int MinBitIndex;
			public int MaxBitIndex;
			public Dictionary<int, HdlPort> Ports = new Dictionary<int, HdlPort>();
		}

		public virtual void ToSystemVerilog(TextWriter s)
		{
			string module_name = "top";
			if (Strings.ContainsKey("hdl-module"))
				module_name = Strings["hdl-module"];

			string cell_prefix = module_name + "_";
			if (Strings.ContainsKey("hdl-cell-prefix"))
				cell_prefix = Strings["hdl-cell-prefix"];

			string port_cell_name = "port";
			if (Strings.ContainsKey("hdl-port"))
				port_cell_name = Strings["hdl-port"];

			float lconv = 1.0f;
			if (Strings.ContainsKey("hdl-length-conv"))
			{
				float t;
				if (float.TryParse(Strings["hdl-length-conv"],
				                   NumberStyles.AllowDecimalPoint,
				                   NumberFormatInfo.InvariantInfo,
				                   out t))
					lconv = t;
			}

			CellDefinition port_cell = Cells[port_cell_name];

			Dictionary<string, HdlWire> wires = new Dictionary<string, HdlWire>();
			Dictionary<string, HdlPort> ports = new Dictionary<string, HdlPort>();
			Dictionary<string, HdlVector> vectors = new Dictionary<string, HdlVector>();
			Dictionary<string, HdlPortVector> pvectors = new Dictionary<string, HdlPortVector>();
			foreach (var wire in Wires.Values)
			{
				HdlWire w = new HdlWire();
				w.Name = wire.Name;
				w.HdlName = w.Name.ToSystemVerilog(SVNameProperties.Vector);
				w.Sources = wire.Sources;
				w.Drains = wire.Drains;

				if (w.Name.HasIndex(out w.BitIndex))
				{
					string basename = wire.Name.ToSystemVerilog(SVNameProperties.Basename);
					HdlVector vec = null;
					if (vectors.ContainsKey(basename))
					{
						vec = vectors[basename];
						if (vec.MinBitIndex > w.BitIndex)
							vec.MinBitIndex = w.BitIndex;
						if (vec.MaxBitIndex < w.BitIndex)
							vec.MaxBitIndex = w.BitIndex;
					}
					else
					{
						vec = new HdlVector();
						vec.HdlName = basename;
						vectors.Add(basename, vec);
						vec.MinBitIndex = w.BitIndex;
						vec.MaxBitIndex = w.BitIndex;
					}
					vec.Wires.Add(w.BitIndex, w);
					w.Vector = vec;
				}

				wires.Add(w.Name, w);
			}

			// Undo vectorization for vectors that are not 0 based. Can cause issues in Icarus.
			List<string> undoVectors = new List<string>();
			foreach (var vec in vectors.Values)
			{
				if (vec.MinBitIndex == 0)
					continue;
				foreach (var w in vec.Wires.Values)
				{
					w.HdlName = w.Name.ToSystemVerilog(SVNameProperties.Unvectorized);
					w.Vector = null;
					w.BitIndex = -1;
				}
				undoVectors.Add(vec.HdlName);
			}
			foreach (string vec in undoVectors)
				vectors.Remove(vec);

			foreach (var w in wires.Values)
			{
				w.NeedsKeeper = w.Sources.Count != 0;
				w.MultipleDrivers = w.Sources.Count > 1;
				w.MatchesPortName = false;

				foreach (WireConnection wc in w.Sources)
				{
					TypeDefinition t = Types[Cells[wc.Cell].Type];
					PortDirection d = t.Ports[wc.Port].Direction;
					switch (d)
					{
						case PortDirection.Output:     w.NeedsKeeper = false; break;
						case PortDirection.OutputLow:  w.NeedsKeeper = false; break;
						case PortDirection.OutputHigh: w.NeedsKeeper = false; break;
					}
					if (wc.Cell == port_cell.Name)
					{
						HdlPort p = new HdlPort();
						p.Name = wc.Port;
						p.HdlName = wc.Port.ToSystemVerilog(SVNameProperties.Vector);
						switch (d)
						{
							case PortDirection.Output:     p.Dir = "input"; break;
							case PortDirection.Tristate:   p.Dir = "input"; break;
							case PortDirection.Bidir:      p.Dir = "inout"; break;
							case PortDirection.OutputLow:  p.Dir = "input"; break;
							case PortDirection.OutputHigh: p.Dir = "input"; break;
							default: throw new ApplicationException("Invalid port direction for port " + wc.Port);
						}
						p.Wire = w;
						w.Ports.Add(wc.Port, p);
						ports.Add(wc.Port, p);
						if (p.HdlName == w.HdlName)
							w.MatchesPortName = true;
						if (p.Name.HasIndex(out p.BitIndex))
						{
							string basename = p.Name.ToSystemVerilog(SVNameProperties.Basename);
							HdlPortVector vec = null;
							if (pvectors.ContainsKey(basename))
							{
								vec = pvectors[basename];
								if (vec.MinBitIndex > p.BitIndex)
									vec.MinBitIndex = p.BitIndex;
								if (vec.MaxBitIndex < p.BitIndex)
									vec.MaxBitIndex = p.BitIndex;
							}
							else
							{
								vec = new HdlPortVector();
								vec.HdlName = basename;
								pvectors.Add(basename, vec);
								vec.MinBitIndex = p.BitIndex;
								vec.MaxBitIndex = p.BitIndex;
							}
							vec.Ports.Add(p.BitIndex, p);
							p.Vector = vec;
						}
					}
				}

				foreach (WireConnection wc in w.Drains)
				{
					TypeDefinition t = Types[Cells[wc.Cell].Type];
					PortDirection d = t.Ports[wc.Port].Direction;
					if (wc.Cell == port_cell.Name)
					{
						HdlPort p = new HdlPort();
						p.Name = wc.Port;
						p.HdlName = wc.Port.ToSystemVerilog(SVNameProperties.Vector);
						switch (d)
						{
							case PortDirection.Input: p.Dir = "output"; break;
							case PortDirection.Bidir: continue;
							default: throw new ApplicationException("Invalid port direction for port " + wc.Port);
						}
						p.Wire = w;
						w.Ports.Add(wc.Port, p);
						ports.Add(wc.Port, p);
						if (p.HdlName == w.HdlName)
							w.MatchesPortName = true;
						if (p.Name.HasIndex(out p.BitIndex))
						{
							string basename = p.Name.ToSystemVerilog(SVNameProperties.Basename);
							HdlPortVector vec = null;
							if (pvectors.ContainsKey(basename))
							{
								vec = pvectors[basename];
								if (vec.MinBitIndex > p.BitIndex)
									vec.MinBitIndex = p.BitIndex;
								if (vec.MaxBitIndex < p.BitIndex)
									vec.MaxBitIndex = p.BitIndex;
							}
							else
							{
								vec = new HdlPortVector();
								vec.HdlName = basename;
								pvectors.Add(basename, vec);
								vec.MinBitIndex = p.BitIndex;
								vec.MaxBitIndex = p.BitIndex;
							}
							vec.Ports.Add(p.BitIndex, p);
							p.Vector = vec;
						}
					}
				}

				w.IsVarType = !w.NeedsKeeper && !w.MultipleDrivers;

				if (w.NeedsKeeper)
				{
					foreach (var p in w.Ports.Values)
					{
						if (p.Dir == "inout")
						{
							Console.Error.WriteLine("Warning: Inout port " + p.HdlName + " needs a bus keeper in the higher level module.");
							w.NeedsKeeper = false;
						}
					}
				}
			}

			// Unify nettypes for vectors
			foreach (var vec in vectors.Values)
			{
				bool needsKeeper = false;
				bool isVarType   = true;
				foreach (var w in vec.Wires.Values)
				{
					if (w.NeedsKeeper)
						needsKeeper = true;
					if (!w.IsVarType)
						isVarType = false;
				}
				foreach (var w in vec.Wires.Values)
				{
					w.NeedsKeeper = needsKeeper;
					w.IsVarType   = isVarType;
				}
			}

			s.WriteLine("`default_nettype none");
			s.WriteLine();
			s.Write("module ");
			s.Write(module_name);
			s.Write(" #(");

			string sep = "";
			foreach (var p in ports.Values)
			{
				if (p.Dir == "input")
					continue;
				float sum = 0.0f;
				foreach (List<float> l in Wires[p.Wire.Name].Coords)
				{
					if (l.Count < 2)
						continue;
					float prev_y = l[0];
					float prev_x = l[1];
					for (int i = 2; i < l.Count; i += 2)
					{
						float y = l[i];
						float x = l[i+1];
						sum += Vector2.Distance(new Vector2(x, y), new Vector2(prev_x, prev_y)) * lconv;
						prev_y = y;
						prev_x = x;
					}
				}
				s.WriteLine(sep);
				s.Write("\t\tparameter real {0,-13} = {1}", p.Name.ToSystemVerilog(SVNameProperties.Unvectorized, "L_"),
				                                            sum.ToString(CultureInfo.InvariantCulture));
				sep = ",";
			}

			s.WriteLine();
			s.Write("\t) (");

			sep = "";
			foreach (var pv in pvectors.Values)
			{
				HdlPort p = pv.Ports[pv.MinBitIndex];
				s.WriteLine(sep);
				string w = "tri logic";
				if (p.Wire.IsVarType)
					w = "logic";
				s.Write("\t\t{0,-6} {1,9} [{4}:{3}] {2}", p.Dir, w, pv.HdlName, pv.MinBitIndex, pv.MaxBitIndex);
				sep = ",";
			}
			foreach (var p in ports.Values)
			{
				if (p.Vector != null)
					continue;
				s.WriteLine(sep);
				string w = "tri logic";
				if (p.Wire.IsVarType)
					w = "logic";
				s.Write("\t\t{0,-6} {1,9} {2}", p.Dir, w, p.HdlName);
				sep = ",";
			}

			s.WriteLine();
			s.WriteLine("\t);");
			s.WriteLine();

			// Emit wire (&vector) declarations for all wires except the ones that have the same name as their
			// respective ports, because ports are already declared.
			foreach (var vec in vectors.Values)
			{
				var w = vec.Wires[vec.MinBitIndex];
				if (w.MatchesPortName)
					continue;
				if (w.IsVarType)
					s.Write("\tlogic     [");
				else
					s.Write("\ttri logic [");
				s.Write(vec.MaxBitIndex.ToString(CultureInfo.InvariantCulture));
				s.Write(":");
				s.Write(vec.MinBitIndex.ToString(CultureInfo.InvariantCulture));
				s.Write("] ");
				s.Write(vec.HdlName);
				s.WriteLine(";");
			}
			foreach (var w in wires.Values)
			{
				if (w.Vector != null)
					continue;
				if (w.MatchesPortName)
					continue;
				if (w.IsVarType)
					s.Write("\tlogic     ");
				else
					s.Write("\ttri logic ");
				s.Write(w.HdlName);
				s.WriteLine(";");
			}

			s.WriteLine();

			// Assign wires to output ports.
			bool found = false;
			foreach (var w in wires.Values)
			{
				foreach (WireConnection wc in w.Drains)
				{
					if (wc.Cell == port_cell.Name)
					{
						var p = ports[wc.Port];
						if (p.HdlName == w.HdlName)
							continue;
						if (p.Dir == "inout")
							continue;
						s.WriteLine("\tassign {0,-16} = {1};", p.HdlName, w.HdlName);
						found = true;
					}
				}
			}
			if (found)
				s.WriteLine();

			// Assign input ports to wires.
			found = false;
			foreach (var w in wires.Values)
			{
				foreach (WireConnection wc in w.Sources)
				{
					if (wc.Cell == port_cell.Name)
					{
						var p = ports[wc.Port];
						if (p.HdlName == w.HdlName)
							continue;
						if (p.Dir == "inout")
							continue;
						s.WriteLine("\tassign {0} = {1};", w.HdlName, p.HdlName);
						found = true;
					}
				}
			}
			if (found)
				s.WriteLine();

			// Bridge bidirectional ports with wires.
			found = false;
			foreach (var w in wires.Values)
			{
				foreach (WireConnection wc in w.Sources)
				{
					if (wc.Cell == port_cell.Name)
					{
						var p = ports[wc.Port];
						if (p.HdlName == w.HdlName)
							continue;
						if (p.Dir != "inout")
							continue;
						s.WriteLine("\ttran ({0}, {1});", w.HdlName, p.HdlName);
						found = true;
					}
				}
			}
			if (found)
				s.WriteLine();

			foreach (var c in Cells.Values)
			{
				if (c.Name == port_cell.Name)
					continue;
				TypeDefinition t = Types[c.Type];
				StringBuilder cs = new StringBuilder();
				cs.Append("\t");
				cs.Append(cell_prefix);
				cs.Append(c.Type.ToSystemVerilog());
				cs.Append(" #(");
				sep = "";
				foreach (var p in t.Ports.Values)
				{
					if (p.Direction == PortDirection.Input || p.Direction == PortDirection.NotConnected)
						continue;
					WireConnection wc = new WireConnection(c.Name, p.Name);
					if (!Cons.ContainsKey(wc))
						continue;
					WireDefinition w = Cons[wc];
					if (w.Coords.Count == 0)
						continue;
					cs.AppendLine(sep);
					cs.Append("\t\t\t.");
					cs.Append(p.Name.ToSystemVerilog(SVNameProperties.Unvectorized, "L_"));
					cs.Append("(");
					float sum = 0.0f;
					foreach (List<float> l in w.Coords)
					{
						if (l.Count < 2)
							continue;
						float prev_y = l[0];
						float prev_x = l[1];
						for (int i = 2; i < l.Count; i += 2)
						{
							float y = l[i];
							float x = l[i+1];
							sum += Vector2.Distance(new Vector2(x, y), new Vector2(prev_x, prev_y)) * lconv;
							prev_y = y;
							prev_x = x;
						}
					}
					string par = null;
					HdlWire hw = wires[w.Name];
					if (hw.HasPorts)
					{
						HdlPort hp = null;
						foreach (var v in hw.Ports.Values)
						{
							if (v.Dir != "input")
							{
								if (hp != null)
								{
									Console.Error.WriteLine("Warning: Wire " + w.Name + " is connected to multiple output ports. Using only the wire length parameter (L_*) of first port.");
									break;
								}
								hp = v;
							}
						}
						if (hp != null)
							par = hp.Name.ToSystemVerilog(SVNameProperties.Unvectorized, "L_");
					}
					if (par != null)
						cs.Append(par);
					else
						cs.Append(sum.ToString(CultureInfo.InvariantCulture));
					cs.Append(")");
					sep = ",";
				}
				cs.AppendLine();
				cs.Append("\t\t) ");
				cs.Append(c.Name.ToSystemVerilog(SVNameProperties.Unvectorized, "", "_inst"));
				cs.Append(" (");
				Dictionary<string, HdlVector> vecs = new Dictionary<string, HdlVector>();
				sep = "";
				foreach (var p in t.Ports.Values)
				{
					WireConnection wc = new WireConnection(c.Name, p.Name);
					if (p.Name.HasIndex(out int idx))
					{
						string basename = p.Name.ToSystemVerilog(SVNameProperties.Basename);
						HdlVector vec = null;
						if (vecs.ContainsKey(basename))
						{
							vec = vecs[basename];
							if (vec.MinBitIndex > idx)
								vec.MinBitIndex = idx;
							if (vec.MaxBitIndex < idx)
								vec.MaxBitIndex = idx;
						}
						else
						{
							vec = new HdlVector();
							vec.Name = p.Name.Substring(0, p.Name.LastIndexOf('['));
							vec.HdlName = basename;
							vecs.Add(basename, vec);
							vec.MinBitIndex = idx;
							vec.MaxBitIndex = idx;
							vec.HdlPortWireName = null;
						}
						if (Cons.ContainsKey(wc))
							vec.Wires.Add(idx, wires[Cons[wc].Name]);
						vec.Dirs.Add(idx, p.Direction);
						continue;
					}
					cs.AppendLine(sep);
					cs.Append("\t\t\t.");
					cs.Append(p.Name.ToSystemVerilog());
					cs.Append("(");
					if (Cons.ContainsKey(wc))
						cs.Append(wires[Cons[wc].Name].HdlName);
					cs.Append(")");
					sep = ",";
				}
				foreach (HdlVector vec in vecs.Values)
				{
					HdlVector refvec = null;
					HdlWire firstwire = null;
					bool is1to1 = true;
					for (int i = vec.MinBitIndex; i <= vec.MaxBitIndex; i++)
					{
						HdlWire w = null;
						if (!vec.Wires.ContainsKey(i))
						{
							if (firstwire != null)
							{
								is1to1 = false;
								break;
							}
							continue;
						}
						w = vec.Wires[i];
						if (w.Vector == null)
						{
							is1to1 = false;
							break;
						}
						if (refvec == null)
							refvec = w.Vector;
						if (w.Vector != refvec || w.BitIndex != i)
						{
							is1to1 = false;
							break;
						}
						if (firstwire == null)
							firstwire = w;
					}
					string wname = "";
					if (is1to1 && firstwire != null)
						wname = firstwire.Vector.HdlName;
					if (!is1to1)
					{
						wname = c.Name.ToSystemVerilog(SVNameProperties.Unvectorized, "", "_port_" + vec.Name);
						vec.HdlPortWireName = wname;
					}
					cs.AppendLine(sep);
					cs.Append("\t\t\t.");
					cs.Append(vec.HdlName);
					cs.Append("(");
					cs.Append(wname);
					cs.Append(")");
					sep = ",";
				}
				cs.AppendLine();
				cs.AppendLine("\t\t);");
				cs.AppendLine();
				foreach (HdlVector vec in vecs.Values)
				{
					if (vec.HdlPortWireName == null)
						continue;
					s.Write("\ttri logic [");
					s.Write(vec.MaxBitIndex.ToString(CultureInfo.InvariantCulture));
					s.Write(":");
					s.Write(vec.MinBitIndex.ToString(CultureInfo.InvariantCulture));
					s.Write("] ");
					s.Write(vec.HdlPortWireName);
					s.WriteLine(";");
					foreach (var kvp in vec.Wires)
					{
						PortDirection d = vec.Dirs[kvp.Key];
						if (d == PortDirection.Input && kvp.Value.IsVarType)
						{
							s.WriteLine("\tassign {0}[{1}] = {2};",
							            vec.HdlPortWireName,
							            kvp.Key.ToString(CultureInfo.InvariantCulture),
							            kvp.Value.HdlName);
						}
						else if ((d == PortDirection.Output ||
						          d == PortDirection.OutputLow ||
						          d == PortDirection.OutputHigh) &&
						         kvp.Value.IsVarType)
						{
							s.WriteLine("\tassign {2} = {0}[{1}];",
							            vec.HdlPortWireName,
							            kvp.Key.ToString(CultureInfo.InvariantCulture),
							            kvp.Value.HdlName);
						}
						else
						{
							s.WriteLine("\ttran ({0}[{1}], {2});",
							            vec.HdlPortWireName,
							            kvp.Key.ToString(CultureInfo.InvariantCulture),
							            kvp.Value.HdlName);
						}
					}
				}
				s.Write(cs.ToString());
			}

			// Emit bus keepers for wires that need them.
			found = false;
			foreach (var vec in vectors.Values)
			{
				var w = vec.Wires[vec.MinBitIndex];
				if (!w.NeedsKeeper)
					continue;
				s.Write("\tkeeper #(.N(");
				int width = vec.MaxBitIndex - vec.MinBitIndex + 1;
				s.Write(width.ToString(CultureInfo.InvariantCulture));
				s.Write(")) ");
				s.Write(w.Name.ToSystemVerilog(SVNameProperties.Basename, "", "_keeper"));
				s.Write("(.n(");
				s.Write(vec.HdlName);
				s.WriteLine("));");
				found = true;
			}
			foreach (var w in wires.Values)
			{
				if (w.Vector != null)
					continue;
				if (!w.NeedsKeeper)
					continue;
				s.Write("\tkeeper ");
				s.Write(w.Name.ToSystemVerilog(SVNameProperties.Unvectorized, "", "_keeper"));
				s.Write("(.n(");
				s.Write(w.HdlName);
				s.WriteLine("));");
				found = true;
			}
			if (found)
				s.WriteLine();

			s.WriteLine("endmodule");
			s.WriteLine();
			s.WriteLine("`default_nettype wire");
		}
	}
}

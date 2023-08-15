using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;

namespace nlconv
{
	public class CellDefinition : ParserToken, IIntersectable
	{
		public readonly string           Name;
		public readonly string           Type;
		public readonly CellOrientation? Orientation;
		public readonly bool?            IsFlipped;
		public readonly bool             IsSpare, IsVirtual, IsComp, IsTrivial;
		public readonly string           Description;
		public readonly Dictionary<string, List<List<float>>> Coords;
		public readonly List<string>     Alias;
		public readonly string           Category;

		public CellDefinition(Position         pos,
		                      string           name,
		                      string           type,
		                      CellOrientation? orientation,
		                      bool?            flipped,
		                      bool             spare,
		                      bool             virt,
		                      bool             comp,
		                      bool             trivial,
		                      string           desc,
		                      string           category)
			: base(pos)
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
			Category    = category;
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
			if (!string.IsNullOrEmpty(Category))
			{
				sb.Append(" ->");
				sb.Append(Category);
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

				bool portLink = Coords.ContainsKey("") && Coords.ContainsKey(p.Name) && netlist.Strings.ContainsKey("map-url");
				s.Write("<span class=\"" + p.CssClass + "\">");
				if (portLink)
					s.Write("<a href=\"" + netlist.Strings["map-url"] + "&view=c:" + Name.ToUrl() + "&" + PortCoordString(Coords[p.Name]) + "\">");
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
				s.Write("<span class=\"" + w.GetCssClass(netlist) + "\"><a href=\"#w_" + w.Name.ToHtmlId() + "\">" + w.Name.ToHtmlName() + "</a></span>");

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
			if (!string.IsNullOrEmpty(Category))
				s.Write("<dt>Category</dt><dd>" + Category.ToHtmlName() + "</dd>");
			else
				s.Write("<dt>Category</dt><dd>-</dd>");
			s.Write("<dt>Orientation</dt><dd>" + OrientationString + "</dd>");
			if (Coords.ContainsKey("") && netlist.Strings.ContainsKey("map-url"))
				s.Write("<dt>Location</dt><dd><a href=\"" + netlist.Strings["map-url"] + "&view=c:" + Name.ToUrl() + "\">Highlight on map</a></dd>");
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
			return GetColorFromString(netlist.Types[Type].Color);
		}

		public Color? GetFloorColor(Netlist netlist)
		{
			if (!CanDrawFloor(netlist))
				return null;

			return GetColorFromString(netlist.Categories[Category].Color);
		}

		public virtual bool CanDraw
		{
			get { return !IsVirtual && Coords.ContainsKey("") && Orientation != null && IsFlipped != null; }
		}

		public virtual bool CanDrawFloor(Netlist netlist)
		{
			return !IsVirtual &&
			       Coords.ContainsKey("") &&
			       !string.IsNullOrEmpty(Category) &&
			       !string.IsNullOrEmpty(netlist.Categories[Category].Color);
		}

		public virtual RectangleF? GetBoundingBox(float sx, float sy)
		{
			if (!Coords.ContainsKey(""))
				return null;
			var p = Coords[""][0];
			float x1 = MathF.Min(p[0], p[2]);
			float y1 = MathF.Min(p[1], p[3]);
			float x2 = MathF.Max(p[0], p[2]);
			float y2 = MathF.Max(p[1], p[3]);
			float x = x1 * sx + 1.0f;
			float y = y1 * sy + 1.0f;
			float w = (x2 - x1) * sx - 2.0f;
			float h = (y2 - y1) * sy - 2.0f;
			return new RectangleF(x, y, w, h);
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

		public virtual Func<float, float, (float, float)> GetTransformation(Netlist netlist)
		{
			if (!CanDraw || !netlist.Types[Type].Center.HasValue)
				return null;

			var cmid = Center.Value;
			var tmid = netlist.Types[Type].Center.Value;

			Func<float, float, (float, float)> transform = null;

			switch (Orientation.Value)
			{
			case CellOrientation.Rot0:
				if (IsFlipped.Value)
					transform = (x, y) => ((x - tmid.X) + cmid.X, -(y - tmid.Y) + cmid.Y);
				else
					transform = (x, y) => ((x - tmid.X) + cmid.X, (y - tmid.Y) + cmid.Y);
				break;
			case CellOrientation.Rot90:
				if (IsFlipped.Value)
					transform = (x, y) => ((y - tmid.Y) + cmid.X, (x - tmid.X) + cmid.Y);
				else
					transform = (x, y) => (-(y - tmid.Y) + cmid.X, (x - tmid.X) + cmid.Y);
				break;
			case CellOrientation.Rot180:
				if (IsFlipped.Value)
					transform = (x, y) => (-(x - tmid.X) + cmid.X, (y - tmid.Y) + cmid.Y);
				else
					transform = (x, y) => (-(x - tmid.X) + cmid.X, -(y - tmid.Y) + cmid.Y);
				break;
			case CellOrientation.Rot270:
				if (IsFlipped.Value)
					transform = (x, y) => (-(y - tmid.Y) + cmid.X, -(x - tmid.X) + cmid.Y);
				else
					transform = (x, y) => ((y - tmid.Y) + cmid.X, -(x - tmid.X) + cmid.Y);
				break;
			}

			return transform;
		}

		protected static void DrawCross(Graphics g, Pen pen, float x, float y, float sx, float sy, float scale)
		{
			g.DrawLine(pen, x * sx - 5.0f * scale, y * sy - 5.0f * scale, x * sx + 5.0f * scale, y * sy + 5.0f * scale);
			g.DrawLine(pen, x * sx - 5.0f * scale, y * sy + 5.0f * scale, x * sx + 5.0f * scale, y * sy - 5.0f * scale);
		}

		public virtual void Draw(Netlist netlist, Graphics g, float sx, float sy)
		{
			if (!CanDraw)
				return;

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

			Pen   pen   = new Pen(GetColor(netlist), 3.0f * scale);
			Brush brush = new SolidBrush(Color.FromArgb(40, IsSpare ? Color.Black : Color.White));

			var box = GetBoundingBox(sx, sy).Value;

			g.FillRectangle(brush, box);
			g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);

			Func<float, float, (float, float)> identity  = (x, y) => (x, y);
			Func<float, float, (float, float)> transform = GetTransformation(netlist);

			foreach (var port in netlist.Types[Type].Ports)
			{
				var fix = identity;
				var c = Coords.ContainsKey(port.Key) ? Coords[port.Key] : null;
				if (c == null)
				{
					if (!netlist.Types[Type].Center.HasValue)
						continue;
					netlist.Types[Type].Coords.TryGetValue(port.Key, out c);
					fix = transform;
				}
				if (c == null)
					continue;

				Color col = port.Value.Color;
				Pen small = new Pen(Color.FromArgb(100, col), 3.0f * scale);
				//Pen big   = new Pen(Color.FromArgb(100, col), 5.0f * scale);

				foreach (var i in c)
				{
					if (i.Count == 2)
					{
						(float x, float y) pt = fix(i[0], i[1]);
						DrawCross(g, small, pt.x, pt.y, sx, sy, scale);
					}
					else
					{
						// Draw line:
						/*
						PointF[] pts = new PointF[i.Count / 2];
						for (int j = 0; j < i.Count / 2; j++)
						{
							(float x, float y) pt = fix(i[j * 2], i[j * 2 + 1]);
							pts[j] = new PointF(pt.x * sx, pt.y * sy);
						}
						g.DrawLines(big, pts);
						*/

						// Draw crosses:
						for (int j = 0; j < i.Count / 2; j++)
						{
							(float x, float y) pt = fix(i[j * 2], i[j * 2 + 1]);
							DrawCross(g, small, pt.x, pt.y, sx, sy, scale);
						}
					}
				}
			}
		}

		public virtual void DrawLabels(Graphics g, float sx, float sy)
		{
			if (!CanDraw)
				return;

			var box = GetBoundingBox(sx, sy).Value;

			float fsize = 15.0f;
			float min = MathF.Min(box.Width, box.Height);
			float max = MathF.Max(box.Width, box.Height);
			if (min > 250.0f)  fsize = 70.0f;
			if (min > 480.0f)  fsize = 150.0f;
			if (min > 2400.0f) fsize = 380.0f;

			using Font font = new Font(FontFamily.GenericMonospace, fsize);

			using Bitmap bmp = new Bitmap((int)max, (int)max, PixelFormat.Format32bppArgb);
			using Graphics gt = Graphics.FromImage(bmp);

			gt.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			gt.TranslateTransform(max / 2.0f, max / 2.0f);

			var tbox = new RectangleF(-(max / 2.0f), -(max / 2.0f), max, max);

			using var format = new StringFormat(StringFormatFlags.NoClip |
			                                    StringFormatFlags.NoWrap);
			format.Alignment     = StringAlignment.Center;
			format.LineAlignment = StringAlignment.Center;

			switch (Orientation.Value)
			{
				case CellOrientation.Rot0:
				case CellOrientation.Rot180:
					gt.RotateTransform(90.0f);
					break;
				case CellOrientation.Rot90:
					gt.RotateTransform(180.0f);
					break;
			}

			gt.DrawString(Name.ToUpperInvariant().Unbar(), font, Brushes.Black, tbox, format);
			gt.Flush(FlushIntention.Flush);

			g.DrawImage(bmp, box.X - max / 2.0f + box.Width / 2.0f, box.Y - max / 2.0f + box.Height / 2.0f);
			g.Flush(FlushIntention.Flush);
		}

		public virtual void DrawFloorplan(Netlist netlist, Graphics g, float sx, float sy)
		{
			Color? col = GetFloorColor(netlist);
			if (!col.HasValue)
				return;
			Brush brush = new SolidBrush(Color.FromArgb(128, col.Value));
			var box = GetBoundingBox(sx, sy).Value;
			g.FillRectangle(brush, box);
		}

		public virtual bool Intersects(Box b)
		{
			if (!Coords.ContainsKey(""))
				return false;
			var p = Coords[""][0];
			float x1 = MathF.Min(p[0], p[2]);
			float y1 = MathF.Min(p[1], p[3]);
			float x2 = MathF.Max(p[0], p[2]);
			float y2 = MathF.Max(p[1], p[3]);
			return b.Intersects(new Box(new Vector(x1, y1), new Vector(x2, y2)));
		}
	}
}

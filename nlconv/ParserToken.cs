using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;

namespace nlconv
{
	public abstract class ParserToken
	{
		public readonly Position Pos;
		public readonly int Line;
		public readonly int Col;

		protected ParserToken(Position pos)
		{
			Pos = pos;
		}

		protected static string CoordString(List<float> c, Func<float, float, (float, float)> fix)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < c.Count; i += 2)
			{
				if (i != 0)
					sb.Append(",");
				var (x, y) = fix(c[i], c[i+1]);
				sb.Append(x.ToString(CultureInfo.InvariantCulture));
				sb.Append(",");
				sb.Append(y.ToString(CultureInfo.InvariantCulture));
			}
			return sb.ToString();
		}

		protected static string CoordString(List<float> c)
		{
			return CoordString(c, (x, y) => (x, y));
		}

		protected static string LineCoordString(List<List<float>> l, int idx, Func<float, float, (float, float)> fix)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < l.Count; i++)
			{
				if (i != 0)
					sb.Append('&');
				sb.Append("line[");
				sb.Append((i + idx).ToString(CultureInfo.InvariantCulture));
				sb.Append("]=");
				sb.Append(CoordString(l[i], fix));
			}
			return sb.ToString();
		}

		protected static string LineCoordString(List<List<float>> l, Func<float, float, (float, float)> fix)
		{
			return LineCoordString(l, 0, fix);
		}

		protected static string LineCoordString(List<List<float>> l, int idx)
		{
			return LineCoordString(l, idx, (x, y) => (x, y));
		}

		protected static string LineCoordString(List<List<float>> l)
		{
			return LineCoordString(l, 0, (x, y) => (x, y));
		}

		protected static string BoxCoordString(List<float> l, int idx)
		{
			List<List<float>> ll = new List<List<float>>();
			ll.Add(new List<float>());
			ll[0].Add(l[0]);
			ll[0].Add(l[1]);
			ll[0].Add(l[0]);
			ll[0].Add(l[3]);
			ll[0].Add(l[2]);
			ll[0].Add(l[3]);
			ll[0].Add(l[2]);
			ll[0].Add(l[1]);
			ll[0].Add(l[0]);
			ll[0].Add(l[1]);
			return LineCoordString(ll, idx);
		}

		protected static string BoxCoordString(List<float> l)
		{
			return BoxCoordString(l, 0);
		}

		protected static string PortCoordString(List<List<float>> l, int lineIdx, int markIdx, Func<float, float, (float, float)> fix)
		{
			if (l.Count == 1 && l[0].Count == 2)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("mark[");
				sb.Append(markIdx.ToString(CultureInfo.InvariantCulture));
				sb.Append("]=");
				sb.Append(CoordString(l[0], fix));
				return sb.ToString();
			}

			return LineCoordString(l, lineIdx, fix);
		}

		protected static string PortCoordString(List<List<float>> l)
		{
			return PortCoordString(l, 0, 0, (x, y) => (x, y));
		}

		protected static string PortCoordString(List<List<float>> l, Func<float, float, (float, float)> fix)
		{
			return PortCoordString(l, 0, 0, fix);
		}

		public static Color GetColorFromString(string s)
		{
			switch (s)
			{
				case "red":       return Color.Red;
				case "lime":      return Color.Lime;
				case "blue":      return Color.Blue;
				case "pink":      return Color.Pink;
				case "navy":      return Color.Navy;
				case "yellow":    return Color.Yellow;
				case "cyan":      return Color.Cyan;
				case "magenta":   return Color.Magenta;
				case "orange":    return Color.Orange;
				case "purple":    return Color.Purple;
				case "teal":      return Color.Teal;
				case "green":     return Color.Green;
				case "brown":     return Color.Brown;
				case "gray":      return Color.Gray;
				case "black":     return Color.Black;
				case "white":     return Color.White;
			}
			return Color.Black;
		}
	}
}

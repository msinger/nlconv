using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	public abstract class ParserToken
	{
		public readonly int Pos;
		public readonly int Line;
		public readonly int Col;

		protected ParserToken(int pos, int line, int col)
		{
			Pos  = pos;
			Line = line;
			Col  = col;
		}

		protected ParserToken(int pos)
			: this (pos, 1, pos + 1)
		{ }

		protected ParserToken() : this (0) { }

		protected static string CoordString(List<float> c)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < c.Count; i++)
			{
				if (i != 0)
					sb.Append(",");
				sb.Append(c[i].ToString(CultureInfo.InvariantCulture));
			}
			return sb.ToString();
		}

		protected static string LineCoordString(List<List<float>> l, int idx)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < l.Count; i++)
			{
				if (i != 0)
					sb.Append('&');
				sb.Append("line[");
				sb.Append((i + idx).ToString(CultureInfo.InvariantCulture));
				sb.Append("]=");
				sb.Append(CoordString(l[i]));
			}
			return sb.ToString();
		}

		protected static string LineCoordString(List<List<float>> l)
		{
			return LineCoordString(l, 0);
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

		protected static string PortCoordString(List<List<float>> l, int lineIdx, int markIdx)
		{
			if (l.Count == 1 && l[0].Count == 2)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("mark[");
				sb.Append(markIdx.ToString(CultureInfo.InvariantCulture));
				sb.Append("]=");
				sb.Append(CoordString(l[0]));
				return sb.ToString();
			}

			return LineCoordString(l, lineIdx);
		}

		protected static string PortCoordString(List<List<float>> l)
		{
			return PortCoordString(l, 0, 0);
		}
	}
}

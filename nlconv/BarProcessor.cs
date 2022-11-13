using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	public static class BarProcessor
	{
		public static string ProcessBars(string n, string barOn, string barOff, string ws, Dictionary<char, string> map)
		{
			StringBuilder sb = new StringBuilder();
			int barMode = 0;
			string tilde = "~";
			if (map.ContainsKey('~'))
				tilde = map['~'];
			foreach (char c in n)
			{
				if (c == '~' && barMode == 0)
				{
					barMode = 1;
					continue;
				}
				else if (barMode == 1)
				{
					if (c == '~')
					{
						barMode = 0;
						sb.Append(tilde);
						continue;
					}
					sb.Append(barOn);
					if (c == '{')
					{
						barMode = 2;
						continue;
					}
					barMode = 3;
				}
				else if (barMode == 2)
				{
					if (c == '~')
					{
						barMode = 5;
						continue;
					}
					else if (c == '}')
					{
						barMode = 0;
						sb.Append(barOff);
						continue;
					}
				}
				else if (c == '~' && barMode == 3)
				{
					barMode = 4;
					continue;
				}
				else if (barMode == 4)
				{
					if (c == '~')
					{
						barMode = 3;
						sb.Append(tilde);
						continue;
					}
					sb.Append(barOff);
					barMode = 0;
				}
				else if (barMode == 5)
				{
					sb.Append(tilde);
					barMode = 2;
					if (c == '~')
					{
						continue;
					}
					else if (c == '}')
					{
						barMode = 0;
						sb.Append(barOff);
						continue;
					}
				}
				if (map.ContainsKey(c))
					sb.Append(map[c]);
				else if (ws != null && char.IsWhiteSpace(c))
					sb.Append(ws);
				else if (!char.IsControl(c) || (ws == null && char.IsWhiteSpace(c)))
					sb.Append(c);
			}
			if (barMode == 1 || barMode == 5)
				sb.Append(tilde);
			else if (barMode > 1)
				sb.Append(barOff);
			return sb.ToString();
		}
	}
}

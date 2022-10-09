using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	public static class StringHtmlExtension
	{
		private static readonly Dictionary<char, string> toHtmlMap                = new Dictionary<char, string>();
		private static readonly Dictionary<char, string> toHtmlNameMap            = new Dictionary<char, string>();
		private static readonly Dictionary<char, string> toHtmlNameWithoutBarsMap = new Dictionary<char, string>();

		static StringHtmlExtension()
		{
			toHtmlMap.Add('\n', "<br>");
			toHtmlMap.Add('&', "&amp;");
			toHtmlMap.Add('<', "&lt;");
			toHtmlMap.Add('>', "&gt;");

			toHtmlNameMap.Add('&', "&amp;");
			toHtmlNameMap.Add('<', "&lt;");
			toHtmlNameMap.Add('>', "&gt;");
		}

		public static string ToHtml(this string s)
		{
			return ProcessBars(s, "<span style=\"text-decoration:overline\">", "</span>", " ", toHtmlMap);
		}

		public static string ToHtmlName(this string n)
		{
			return ProcessBars(n, "<span style=\"text-decoration:overline\">", "</span>", "&nbsp;", toHtmlNameMap);
		}

		public static string ToNameWithoutBars(this string n)
		{
			return ProcessBars(n, "", "", " ", toHtmlNameWithoutBarsMap);
		}

		public static bool IsHtmlIdChar(this char c)
		{
			if (c >= 'a' && c <= 'z')
				return true;
			if (c >= 'Z' && c <= 'Z')
				return true;
			if (c >= '0' && c <= '9')
				return true;
			return c == '_';
		}

		public static string ToHtmlId(this string n)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in n)
			{
				if (c.IsHtmlIdChar())
				{
					sb.Append(c);
				}
				else
				{
					sb.Append('-');
					sb.Append(((int)c).ToString(CultureInfo.InvariantCulture));
					sb.Append('_');
				}
			}
			return sb.ToString();
		}

		private static string ProcessBars(string n, string barOn, string barOff, string ws, Dictionary<char, string> map)
		{
			StringBuilder sb = new StringBuilder();
			int barMode = 0;
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
						sb.Append('~');
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
				else if (c == '}' && barMode == 2)
				{
					barMode = 0;
					sb.Append(barOff);
					continue;
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
						sb.Append('~');
						continue;
					}
					sb.Append(barOff);
					barMode = 0;
				}
				if (map.ContainsKey(c))
					sb.Append(map[c]);
				else if (char.IsWhiteSpace(c))
					sb.Append(ws);
				else if (!char.IsControl(c))
					sb.Append(c);
			}
			if (barMode == 1)
				sb.Append('~');
			else if (barMode > 1)
				sb.Append(barOff);
			return sb.ToString();
		}
	}
}

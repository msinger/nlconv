using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	public static class StringHtmlExtension
	{
		private static readonly Dictionary<char, string> toHtmlMap     = new Dictionary<char, string>();
		private static readonly Dictionary<char, string> toHtmlNameMap = new Dictionary<char, string>();

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
			return BarProcessor.ProcessBars(s, "<span style=\"text-decoration:overline\">", "</span>", " ", toHtmlMap);
		}

		public static string ToHtmlName(this string n)
		{
			return BarProcessor.ProcessBars(n, "<span style=\"text-decoration:overline\">", "</span>", "&nbsp;", toHtmlNameMap);
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

		public static bool IsUrlChar(this char c)
		{
			if (c >= 'a' && c <= 'z')
				return true;
			if (c >= 'Z' && c <= 'Z')
				return true;
			if (c >= '0' && c <= '9')
				return true;
			return c == '_' || c == '-' || c == '.' || c == ',' || c == ':' || c == ';' || c == '~' ||
			       c == '{' || c == '}' || c == '[' || c == ']' || c == '(' || c == ')' ||
			       c == '+' || c == '*';
		}

		public static string ToHtmlId(this string n)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in n.CanonicalizeBars())
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

		public static string ToUrl(this string n)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in n)
			{
				if (c.IsUrlChar())
				{
					sb.Append(c);
				}
				else
				{
					sb.Append('%');
					sb.Append(((int)c).ToString("X2"));
				}
			}
			return sb.ToString();
		}
	}
}

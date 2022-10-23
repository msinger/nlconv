using System.Text;
using System.Globalization;

namespace nlconv
{
	public static class StringEscapeExtension
	{
		public static string Escape(this string n)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in n)
			{
				switch (c)
				{
					case '"':  sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					default:   sb.Append(c);      break;
				}
			}
			return sb.ToString();
		}
	}
}

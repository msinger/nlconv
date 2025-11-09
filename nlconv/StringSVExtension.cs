using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	public static class StringSVExtension
	{
		private static readonly Dictionary<char, string> map = new Dictionary<char, string>();

		static StringSVExtension()
		{
			map.Add('[', "");
			map.Add(']', "");
		}

		public static string ToSystemVerilog(this string s)
		{
			return BarProcessor.ProcessBars(s, "", "_n", "_", map);
		}
	}
}

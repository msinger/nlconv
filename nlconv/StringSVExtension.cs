using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	public static class StringSVExtension
	{
		private static readonly Dictionary<char, string> empty = new Dictionary<char, string>();

		public static string ToSystemVerilog(this string s, SVNameProperties p = SVNameProperties.Unvectorized)
		{
			bool hasIndex = s.HasIndex(out int index);
			if (p == SVNameProperties.Vector || !hasIndex)
				return BarProcessor.ProcessBars(s, "", "_n", "_", empty);
			string basename = s.Substring(0, s.LastIndexOf('['));
			string svname = BarProcessor.ProcessBars(basename, "", "_n", "_", empty);
			if (p == SVNameProperties.Basename)
				return svname;
			if (char.IsDigit(svname[svname.Length - 1]))
				svname += '_';
			return svname + index.ToString(CultureInfo.InvariantCulture);
		}

		public static bool HasIndex(this string s, out int index)
		{
			index = -1;
			int openIndex = s.LastIndexOf('[');
			if (openIndex >= 0 && s.EndsWith("]"))
			{
				string numberPart = s.Substring(openIndex + 1, s.Length - openIndex - 2);
				return int.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
			}
			return false;
		}
	}
}

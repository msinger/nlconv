using System.Collections.Generic;

namespace nlconv
{
	public static class StringUnbarExtension
	{
		private static readonly Dictionary<char, string> empty    = new Dictionary<char, string>();
		private static readonly Dictionary<char, string> tildeMap = new Dictionary<char, string>();

		static StringUnbarExtension()
		{
			tildeMap.Add('~', "~~");
		}

		public static string Unbar(this string n)
		{
			return BarProcessor.ProcessBars(n, "/", "", " ", empty);
		}

		public static string WithoutBars(this string n)
		{
			return BarProcessor.ProcessBars(n, "", "", " ", empty);
		}

		public static string CanonicalizeBars(this string n)
		{
			return BarProcessor.ProcessBars(n, "~{", "}", " ", tildeMap);
		}
	}
}

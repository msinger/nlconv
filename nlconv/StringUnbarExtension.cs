using System.Collections.Generic;

namespace nlconv
{
	public static class StringUnbarExtension
	{
		private static readonly Dictionary<char, string> empty = new Dictionary<char, string>();

		public static string Unbar(this string n)
		{
			return BarProcessor.ProcessBars(n, "/", "", " ", empty);
		}
	}
}

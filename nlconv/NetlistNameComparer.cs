using System.Collections.Generic;

namespace nlconv
{
	public class NetlistNameComparer : Comparer<string>
	{
		public static readonly new NetlistNameComparer Default = new NetlistNameComparer();

		public override int Compare(string x, string y)
		{
			return x.WithoutBars().CompareTo(y.WithoutBars());
		}
	}
}

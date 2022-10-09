using System;

namespace nlconv
{
	[Serializable]
	public enum WireClass
	{
		None = 0,
		Ground,
		Power,
		Decoded,
		Control,
		Clock,
		Data,
		Address,
		Reset,
		Analog,
	}
}

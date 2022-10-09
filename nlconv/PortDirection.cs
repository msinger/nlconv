using System;

namespace nlconv
{
	[Serializable]
	public enum PortDirection
	{
		Input = 0,
		Output,
		Tristate,
		Bidir,
		OutputLow,
		OutputHigh,
		NotConnected,
	}
}

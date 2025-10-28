using System.Drawing;

namespace nlconv
{
	public class PortDefinition : ParserToken
	{
		public readonly string        Name;
		public readonly PortDirection Direction;

		public PortDefinition(Position pos, string name, PortDirection dir) : base(pos)
		{
			Name      = name;
			Direction = dir;
		}

		public override string ToString()
		{
			string d = "<invalid>";
			switch (Direction)
			{
				case PortDirection.Input:        d = "in";            break;
				case PortDirection.Output:       d = "out";           break;
				case PortDirection.Tristate:     d = "tri";           break;
				case PortDirection.Bidir:        d = "inout";         break;
				case PortDirection.OutputLow:    d = "out0";          break;
				case PortDirection.OutputHigh:   d = "out1";          break;
				case PortDirection.NotConnected: d = "nc";            break;
			}
			return Name + ":" + d;
		}

		public string CssClass
		{
			get
			{
				switch (Direction)
				{
					case PortDirection.Input:        return "bg_lime";
					case PortDirection.Output:       return "bg_red";
					case PortDirection.Tristate:     return "bg_orange";
					case PortDirection.Bidir:        return "bg_yellow";
					case PortDirection.OutputLow:    return "bg_purple";
					case PortDirection.OutputHigh:   return "bg_magenta";
					case PortDirection.NotConnected: return "bg_blue";
				}
				return "bg_white";
			}
		}

		public Color Color
		{
			get
			{
				switch (Direction)
				{
					case PortDirection.Input:        return Color.Lime;
					case PortDirection.Output:       return Color.Red;
					case PortDirection.Tristate:     return Color.Orange;
					case PortDirection.Bidir:        return Color.Yellow;
					case PortDirection.OutputLow:    return Color.Purple;
					case PortDirection.OutputHigh:   return Color.Magenta;
					case PortDirection.NotConnected: return Color.Blue;
				}
				return Color.White;
			}
		}
	}
}

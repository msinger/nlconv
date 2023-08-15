using System;

namespace nlconv
{
	[Serializable]
	public class NetlistFormatException : FormatException
	{
		private readonly Position pos;

		public NetlistFormatException(Position pos, string message, Exception inner)
			: base(message, inner)
		{
			this.pos = pos;
		}

		public NetlistFormatException(Position pos, string message)
			: this(pos, message, null)
		{ }

		protected NetlistFormatException(System.Runtime.Serialization.SerializationInfo info,
		                                 System.Runtime.Serialization.StreamingContext  context)
			: base(info, context)
		{
			pos = (Position)info.GetValue("Position", typeof(Position));
		}

		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
		                                   System.Runtime.Serialization.StreamingContext  context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Position", pos, typeof(Position));
		}

		public int Position
		{
			get { return pos.Pos; }
		}

		public int Line
		{
			get { return pos.Line; }
		}

		public int Column
		{
			get { return pos.Col; }
		}

		public override string Message
		{
			get { return pos.ToString() + ": " + base.Message; }
		}
	}
}

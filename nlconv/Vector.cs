using System;

namespace nlconv
{
	public struct Vector
	{
		public float X, Y;

		public Vector(float x, float y)
		{
			X = x;
			Y = y;
		}

		public static int Order(Vector a, Vector b, Vector c)
		{
			// ABC is clockwise:         returns 1
			// ABC is counter-clockwise: returns -1
			return MathF.Sign((c.Y - a.Y) * (b.X - a.X) - (b.Y - a.Y) * (c.X - a.X));
		}
	}
}

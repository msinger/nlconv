using System;

namespace nlconv
{
	public struct Box
	{
		public Vector P0, P1;

		public Box(Vector p0, Vector p1)
		{
			P0 = p0;
			P1 = p1;
		}

		public bool Contains(Vector v)
		{
			float x0 = MathF.Min(P0.X, P1.X);
			float x1 = MathF.Max(P0.X, P1.X);
			float y0 = MathF.Min(P0.Y, P1.Y);
			float y1 = MathF.Max(P0.Y, P1.Y);
			return (v.X >= x0 && v.X <= x1 && v.Y >= y0 && v.Y <= y1);
		}

		public bool Intersects(Box o)
		{
			if (Contains(o.P0) || Contains(o.P1) || o.Contains(P0) || o.Contains(P1))
				return true;
			if (Contains(new Vector(o.P0.X, o.P1.Y)) || Contains(new Vector(o.P1.X, o.P0.Y)))
				return true;
			if (o.Contains(new Vector(P0.X, P1.Y)) || o.Contains(new Vector(P1.X, P0.Y)))
				return true;
			return false;
		}

		public bool SegmentIntersects(Line l)
		{
			if (Contains(l.P0))
				return true;
			if (l.SegmentsIntersect(new Line(P0, new Vector(P0.X, P1.Y))))
				return true;
			if (l.SegmentsIntersect(new Line(P1, new Vector(P1.X, P0.Y))))
				return true;
			if (l.SegmentsIntersect(new Line(P0, new Vector(P1.X, P0.Y))))
				return true;
			if (l.SegmentsIntersect(new Line(P1, new Vector(P0.X, P1.Y))))
				return true;
			return false;
		}

		public bool SegmentIntersects(Line l, float maxDist)
		{
			// maxDist * 2 == line width
			Vector p0 = new Vector(MathF.Min(P0.X, P1.X) - maxDist, MathF.Min(P0.Y, P1.Y) - maxDist);
			Vector p1 = new Vector(MathF.Max(P0.X, P1.X) + maxDist, MathF.Max(P0.Y, P1.Y) + maxDist);
			Box b = new Box(p0, p1);
			return b.SegmentIntersects(l);
		}
	}
}

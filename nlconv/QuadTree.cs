using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	public class QuadTree
	{
		public readonly Vector Center;
		public readonly float  HalfSize;
		public readonly int    LeafSize;
		public readonly int    MaxDepth;

		public readonly List<IIntersectable> Content;

		public QuadTree[] Sub;

		public QuadTree(Vector center, float halfSize, int leafSize, int maxDepth)
		{
			Center   = center;
			HalfSize = halfSize;
			LeafSize = leafSize;
			MaxDepth = maxDepth;
			Content  = new List<IIntersectable>();
		}

		public bool IsLeaf
		{
			get { return Sub == null; }
		}

		public Box Box
		{
			get
			{
				return new Box(new Vector(Center.X - HalfSize, Center.Y - HalfSize),
				               new Vector(Center.X + HalfSize, Center.Y + HalfSize));
			}
		}

		public bool Push(IIntersectable o)
		{
			if (!o.Intersects(Box))
				return false;
			Content.Add(o);
			if (!IsLeaf)
			{
				PushDown(o);
			}
			else if (Content.Count > LeafSize && MaxDepth > 0)
			{
				float hs = HalfSize / 2.0f;
				Sub = new QuadTree[] {
					new QuadTree(new Vector(Center.X + hs, Center.Y - hs), hs, LeafSize, MaxDepth - 1),
					new QuadTree(new Vector(Center.X + hs, Center.Y + hs), hs, LeafSize, MaxDepth - 1),
					new QuadTree(new Vector(Center.X - hs, Center.Y - hs), hs, LeafSize, MaxDepth - 1),
					new QuadTree(new Vector(Center.X - hs, Center.Y + hs), hs, LeafSize, MaxDepth - 1)
				};
				foreach (var i in Content)
					PushDown(i);
			}
			return true;
		}

		private void PushDown(IIntersectable o)
		{
			foreach (var s in Sub)
				s.Push(o);
		}

		public void ToJavaScript(TextWriter s)
		{
			s.Write("{p:[");
			s.Write((Center.X - HalfSize).ToString(CultureInfo.InvariantCulture));
			s.Write(",");
			s.Write((Center.Y - HalfSize).ToString(CultureInfo.InvariantCulture));
			s.Write(",");
			s.Write((Center.X + HalfSize).ToString(CultureInfo.InvariantCulture));
			s.Write(",");
			s.Write((Center.Y + HalfSize).ToString(CultureInfo.InvariantCulture));
			if (IsLeaf)
			{
				s.Write("],c:[");
				foreach (var x in Content)
				{
					var c = x as CellDefinition;
					if (c == null)
						continue;
					s.Write("\"");
					s.Write(c.Name.Escape());
					s.Write("\",");
				}
				s.Write("],w:[");
				foreach (var x in Content)
				{
					var w = x as WireDefinition;
					if (w == null)
						continue;
					s.Write("\"");
					s.Write(w.Name.Escape());
					s.Write("\",");
				}
				s.Write("]");
			}
			else
			{
				s.Write("],d:[");
				foreach (var x in Sub)
				{
					x.ToJavaScript(s);
					s.Write(",");
				}
				s.Write("]");
			}
			s.WriteLine("}");
		}
	}
}

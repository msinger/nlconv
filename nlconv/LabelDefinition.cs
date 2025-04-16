using System;
using System.Text;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace nlconv
{
	public class LabelDefinition : ParserToken
	{
		public readonly string          Text;
		public readonly string          Color;
		public readonly float           Size;
		public readonly CellOrientation Orientation;
		public readonly bool            IsFlipped;
		public readonly Vector          Coords;
		public readonly Alignment       Alignment;

		public LabelDefinition(Position        pos,
		                       string          text,
		                       string          color,
		                       float           size,
		                       CellOrientation orientation,
		                       bool            flipped,
		                       Vector          coords,
		                       Alignment       alignment)
			: base(pos)
		{
			Text        = text;
			Color       = color;
			Size        = size;
			Orientation = orientation;
			IsFlipped   = flipped;
			Coords      = coords;
			Alignment   = alignment;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("label \"");
			sb.Append(Text.Escape());
			sb.Append("\"");
			if (!string.IsNullOrEmpty(Color))
			{
				sb.Append(":");
				sb.Append(Color);
			}
			sb.Append(" ");
			sb.Append(Size.ToString(CultureInfo.InvariantCulture));
			switch (Orientation)
			{
				case CellOrientation.Rot0:   sb.Append(" rot0");      break;
				case CellOrientation.Rot90:  sb.Append(" rot90");     break;
				case CellOrientation.Rot180: sb.Append(" rot180");    break;
				case CellOrientation.Rot270: sb.Append(" rot270");    break;
				default:                     sb.Append(" <invalid>"); break;
			}
			if (IsFlipped)
				sb.Append(", flip");
			sb.Append(" @");
			sb.Append(Coords.X.ToString(CultureInfo.InvariantCulture));
			sb.Append(",");
			sb.Append(Coords.Y.ToString(CultureInfo.InvariantCulture));
			switch (Alignment)
			{
				case Alignment.Center:       sb.Append(" center;");        break;
				case Alignment.TopLeft:      sb.Append(" top-left;");      break;
				case Alignment.TopCenter:    sb.Append(" top-center;");    break;
				case Alignment.TopRight:     sb.Append(" top-right;");     break;
				case Alignment.CenterLeft:   sb.Append(" center-left;");   break;
				case Alignment.CenterRight:  sb.Append(" center-right;");  break;
				case Alignment.BottomLeft:   sb.Append(" bottom-left;");   break;
				case Alignment.BottomCenter: sb.Append(" bottom-center;"); break;
				case Alignment.BottomRight:  sb.Append(" bottom-right;");  break;
				default:                     sb.Append(" <invalid>;");     break;
			}
			return sb.ToString();
		}

		public Color GetColor()
		{
			return GetColorFromString(Color);
		}

		public virtual void Draw(Graphics g, float sx, float sy)
		{
			string txt  = Text.Unbar(true);
			Color col   = GetColor();
			Brush brush = new SolidBrush(col);
			float sz    = Size * sx;
			Font font   = new Font(FontFamily.GenericMonospace, sz);

			SizeF sf = g.MeasureString(txt, font);

			int w = (int)(((int)MathF.Max(sf.Width, sf.Height) + 1) & ~1u);
			float hw = (float)w / 2.0f;
			Bitmap bmp = new Bitmap(w, w, PixelFormat.Format32bppArgb);
			Graphics gt = Graphics.FromImage(bmp);

			gt.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			gt.TranslateTransform(hw, hw);

			var tbox = new RectangleF(-hw, -hw, (float)w, (float)w);

			var format = new StringFormat(StringFormatFlags.NoClip |
			                              StringFormatFlags.NoWrap);

			switch (Alignment)
			{
			case Alignment.Center:
				format.Alignment     = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;
				break;
			case Alignment.TopLeft:
				format.Alignment     = StringAlignment.Near;
				format.LineAlignment = StringAlignment.Near;
				break;
			case Alignment.TopCenter:
				format.Alignment     = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Near;
				break;
			case Alignment.TopRight:
				format.Alignment     = StringAlignment.Far;
				format.LineAlignment = StringAlignment.Near;
				break;
			case Alignment.CenterLeft:
				format.Alignment     = StringAlignment.Near;
				format.LineAlignment = StringAlignment.Center;
				break;
			case Alignment.CenterRight:
				format.Alignment     = StringAlignment.Far;
				format.LineAlignment = StringAlignment.Center;
				break;
			case Alignment.BottomLeft:
				format.Alignment     = StringAlignment.Near;
				format.LineAlignment = StringAlignment.Far;
				break;
			case Alignment.BottomCenter:
				format.Alignment     = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Far;
				break;
			case Alignment.BottomRight:
				format.Alignment     = StringAlignment.Far;
				format.LineAlignment = StringAlignment.Far;
				break;
			}

			switch (Orientation)
			{
			case CellOrientation.Rot0:
				gt.RotateTransform(90.0f);
				break;
			case CellOrientation.Rot90:
				gt.RotateTransform(180.0f);
				break;
			case CellOrientation.Rot180:
				gt.RotateTransform(270.0f);
				break;
			}

			if (IsFlipped)
				gt.ScaleTransform(-1.0f, 1.0f);

			gt.DrawString(txt, font, brush, tbox, format);
			gt.Flush(FlushIntention.Flush);

			float fx = 0.0f;
			float fy = 0.0f;

			switch (Alignment)
			{
			case Alignment.TopLeft:
				fx = -hw;
				fy = hw;
				break;
			case Alignment.TopCenter:
				fx = -hw;
				break;
			case Alignment.TopRight:
				fx = -hw;
				fy = -hw;
				break;
			case Alignment.CenterLeft:
				fy = hw;
				break;
			case Alignment.CenterRight:
				fy = -hw;
				break;
			case Alignment.BottomLeft:
				fx = hw;
				fy = hw;
				break;
			case Alignment.BottomCenter:
				fx = hw;
				break;
			case Alignment.BottomRight:
				fx = hw;
				fy = -hw;
				break;
			}

			if (IsFlipped)
				fy = -fy;

			float tmp;
			switch (Orientation)
			{
			case CellOrientation.Rot90:
				tmp = fx;
				fx = -fy;
				fy = tmp;
				break;
			case CellOrientation.Rot180:
				fx = -fx;
				fy = -fy;
				break;
			case CellOrientation.Rot270:
				tmp = fx;
				fx = fy;
				fy = -tmp;
				break;
			}

			g.DrawImage(bmp, Coords.X * sx - hw + fx, Coords.Y * sy - hw + fy);
			g.Flush(FlushIntention.Flush);

			format.Dispose();
			gt.Dispose();
			bmp.Dispose();
			font.Dispose();
			brush.Dispose();
		}
	}
}

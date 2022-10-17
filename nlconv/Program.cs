using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Drawing.Imaging;

namespace nlconv
{
	class MainClass
	{
		public static int Main(string[] args)
		{
			string option = args.Length == 1 ? args[0] : null;
			switch (option)
			{
			case null:
			case "--html":
			case "--png-cells":
			case "--png-wires":
			case "--png-labels":
			case "--png":
			case "--js":
				break;
			default:
				Console.Error.WriteLine("Usage: nlconv.exe [<OPTIONS>]");
				Console.Error.WriteLine();
				Console.Error.WriteLine("OPTIONS:");
				Console.Error.WriteLine("  --html        Convert netlist from STDIN to HTML on STDOUT.");
				Console.Error.WriteLine("  --png-cells   Convert netlist from STDIN to PNG containing all cells on STDOUT.");
				Console.Error.WriteLine("  --png-wires   Convert netlist from STDIN to PNG containing all wires on STDOUT.");
				Console.Error.WriteLine("  --png-labels  Convert netlist from STDIN to PNG containing all labels on STDOUT.");
				Console.Error.WriteLine("  --png         Convert netlist from STDIN to PNG containing everything on STDOUT.");
				Console.Error.WriteLine("  --js          Convert netlist from STDIN to Java Script containing all coordinates on STDOUT.");
				Console.Error.WriteLine();
				Console.Error.WriteLine("Without option, nlconv.exe just reads netlist from STDIN");
				Console.Error.WriteLine("and checks if there are no errors.");
				return option == "--help" ? 0 : 1;
			}

			Netlist nl = new Netlist();
			nl.DefaultDocUrl = "http://iceboy.a-singer.de/doc/dmg_cells.html#%t";
			nl.MapUrl        = "http://iceboy.a-singer.de/dmg_cpu_b_map/?wires=0&cells=0";

			string l;
			while ((l = Console.ReadLine()) != null)
				nl.WriteLine(l);
			nl.Flush();

			switch (option)
			{
			default:
				Console.Error.WriteLine("Netlist parsed successfully.");
				return 0;
			case "--html":
				return GenHtml(nl);
			case "--png-cells":
				return GenCellsPng(nl);
			case "--png-wires":
				return GenWiresPng(nl);
			case "--png-labels":
				return GenLabelsPng(nl);
			case "--png":
				return GenAllPng(nl);
			case "--js":
				return GenJavaScript(nl);
			}
		}

		private static int GenHtml(Netlist nl)
		{
			const string style =
				"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
				"<style>" +
				"img { max-width: 100%; display: inline; } " +
				"table.default {" +
				  "border-top: 1px solid #b2b2b2;" +
				  "border-left: 1px solid #b2b2b2;" +
				  "border-bottom: 1px solid #4c4c4c;" +
				  "border-right: 1px solid #4c4c4c;" +
				"} " +
				  "table.default tr th {" +
				  "border-top: 1px solid #4c4c4c;" +
				  "border-left: 1px solid #4c4c4c;" +
				  "border-bottom: 1px solid #b2b2b2;" +
				  "border-right: 1px solid #b2b2b2;" +
				"} " +
				"table.default tr td {" +
				  "border-top: 1px solid #4c4c4c;" +
				  "border-left: 1px solid #4c4c4c;" +
				  "border-bottom: 1px solid #b2b2b2;" +
				  "border-right: 1px solid #b2b2b2;" +
				"}" +
				".bg_red { background-color: rgba(255, 0, 0, 0.3); }" +
				".bg_lime { background-color: rgba(0, 255, 0, 0.3); }" +
				".bg_blue { background-color: rgba(0, 0, 255, 0.3); }" +
				".bg_yellow { background-color: rgba(255, 255, 0, 0.3); }" +
				".bg_cyan { background-color: rgba(0, 255, 255, 0.3); }" +
				".bg_magenta { background-color: rgba(255, 0, 255, 0.3); }" +
				".bg_orange { background-color: rgba(255, 127, 0, 0.3); }" +
				".bg_purple { background-color: rgba(127, 0, 127, 0.3); }" +
				".bg_turquoise { background-color: rgba(0, 127, 127, 0.3); }" +
				".bg_green { background-color: rgba(0, 127, 0, 0.3); }" +
				".bg_black { background-color: rgba(0, 0, 0, 0.3); }" +
				".bg_white { background-color: rgba(255, 255, 255, 0.3); }" +
				"</style>";

			const string footer =
				"<p><a rel=\"license\" " +
				      "href=\"http://creativecommons.org/licenses/by-sa/4.0/\">" +
				"<img alt=\"Creative Commons License\" style=\"border-width:0\" " +
				     "src=\"https://i.creativecommons.org/l/by-sa/4.0/88x31.png\">" +
				"</a><br>This work is licensed under a " +
				"<a rel=\"license\" " +
				   "href=\"http://creativecommons.org/licenses/by-sa/4.0/\">" +
				"Creative Commons Attribution-ShareAlike 4.0 International License</a>.</p>";

			Console.WriteLine("<!DOCTYPE html>");
			Console.Write("<html lang=\"en\"><head><meta charset=\"UTF-8\"><title>Netlist</title>");
			Console.Write(style);
			Console.Write("</head><body><nav><p>");
			Console.Write("<a href=\"http://iceboy.a-singer.de/\">Home</a>");
			Console.Write("</p><hr></nav><main><h1>Netlist</h1>");
			nl.ToHtml(Console.Out);
			Console.Write("</main><footer><hr>" + footer);
			Console.Write("</footer></body></html>");

			return 0;
		}

		private static int GenPng(Netlist nl, Action<Graphics, float, float> draw)
		{
			const int width  = 16384;
			const int height = 16384;

			// Scaling needed from Leaflet 256x256 coordinates to image size
			const float sx = (float)height / 256.0f;
			const float sy = (float)width / 256.0f;

			Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

				// Rotate to Leaflet map coordinate system
				g.RotateTransform(-90.0f);

				draw(g, sx, sy);
			}
			bmp.Save(Console.OpenStandardOutput(), ImageFormat.Png);
			return 0;
		}

		private static int GenCellsPng(Netlist nl)
		{
			return GenPng(nl, (g, sx, sy) => { nl.DrawCells(g, sx, sy); });
		}

		private static int GenWiresPng(Netlist nl)
		{
			return GenPng(nl, (g, sx, sy) => { nl.DrawWires(g, sx, sy); });
		}

		private static int GenLabelsPng(Netlist nl)
		{
			return GenPng(nl, (g, sx, sy) => { nl.DrawLabels(g, sx, sy); });
		}

		private static int GenAllPng(Netlist nl)
		{
			return GenPng(nl, (g, sx, sy) => {
				nl.DrawCells(g, sx, sy);
				nl.DrawWires(g, sx, sy);
				nl.DrawLabels(g, sx, sy);
			});
		}

		private static int GenJavaScript(Netlist nl)
		{
			nl.ToJavaScript(Console.Out);
			return 0;
		}
	}
}

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
			bool   parseOptions = true;
			bool   genHtml      = false;
			bool   genPngCells  = false;
			bool   genPngWires  = false;
			bool   genPngLabels = false;
			bool   genPngFloor  = false;
			bool   genPng       = false;
			bool   genJS        = false;
			string outHtml      = null;
			string outPngCells  = null;
			string outPngWires  = null;
			string outPngLabels = null;
			string outPngFloor  = null;
			string outPng       = null;
			string outJS        = null;

			for (int i = 0; i < args.Length; i++)
			{
				if (parseOptions && args[i].StartsWith("--"))
				{
					string nextArg = (i + 1 < args.Length) ? args[i + 1] : null;
					switch (args[i])
					{
						case "--html":       genHtml      = true; outHtml      = nextArg; i++; break;
						case "--png-cells":  genPngCells  = true; outPngCells  = nextArg; i++; break;
						case "--png-wires":  genPngWires  = true; outPngWires  = nextArg; i++; break;
						case "--png-labels": genPngLabels = true; outPngLabels = nextArg; i++; break;
						case "--png-floor":  genPngFloor  = true; outPngFloor  = nextArg; i++; break;
						case "--png":        genPng       = true; outPng       = nextArg; i++; break;
						case "--js":         genJS        = true; outJS        = nextArg; i++; break;
						case "--":           parseOptions = false;                             break;
						default:             PrintHelp(); return args[i] == "--help" ? 0 : 1;
					}
					continue;
				}

				PrintHelp();
				return 1;
			}

			Netlist nl = new Netlist();
			nl.DefaultDocUrl = "/doc/dmg_cells.html#%t";
			nl.MapUrl        = "/dmg_cpu_b_map/?wires=0";

			string l;
			while ((l = Console.ReadLine()) != null)
				nl.WriteLine(l);
			nl.Flush();

			if (!genHtml && !genPngCells && !genPngWires && !genPngLabels && !genPngFloor && !genPng && !genJS)
				Console.Error.WriteLine("Netlist parsed successfully.");

			if (genHtml)
			{
				TextWriter s = Console.Out;
				if (!string.IsNullOrEmpty(outHtml) && outHtml != "-")
					s = File.CreateText(outHtml);
				GenHtml(s, nl);
			}

			if (genPngCells)
			{
				Stream s = Console.OpenStandardOutput();
				if (!string.IsNullOrEmpty(outPngCells) && outPngCells != "-")
					s = File.Create(outPngCells);
				GenCellsPng(s, nl);
			}

			if (genPngWires)
			{
				Stream s = Console.OpenStandardOutput();
				if (!string.IsNullOrEmpty(outPngWires) && outPngWires != "-")
					s = File.Create(outPngWires);
				GenWiresPng(s, nl);
			}

			if (genPngLabels)
			{
				Stream s = Console.OpenStandardOutput();
				if (!string.IsNullOrEmpty(outPngLabels) && outPngLabels != "-")
					s = File.Create(outPngLabels);
				GenLabelsPng(s, nl);
			}

			if (genPngFloor)
			{
				Stream s = Console.OpenStandardOutput();
				if (!string.IsNullOrEmpty(outPngFloor) && outPngFloor != "-")
					s = File.Create(outPngFloor);
				GenFloorplanPng(s, nl);
			}

			if (genPng)
			{
				Stream s = Console.OpenStandardOutput();
				if (!string.IsNullOrEmpty(outPng) && outPng != "-")
					s = File.Create(outPng);
				GenAllPng(s, nl);
			}

			if (genJS)
			{
				TextWriter s = Console.Out;
				if (!string.IsNullOrEmpty(outJS) && outJS != "-")
					s = File.CreateText(outJS);
				nl.ToJavaScript(s);
			}

			return 0;
		}

		private static void PrintHelp()
		{
			Console.Error.WriteLine("Usage: nlconv.exe [<OPTIONS>] [<FILES>]");
			Console.Error.WriteLine();
			Console.Error.WriteLine("OPTIONS:");
			Console.Error.WriteLine("  --html <FILE>        Convert netlist to HTML.");
			Console.Error.WriteLine("  --png-cells <FILE>   Convert netlist to PNG containing all cells.");
			Console.Error.WriteLine("  --png-wires <FILE>   Convert netlist to PNG containing all wires.");
			Console.Error.WriteLine("  --png-labels <FILE>  Convert netlist to PNG containing all labels.");
			Console.Error.WriteLine("  --png-floor <FILE>   Convert netlist to PNG containing the floorplan of all cells.");
			Console.Error.WriteLine("  --png <FILE>         Convert netlist to PNG containing everything.");
			Console.Error.WriteLine("  --js <FILE>          Convert netlist to Java Script containing all coordinates.");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Without any options, nlconv.exe just reads netlist");
			Console.Error.WriteLine("and checks if there are no errors.");
		}

		private static void GenHtml(TextWriter s, Netlist nl)
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
				".bg_pink { background-color: rgba(255, 192, 203, 0.3); }" +
				".bg_navy { background-color: rgba(0, 0, 128, 0.3); }" +
				".bg_yellow { background-color: rgba(255, 255, 0, 0.3); }" +
				".bg_cyan { background-color: rgba(0, 255, 255, 0.3); }" +
				".bg_magenta { background-color: rgba(255, 0, 255, 0.3); }" +
				".bg_orange { background-color: rgba(255, 165, 0, 0.3); }" +
				".bg_purple { background-color: rgba(128, 0, 128, 0.3); }" +
				".bg_teal { background-color: rgba(0, 128, 128, 0.3); }" +
				".bg_green { background-color: rgba(0, 128, 0, 0.3); }" +
				".bg_brown { background-color: rgba(165, 42, 42, 0.3); }" +
				".bg_gray { background-color: rgba(128, 128, 128, 0.3); }" +
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

			s.WriteLine("<!DOCTYPE html>");
			s.Write("<html lang=\"en\"><head><meta charset=\"UTF-8\"><title>Netlist</title>");
			s.Write(style);
			s.Write("</head><body><nav><p>");
			s.Write("<a href=\"http://iceboy.a-singer.de/\">Home</a>");
			s.Write("</p><hr></nav><main><h1>Netlist</h1>");
			nl.ToHtml(s);
			s.Write("</main><footer><hr>" + footer);
			s.Write("</footer></body></html>");
		}

		private static void GenPng(Stream s, Netlist nl, Action<Graphics, float, float> draw)
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
			bmp.Save(s, ImageFormat.Png);
		}

		private static void GenCellsPng(Stream s, Netlist nl)
		{
			GenPng(s, nl, (g, sx, sy) => { nl.DrawCells(g, sx, sy); });
		}

		private static void GenWiresPng(Stream s, Netlist nl)
		{
			GenPng(s, nl, (g, sx, sy) => { nl.DrawWires(g, sx, sy); });
		}

		private static void GenLabelsPng(Stream s, Netlist nl)
		{
			GenPng(s, nl, (g, sx, sy) => { nl.DrawLabels(g, sx, sy); });
		}

		private static void GenFloorplanPng(Stream s, Netlist nl)
		{
			GenPng(s, nl, (g, sx, sy) => { nl.DrawFloorplan(g, sx, sy); });
		}

		private static void GenAllPng(Stream s, Netlist nl)
		{
			GenPng(s, nl, (g, sx, sy) => {
				nl.DrawFloorplan(g, sx, sy);
				nl.DrawCells(g, sx, sy);
				nl.DrawWires(g, sx, sy);
				nl.DrawLabels(g, sx, sy);
			});
		}
	}
}

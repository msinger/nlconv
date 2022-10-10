﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	class MainClass
	{
		public static int Main(string[] args)
		{
			const string home = "http://iceboy.a-singer.de/";
			const string map  = "http://iceboy.a-singer.de/dmg_cpu_b_map/?wires=0";

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
				".bg_green { background-color: rgba(0, 255, 0, 0.3); }" +
				".bg_blue { background-color: rgba(0, 0, 255, 0.3); }" +
				".bg_yellow { background-color: rgba(255, 255, 0, 0.3); }" +
				".bg_cyan { background-color: rgba(0, 255, 255, 0.3); }" +
				".bg_magenta { background-color: rgba(255, 0, 255, 0.3); }" +
				".bg_orange { background-color: rgba(255, 127, 0, 0.3); }" +
				".bg_purple { background-color: rgba(127, 0, 127, 0.3); }" +
				".bg_turquoise { background-color: rgba(0, 127, 127, 0.3); }" +
				".bg_darkgreen { background-color: rgba(0, 127, 0, 0.3); }" +
				".bg_black { background-color: rgba(0, 0, 0, 0.3); }" +
				".bg_white { background-color: rgba(255, 255, 255, 0.3); }" +
				"</style>";

			const string footer =
				"<p><a rel=\"license\" " +
				      "href=\"http://creativecommons.org/licenses/by-sa/4.0/\">" +
				"<img alt=\"Creative Commons License\" style=\"border-width:0\" " +
				     "src=\"https://i.creativecommons.org/l/by-sa/4.0/88x31.png\" />" +
				"</a><br />This work is licensed under a " +
				"<a rel=\"license\" " +
				   "href=\"http://creativecommons.org/licenses/by-sa/4.0/\">" +
				"Creative Commons Attribution-ShareAlike 4.0 International License</a>.</p>";

			Netlist nl = new Netlist();
			nl.DefaultDocUrl = "http://iceboy.a-singer.de/doc/dmg_cells.html#%t";

			string l;
			while ((l = Console.ReadLine()) != null)
				nl.WriteLine(l);
			nl.Flush();

			Console.WriteLine("<!DOCTYPE html>");
			Console.Write("<html lang=\"en\"><head><meta charset=\"UTF-8\"><title>Netlist</title>");
			Console.Write(style);
			Console.Write("</head><body><nav><p>");
			Console.Write("<a href=\"" + home + "\">Home</a>");
			Console.Write("</p><hr></nav><main><h1>Netlist</h1>");
			nl.ToHtml(Console.Out, map);
			Console.Write("</main><footer><hr>" + footer);
			Console.Write("</footer></body></html>");

			return 0;
		}
	}
}

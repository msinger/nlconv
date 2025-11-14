using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace nlconv
{
	public static class StringSVExtension
	{
		private static readonly Dictionary<char, string> empty = new Dictionary<char, string>();

		private static readonly List<string> keywords = new List<string> {
			"accept_on", "alias", "always", "always_comb", "always_ff", "always_latch", "and", "assert", "assign",
			"assume", "automatic", "before", "begin", "bind", "bins", "binsof", "bit", "break", "buf", "bufif0",
			"bufif1", "byte", "case", "casex", "casez", "cell", "chandle", "checker", "class", "clocking", "cmos",
			"config", "const", "constraint", "context", "continue", "cover", "covergroup", "coverpoint", "cross",
			"deassign", "default", "defparam", "design", "disable", "dist", "do", "edge", "else", "end", "endcase",
			"endchecker", "endclass", "endclocking", "endconfig", "endfunction", "endgenerate", "endgroup",
			"endinterface", "endmodule", "endpackage", "endprimitive", "endprogram", "endproperty", "endspecific",
			"endsequence", "endtable", "endtask", "enum", "event", "eventually", "expect", "export", "extents",
			"extern", "final", "first_match", "for", "force", "foreach", "forever", "fork", "forkjoin", "function",
			"generate", "genvar", "global", "highz0", "highz1", "if", "iff", "ifnone", "ignore_bins", "illegal_bins",
			"implements", "implies", "import", "incdir", "include", "initial", "inout", "input", "inside", "instance",
			"int", "integer", "interconnect", "interface", "intersect", "join", "join_any", "join_none", "large",
			"let", "liblist", "library", "local", "localparam", "logic", "longint", "macromodule", "matches",
			"medium", "modport", "module", "nand", "negedge", "nettype", "new", "nexttime", "nmos", "nor",
			"noshowcancelled", "not", "notif0", "notif1", "null", "or", "output", "package", "packed", "parameter",
			"pmos", "posedge", "primitive", "priority", "program", "property", "protected", "pull0", "pull1",
			"pulldown", "pullup", "pulsestyle_ondetect", "pulsestyle_onevent", "pure", "rand", "randc", "randcase",
			"randsequence", "rcmos", "real", "realtime", "ref", "reg", "reject_on", "release", "repeat", "restrict",
			"return", "rnmos", "rpmos", "rtran", "rtranif0", "rtranif1", "s_always", "s_eventually", "s_nexttime",
			"s_until", "s_until_with", "scalared", "sequence", "shortint", "shortreal", "showcancelled", "signed",
			"small", "soft", "solve", "specify", "specparam", "static", "string", "strong", "strong0", "strong1",
			"struct", "super", "supply0", "supply1", "sync_accept_on", "sync_reject_on", "table", "tagged", "task",
			"this", "throughout", "time", "timeprecision", "timeunit", "tran", "tranif0", "tranif1", "tri", "tri0",
			"tri1", "triand", "trior", "trireg", "type", "typedef", "union", "unique", "unique0", "unsigned", "until",
			"until_with", "untyped", "use", "uwire", "var", "vectored", "virtual", "void", "wait", "wait_order",
			"wand", "weak", "weak0", "weak1", "while", "wildcard", "wire", "with", "within", "wor", "xnor", "xor"
		};

		public static string ToSystemVerilog(this string s, SVNameProperties p = SVNameProperties.Unvectorized, string prefix = "", string suffix = "")
		{
			bool hasIndex = s.HasIndex(out int index);
			string basename = s;
			if (hasIndex)
				basename = s.Substring(0, s.LastIndexOf('['));
			string svname = BarProcessor.ProcessBars(basename, "", "_n", "_", empty);
			svname = prefix + svname + suffix;
			if (hasIndex && p == SVNameProperties.Unvectorized)
			{
				if (char.IsDigit(svname[svname.Length - 1]))
					svname += '_';
				svname += index.ToString(CultureInfo.InvariantCulture);
			}
			if (NeedsEscape(svname))
				svname = "\\" + svname + " ";
			if (hasIndex && p == SVNameProperties.Vector)
				svname += "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
			return svname;
		}

		public static bool HasIndex(this string s, out int index)
		{
			index = -1;
			int openIndex = s.LastIndexOf('[');
			if (openIndex >= 0 && s.EndsWith("]"))
			{
				string numberPart = s.Substring(openIndex + 1, s.Length - openIndex - 2);
				return int.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
			}
			return false;
		}

		private static bool NeedsEscape(string s)
		{
			if (string.IsNullOrEmpty(s))
				return true;
			if (!char.IsLetter(s[0]) && s[0] != '_')
				return true;
			foreach (char c in s)
			{
				if (char.IsLetter(c))
					continue;
				if (char.IsDigit(c))
					continue;
				if (c == '_' || c == '$')
					continue;
				return true;
			}
			return keywords.Contains(s);
		}
	}
}

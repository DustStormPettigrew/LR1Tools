using LibLR1;
using System.IO;

namespace LR1Tools.CSetEditor
{
	public static class Tool
	{
		public static CSet Load(string p_path) { return new CSet(File.ReadAllBytes(p_path)); }
	}
}

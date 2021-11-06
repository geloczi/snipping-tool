using System.Linq;
using System.Windows.Forms;

namespace SnippingToolPoc
{
	public static class VirtualDesktop
	{
		public static int Left { get; private set; }
		public static int Right { get; private set; }
		public static int Top { get; private set; }
		public static int Bottom { get; private set; }

		static VirtualDesktop()
		{
			Refresh();
		}

		public static void Refresh()
		{
			Left = Screen.AllScreens.Min(x => x.Bounds.Left);
			Right = Screen.AllScreens.Max(x => x.Bounds.Right);
			Top = Screen.AllScreens.Min(x => x.Bounds.Top);
			Bottom = Screen.AllScreens.Max(x => x.Bounds.Bottom);
		}
	}
}

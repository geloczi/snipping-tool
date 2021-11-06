using System.Drawing;

namespace SnippingToolPoc
{
	public class SnippingToolOptions
	{
		public int SelectionThickness { get; set; } = 2;
		public Color SelectionColor { get; set; } = Color.Red;
		public bool FreezeScreen { get; set; } = true;
	}
}

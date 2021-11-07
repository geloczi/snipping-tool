using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnippingToolPoc
{
	/// <summary>
	/// Simple screenshot implementation
	/// </summary>
	/// <seealso cref="System.Windows.Forms.Form" />
	public class SnippingTool : Form
	{
		#region Fields
		private const int SelectionThickness = 2;
#if DEBUG
		private readonly bool IsDebug = true;
#else
		private readonly bool IsDebug = false;
#endif
		private readonly Color SelectionColor = Color.Red;
		private readonly Color TransparentColor = Color.FromArgb(0, 0, 255);
		private readonly Brush SelectionBrush;
		private readonly Brush SelectionClearBrush;
		private readonly bool FreezeScreen;
		private readonly Rectangle Desktop = new Rectangle(SystemInformation.VirtualScreen.Left, SystemInformation.VirtualScreen.Top, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
		private Bitmap FreezeScreenBitmap;
		private Graphics _graphics;
		private Point? _mouseDownPoint;
		private Point? _mouseUpPoint;
		private Rectangle? _lastRectangle;
		private Rectangle[] _lastBorders = new Rectangle[0];
		#endregion

		#region Properties		
		/// <summary>
		/// Gets the screenshot result.
		/// </summary>
		/// <value>
		/// The result.
		/// </value>
		public Bitmap Result { get; private set; }
		#endregion

		#region Constructor		
		/// <summary>
		/// Initializes a new instance of the <see cref="SnippingTool"/> class.
		/// </summary>
		/// <param name="freezeScreen">True to freeze the screen, False to show the snipping tool on the live screen</param>
		public SnippingTool(bool freezeScreen = false) : base()
		{
			FreezeScreen = freezeScreen;
			SelectionBrush = new SolidBrush(SelectionColor);
			SelectionClearBrush = new SolidBrush(TransparentColor);
			
			if (IsDebug)
			{
				ShowInTaskbar = true;
			}
			else
			{
				ShowInTaskbar = false;
				TopLevel = true;
				TopMost = true;
			}
			KeyPreview = true;
			FormBorderStyle = FormBorderStyle.None;
			Cursor = Cursors.Cross;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(Desktop.Left, Desktop.Top);
			Size = new Size(Desktop.Right - Desktop.Left, Desktop.Bottom - Desktop.Top);

			if (FreezeScreen)
			{
				// A full screenshot is captured now and will be used as source
				BackColor = Color.Black;
				FreezeScreenBitmap = CaptureVirtualDesktopToBitmap();
			}
			else
			{
				// The form will be transparent
				BackColor = TransparentColor;
				TransparencyKey = TransparentColor;
			}

			// Events
			FormClosing += SnippingToolWinForms_FormClosing;
			PreviewKeyDown += SnippingToolWinForms_PreviewKeyDown;
			MouseDown += SnippingToolWinForms_MouseDown;
			MouseUp += SnippingToolWinForms_MouseUp;
			MouseMove += SnippingToolWinForms_MouseMove;
			LostFocus += SnippingToolWinForms_LostFocus;
			Shown += SnippingToolWinForms_Shown;
		}

		#endregion

		private void SnippingToolWinForms_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Escape:
					Close();
					break;
				case Keys.F12:
					CaptureFullScreen();
					break;
			}
		}

		private void SnippingToolWinForms_Shown(object sender, EventArgs e)
		{
			Shown -= SnippingToolWinForms_Shown;
			_graphics = CreateGraphics();
			FullErase();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			// Disabled
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			// Disabled
		}

		private void SnippingToolWinForms_LostFocus(object sender, EventArgs e)
		{
			if (!IsDebug)
				Close();
		}

		private void SnippingToolWinForms_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && (!_mouseUpPoint.HasValue || _mouseUpPoint.Value != e.Location))
			{
				_mouseUpPoint = e.Location;
				Render();
			}
		}

		private void SnippingToolWinForms_MouseUp(object sender, MouseEventArgs e)
		{
			FullErase();
			if (_mouseDownPoint.HasValue)
			{
				_mouseUpPoint = e.Location;
				CaptureSelection();
				return;
			}
		}

		private void SnippingToolWinForms_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				_mouseDownPoint = e.Location;
			else
				ClearSelection();
		}

		private void ClearSelection()
		{
			PartialErase();
			_mouseDownPoint = null;
			_mouseUpPoint = null;
			_lastRectangle = null;
		}

		private void SnippingToolWinForms_FormClosing(object sender, FormClosingEventArgs e)
		{
			_graphics.Dispose();
		}

		protected override void Dispose(bool disposing)
		{
			FreezeScreenBitmap?.Dispose();
			Result?.Dispose();
			base.Dispose(disposing);
		}

		private void FullErase()
		{
			if (FreezeScreen)
				_graphics.DrawImageUnscaled(FreezeScreenBitmap, 0, 0);
			else
				_graphics.Clear(BackColor);
		}

		private void PartialErase()
		{
			if (_lastBorders.Length == 0 || !_lastRectangle.HasValue)
				return;

			if (FreezeScreen)
			{
				_graphics.DrawImage(FreezeScreenBitmap, _lastRectangle.Value, _lastRectangle.Value, GraphicsUnit.Pixel);

				// This is faster, but produces artifacts on multi-monitor setups
				//foreach (var b in _lastBorders)
				//	_graphics.DrawImage(FreezeScreenBitmap, b, b, GraphicsUnit.Pixel);
			}
			else
			{
				_graphics.FillRectangles(SelectionClearBrush, _lastBorders);
			}
		}

		private void Render()
		{
			PartialErase();
			if (_mouseDownPoint.HasValue && _mouseUpPoint.HasValue)
			{
				_lastRectangle = PointsToRectangle(_mouseDownPoint.Value, _mouseUpPoint.Value);
				_lastBorders = RectangleToBorders(_lastRectangle.Value, SelectionThickness);
				_graphics.FillRectangles(SelectionBrush, _lastBorders);
			}
		}

		private Rectangle PointsToRectangle(Point p1, Point p2)
		{
			return new Rectangle
			(
				p1.X <= p2.X ? p1.X : p2.X,
				p1.Y <= p2.Y ? p1.Y : p2.Y,
				Math.Abs(p1.X - p2.X),
				Math.Abs(p1.Y - p2.Y)
			);
		}

		private Rectangle[] RectangleToBorders(Rectangle rect, int thickness)
		{
			if (rect.Width <= thickness * 2 || rect.Height <= thickness * 2)
				return new[] { rect };

			var b = new Rectangle[4];

			// Top
			b[0].X = rect.X;
			b[0].Y = rect.Y;
			b[0].Width = rect.Width;
			b[0].Height = thickness;

			// Bottom
			b[1].X = rect.X;
			b[1].Y = rect.Bottom - thickness;
			b[1].Width = rect.Width;
			b[1].Height = thickness;

			// Left
			b[2].X = rect.X;
			b[2].Y = rect.Y + thickness;
			b[2].Width = thickness;
			b[2].Height = Math.Max(1, rect.Height - (thickness * 2));

			// Right
			b[3].X = rect.Right - thickness;
			b[3].Y = rect.Y + thickness;
			b[3].Width = thickness;
			b[3].Height = Math.Max(1, rect.Height - (thickness * 2));

			return b;
		}

		private void CaptureSelection()
		{
			if (_lastRectangle.HasValue)
			{
				var rect = _lastRectangle.Value;
				Result = new Bitmap(rect.Width, rect.Height);
				using (var g = Graphics.FromImage(Result))
				{
					if (FreezeScreen)
					{
						g.DrawImage(FreezeScreenBitmap, 0, 0, rect, GraphicsUnit.Pixel);
					}
					else
					{
						g.Clear(Color.Black);
						g.CopyFromScreen(Desktop.Left + rect.X, Desktop.Top + rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
					}
				}
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void CaptureFullScreen()
		{
			if (FreezeScreen)
			{
				Result = FreezeScreenBitmap;
				FreezeScreenBitmap = null;
			}
			else
			{
				_graphics.Clear(TransparentColor);
				Result = CaptureVirtualDesktopToBitmap();
			}
			DialogResult = DialogResult.OK;
			Close();
		}

		private Bitmap CaptureVirtualDesktopToBitmap()
		{
			var bmp = new Bitmap(Desktop.Right - Desktop.Left, Desktop.Bottom - Desktop.Top);
			using (var g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.Black);
				g.CopyFromScreen(Desktop.Left, Desktop.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
			}
			return bmp;
		}
	}
}

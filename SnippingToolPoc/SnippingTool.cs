using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnippingToolPoc
{
	public class SnippingTool : Form
	{
#if DEBUG
		private readonly bool IsDebug = true;
#else
		private readonly bool IsDebug = false;
#endif
		private readonly Bitmap _screenshotOnStartup;
		private readonly Color TransparentColor = Color.FromArgb(0, 0, 255);
		private readonly SnippingToolOptions _options;
		private readonly Brush _selectionBrush;
		private readonly Brush _selectionClearBrush;
		private Graphics _graphics;
		private Point? _mouseDownPoint;
		private Point? _mouseUpPoint;
		private Rectangle? _lastRectangle;
		private Rectangle[] _lastBorders = new Rectangle[0];

		public Bitmap Result { get; private set; }

		public SnippingTool() : this(new SnippingToolOptions())
		{
		}
		public SnippingTool(SnippingToolOptions options) : base()
		{
			_options = options;
			_selectionBrush = new SolidBrush(_options.SelectionColor);
			_selectionClearBrush = new SolidBrush(TransparentColor);

			// Appearance
			if (IsDebug)
			{
				ShowInTaskbar = true;
			}
			else
			{
				TopLevel = true;
				TopMost = true;
				ShowInTaskbar = false;
			}
			KeyPreview = true;
			FormBorderStyle = FormBorderStyle.None;
			Cursor = Cursors.Cross;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(VirtualDesktop.Left, VirtualDesktop.Top);
			Size = new Size(VirtualDesktop.Right - VirtualDesktop.Left, VirtualDesktop.Bottom - VirtualDesktop.Top);

			if (_options.FreezeScreen)
			{
				// A full screenshot is captured now and will be used as source
				BackColor = Color.Black;

				// Capture on startup, freeze screen mode
				_screenshotOnStartup = CaptureVirtualDesktopToBitmap();
			}
			else
			{
				// Transparent background mode, screenshot will be captured at the end of selection
				if (_options.SelectionColor == TransparentColor)
					throw new ArgumentException("Selection color must be different than the transparency key.", nameof(_options.SelectionColor));

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
			// Disabled, everything is manually rendered
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			// Disabled, everything is manually rendered
		}

		private void SnippingToolWinForms_LostFocus(object sender, EventArgs e)
		{
			if (!IsDebug)
				Close();

		}

		private void SnippingToolWinForms_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
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
			{
				_mouseDownPoint = e.Location;
			}
			else
			{
				ClearSelection();
			}
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
			_screenshotOnStartup?.Dispose();
			Result?.Dispose();
			base.Dispose(disposing);
		}

		private void FullErase()
		{
			if (_options.FreezeScreen)
			{
				_graphics.DrawImageUnscaled(_screenshotOnStartup, 0, 0);
			}
			else
			{
				_graphics.Clear(BackColor);
			}
		}

		private void PartialErase()
		{
			if (_lastBorders.Length == 0 || !_lastRectangle.HasValue)
				return;

			if (_options.FreezeScreen)
			{
				_graphics.DrawImage(_screenshotOnStartup, _lastRectangle.Value, _lastRectangle.Value, GraphicsUnit.Pixel);

				// This is faster, but produces artifacts on multi-monitor setups
				//foreach (var b in _lastBorders)
				//	_graphics.DrawImage(_screenshotOnStartup, b, b, GraphicsUnit.Pixel);
			}
			else
			{
				_graphics.FillRectangles(_selectionClearBrush, _lastBorders);
			}
		}

		private void Render()
		{
			PartialErase();
			if (_mouseDownPoint.HasValue && _mouseUpPoint.HasValue)
			{
				_lastRectangle = PointsToRectangle(_mouseDownPoint.Value, _mouseUpPoint.Value);
				_lastBorders = RectangleToBorders(_lastRectangle.Value, _options.SelectionThickness);
				_graphics.FillRectangles(_selectionBrush, _lastBorders);
			}
		}

		private Rectangle PointsToRectangle(Point p1, Point p2)
		{
			var rect = new Rectangle
			(
				p1.X <= p2.X ? p1.X : p2.X,
				p1.Y <= p2.Y ? p1.Y : p2.Y,
				Math.Abs(p1.X - p2.X),
				Math.Abs(p1.Y - p2.Y)
			);
			return rect;
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

		private Bitmap CaptureVirtualDesktopToBitmap()
		{
			var bmp = new Bitmap(VirtualDesktop.Right - VirtualDesktop.Left, VirtualDesktop.Bottom - VirtualDesktop.Top);
			using (var g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.Black);
				g.CopyFromScreen(VirtualDesktop.Left, VirtualDesktop.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
			}
			return bmp;
		}

		private void CaptureSelection()
		{
			if (_lastRectangle.HasValue)
			{
				var rect = _lastRectangle.Value;
				Result = new Bitmap(rect.Width, rect.Height);
				using (var g = Graphics.FromImage(Result))
				{
					if (_options.FreezeScreen)
					{
						g.DrawImage(_screenshotOnStartup, 0, 0, rect, GraphicsUnit.Pixel);
					}
					else
					{
						g.Clear(Color.Black);
						g.CopyFromScreen(VirtualDesktop.Left + rect.X, VirtualDesktop.Top + rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
					}
				}
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void CaptureFullScreen()
		{
			if (_options.FreezeScreen)
			{
				Result = _screenshotOnStartup;
			}
			else
			{
				_graphics.Clear(TransparentColor);
				Result = CaptureVirtualDesktopToBitmap();
			}
			DialogResult = DialogResult.OK;
			Close();
		}

	}
}

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SnippingToolPoc
{
	static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			try
			{
				using (var snippingTool = new SnippingTool())
				{
					if (snippingTool.ShowDialog() == DialogResult.OK)
					{
						var sfd = new SaveFileDialog();
						sfd.Filter = "Save to PNG|*.png|Save to JPB|*.jpg|Save to BMP|*.bmp|Copy to clipboard|";
						sfd.DefaultExt = ".png";
						sfd.Title = "Save screenshot";
						sfd.FileName = DateTime.Now.ToString("yyyyMMdd-hhmmss");
						if (sfd.ShowDialog() == DialogResult.OK)
						{
							if (sfd.FilterIndex == 4)
							{
								// Save to clipboard
								Clipboard.SetImage(snippingTool.Result);
							}
							else
							{
								// Save to file
								switch (Path.GetExtension(sfd.FileName).ToLowerInvariant())
								{
									case ".png":
										snippingTool.Result.Save(sfd.FileName, ImageFormat.Png);
										break;
									case ".jpg":
										snippingTool.Result.Save(sfd.FileName, ImageFormat.Jpeg);
										break;
									default:
										snippingTool.Result.Save(sfd.FileName, ImageFormat.Bmp);
										break;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			Application.Exit();
		}
	}
}
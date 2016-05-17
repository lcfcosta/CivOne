// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CivOne.Enums;
using CivOne.Events;
using CivOne.GFX;
using CivOne.Interfaces;
using CivOne.Screens;
using CivOne.Templates;

namespace CivOne
{
	internal partial class Window : Form
	{
		private delegate void DelegateRefreshWindow();
		private delegate void DelegateScreenUpdate();
		
		private Cursor _hiddenCursor;
		private Cursor[,] _cursorPointer,_cursorGoto;
		private MouseCursor _currentCursor = MouseCursor.Pointer;
		
		private IScreen TopScreen
		{
			get
			{
				return Common.Screens.LastOrDefault();
			}
		}
		
		private int CanvasX
		{
			get
			{
				return (ClientSize.Width - CanvasWidth) / 2;
			}
		}
		
		private int CanvasY
		{
			get
			{
				return (ClientSize.Height - CanvasHeight) / 2;
			}
		}
		
		private int ScaleX
		{
			get
			{
				return (int)Math.Floor((float)ClientSize.Width / 320);
			}
		}
		
		private int ScaleY
		{
			get
			{
				return (int)Math.Floor((float)ClientSize.Height / 200);
			}
		}
		
		private void ScreenUpdate()
		{
			if (InvokeRequired)
			{
				Invoke(new DelegateScreenUpdate(ScreenUpdate));
				return;
			}

			Refresh();
		}
		
		private void RefreshWindow()
		{
			if (InvokeRequired)
			{
				Invoke(new DelegateRefreshWindow(RefreshWindow));
				return;
			}
			
			if (Common.ReloadSettings)
			{
				FormBorderStyle = Settings.Instance.FullScreen ? FormBorderStyle.None : FormBorderStyle.Sizable;
				WindowState = Settings.Instance.FullScreen ? FormWindowState.Maximized : FormWindowState.Normal;
				LoadCursors();
				return;
			}
			
			if (TickThread.IsAlive && Common.EndGame)
			{
				TickThread.Abort();
			}
			
			if (!TickThread.IsAlive)
			{
				Close();
				return;
			}
			
			// Update cursor
			if (Common.Screens.Length > 0 && _currentCursor != TopScreen.Cursor)
			{
				_currentCursor = TopScreen.Cursor;
				OnMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, Cursor.Position.X, Cursor.Position.Y, 0));
			}
			
			// Refresh the screen if there's an update
			if (HasUpdate) Refresh();
		}
		
		private void LoadCursor(ref Cursor[,] cursor, int x, int y)
		{
			int sx = (int)Math.Floor((float)ClientSize.Width / 320);
			int sy = (int)Math.Floor((float)ClientSize.Height / 200);
			cursor = new Cursor[sx, sy];
			Bitmap img = Resources.Instance.GetPart("SP257", x, y, 16, 16);
			
			for (int cx = 0; cx < sx; cx++)
			for (int cy = 0; cy < sy; cy++)
			{
				Bitmap res = new Bitmap(32 * sx, 32 * sy, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				Graphics gfx = Graphics.FromImage(res);
				gfx.InterpolationMode = InterpolationMode.NearestNeighbor;
				gfx.PixelOffsetMode = PixelOffsetMode.Half;
				gfx.DrawImage(img, (15 * sx) - cx, (15 * sy) - cy, 16 * sx, 16 * sy);
				cursor[cx, cy] = new Cursor(res.GetHicon());
			}
		}
		
		private void LoadCursors()
		{
			_hiddenCursor = new Cursor(new Bitmap(16, 16).GetHicon());
			LoadCursor(ref _cursorPointer, 112, 32);
			LoadCursor(ref _cursorGoto, 32, 32);
		}
		
		private void ToggleFullScreen()
		{
			if (WindowState == FormWindowState.Maximized)
			{
				Console.WriteLine("Full screen off");
				FormBorderStyle = FormBorderStyle.Sizable;
				WindowState = FormWindowState.Normal;
				LoadCursors();
				return;
			}
			
			Console.WriteLine("Full screen on");
			FormBorderStyle = FormBorderStyle.None;
			WindowState = FormWindowState.Maximized;
			LoadCursors();
		}
		
		private ScreenEventArgs ScaleMouseEventArgs(MouseEventArgs args)
		{
			int xx = args.X - CanvasX, yy = args.Y - CanvasY;
			MouseButton buttons = MouseButton.None;
			if (args.Button == MouseButtons.Left) buttons = MouseButton.Left;
			else if (args.Button == MouseButtons.Right) buttons = MouseButton.Right;
			return new ScreenEventArgs((int)Math.Floor((float)xx / ScaleX), (int)Math.Floor((float)yy / ScaleY), buttons);
		}
		
		private void OnFormClosing(object sender, FormClosingEventArgs args)
		{
			if (TickThread.IsAlive)
			{
				TickThread.Abort();
				args.Cancel = true;
			}
		}
		
		private void OnLoad(object sender, EventArgs args)
		{			
			// Start tick thread
			TickThread = new Thread(new ThreadStart(SetGameTick));
			TickThread.Start();
			
			// Load cursors
			LoadCursors();
		}
		
		private void OnPaint(object sender, PaintEventArgs args)
		{
			args.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			args.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
			
			if (Common.Screens.Length == 0) return;
			
			Color[] colours = TopScreen.Canvas.Image.Palette.Entries;
			colours[0] = Color.Black;
			
			_canvas = new Picture(320, 200, colours);
			foreach (IScreen screen in Common.Screens)
			{
				_canvas.AddLayer(screen.Canvas.Image, 0, 0);
			}
			
			args.Graphics.Clear(Color.Black);
			args.Graphics.DrawImage(_canvas.Image, CanvasX, CanvasY, CanvasWidth, CanvasHeight);
		}
		
		private void OnKeyDown(object sender, KeyEventArgs args)
		{
			if (args.Alt || args.Control)
			{
				if (args.KeyCode == Keys.Enter)
				{
					ToggleFullScreen();
				}
				if (args.Control && args.KeyCode == Keys.F5)
				{
					SaveScreen();
				}
				if (args.Control && args.KeyCode == Keys.F6 && Game.Instance != null)
				{
					Map.Instance.SaveBitmap();
				}
				args.SuppressKeyPress = true;
				return;
			}
			
			if (TopScreen != null && TopScreen.KeyDown(args)) ScreenUpdate();
			
			if (args.KeyCode == Keys.F10)
			{
				args.SuppressKeyPress = true;
			}
		}
		
		private void OnMouseDown(object sender, MouseEventArgs args)
		{
			ScreenEventArgs screenArgs = ScaleMouseEventArgs(args);
			if (TopScreen != null && TopScreen.MouseDown(screenArgs)) ScreenUpdate();
		}
		
		private void OnMouseUp(object sender, MouseEventArgs args)
		{
			ScreenEventArgs screenArgs = ScaleMouseEventArgs(args);
			if (TopScreen != null && TopScreen.MouseUp(screenArgs)) ScreenUpdate();
		}
		
		private void MouseDrag(MouseEventArgs args)
		{
			ScreenEventArgs screenArgs = ScaleMouseEventArgs(args);
			if (TopScreen != null && TopScreen.MouseDrag(screenArgs)) ScreenUpdate();
		}
		
		private void OnMouseMove(object sender, MouseEventArgs args)
		{
			if (args.Button > 0) MouseDrag(args);
			
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				// Linux does not support the custom cursors. This is a temporary fix.
				return;
			}
			
			if (_currentCursor == MouseCursor.None)
			{
				Cursor = _hiddenCursor;
				return;
			}
			
			// apply cursor correction
			int sx = (int)Math.Floor((float)ClientSize.Width / 320);
			int sy = (int)Math.Floor((float)ClientSize.Height / 200);
			if (args.X < 0 || args.Y < 0) return;
			switch (_currentCursor)
			{
				case MouseCursor.Pointer:
					Cursor = _cursorPointer[args.X % sx, args.Y % sy];
					break;
				case MouseCursor.Goto:
					Cursor = _cursorGoto[args.X % sx, args.Y % sy];
					break;
			}
		}
		
		private void OnResizeEnd(object sender, EventArgs args)
		{
			int width = ScaleX * 320;
			int height = ScaleY * 200;
			
			ClientSize = new Size(width, height);
			LoadCursors();
			Refresh();
		}
		
		public string BrowseDataFolder()
		{
			using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog()
				{
					Description = "Select the folder containing the original Civilization data files.",
					RootFolder = Environment.SpecialFolder.MyComputer
				})
			{
				if (folderBrowser.ShowDialog() == DialogResult.OK)
					return folderBrowser.SelectedPath;
				return Settings.Instance.DataDirectory;
			}
		}
		
		public static void CreateWindow(string screen)
		{
			Application.Run(new Window(screen));
		}
		
		private Window(string screen)
		{
			SuspendLayout();
			
			// Set Window properties
			DoubleBuffered = true;
			MaximizeBox = false;
			ClientSize = new Size(320 * Settings.Instance.ScaleX, 200 * Settings.Instance.ScaleY);
			FormBorderStyle = Settings.Instance.FullScreen ? FormBorderStyle.None : FormBorderStyle.Sizable;
			WindowState = Settings.Instance.FullScreen ? FormWindowState.Maximized : FormWindowState.Normal;
			Text = "CivOne";
			
			// Set Window events
			FormClosing += OnFormClosing;
			Load += OnLoad;
			Paint += OnPaint;
			KeyDown += OnKeyDown;
			MouseDown += OnMouseDown;
			MouseUp += OnMouseUp;
			MouseMove += OnMouseMove;
			ResizeEnd += OnResizeEnd;
			
			// Load the first screen
			Init(screen);
			
			ResumeLayout(false);
		}
		
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
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
using System.Windows.Forms;
using CivOne.Enums;
using CivOne.GFX;
using CivOne.Templates;

namespace CivOne.Screens
{
	internal class Menu : BaseScreen
	{
		internal class Item
		{ 
			public event EventHandler Selected;
			public bool Enabled = true;
			public string Text;
			public readonly int Value;
			
			internal void Select()
			{
				if (Selected == null) return;
				Selected(this, null);
			}
			
			public Item(string text, int value = -1)
			{
				Text = text;
				Value = value;
			}
		}
		
		public readonly List<Item> Items = new List<Item>();
		public string Title { get; set; }
		public int FontId { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int Width { get; set; }
		public byte TitleColour { get; set; }
		public byte ActiveColour { get; set; }
		public byte TextColour { get; set; }
		public byte DisabledColour { get; set; }
		
		private bool _change = true;
		private int _activeItem = 0;
		public int ActiveItem
		{
			get
			{
				return _activeItem;
			}
			set
			{
				_change = true;
				_activeItem = value;
				if (_activeItem < 0) _activeItem = 0;
				if (_activeItem >= Items.Count) _activeItem = (Items.Count - 1);
			}
		}
		
		public override bool HasUpdate(uint gameTick)
		{
			int fontHeight = Resources.Instance.GetFontHeight(FontId);
			if (_change)
			{
				int yy = Y + (_activeItem * fontHeight);
				int offsetY = 0;
				
				_canvas.FillRectangle(0, 0, 0, 320, 200);
				if (Title != null)
				{
					_canvas.DrawText(Title, FontId, TitleColour, X + 8, Y + 1);
					offsetY = fontHeight;
				}				
				_canvas.FillRectangle(ActiveColour, X, yy + offsetY, Width, fontHeight);
				for (int i = 0; i < Items.Count; i++)
				{
					yy = Y + (i * fontHeight) + offsetY;
					_canvas.DrawText(Items[i].Text, FontId, (byte)(Items[i].Enabled ? TextColour : DisabledColour), X + 8, yy + 1);
				}				
				_change = false;
				return true;
			}
			return false;
		}
		
		public override bool KeyDown(KeyEventArgs args)
		{
			switch (args.KeyCode)
			{
				case Keys.Up:
					ActiveItem--;
					return true;
				case Keys.Down:
					ActiveItem++;
					return true;
				case Keys.Enter:
					if (!Items[_activeItem].Enabled) return false;
					Items[_activeItem].Select();
					return true;
			}
			return false;
		}
		
		public override bool MouseDown(MouseEventArgs args)
		{
			return false;
		}
		
		public void Close()
		{
			Destroy();
		}
		
		public Menu(Color[] colours)
		{
			Cursor = MouseCursor.Pointer;
			
			_canvas = new Picture(320, 200, colours);
		}
	}
}
// ULDB: upstream's ImageBrowserItem and ImageBrowserCategoryItem without the GDI+ drawing
// (Controls/ImageBrowserItem.cs draws the tile with Graphics; ImageBrowserTileView draws it in
// Avalonia). Members, names and ordering are upstream's.

using System;
using CodeImp.DoomBuilder.Data;

namespace CodeImp.DoomBuilder.Controls
{
	// Values order is used when sorting ImageBrowserItems!
	internal enum ImageBrowserItemType
	{
		FOLDER_UP,
		FOLDER,
		IMAGE,
	}

	internal class ImageBrowserItem : IComparable<ImageBrowserItem>
	{
		protected ImageData icon;
		private bool imageloaded;
		private bool showfullname;
		protected ImageBrowserItemType itemtype;
		private string tooltip;

		public ImageData Icon { get { return icon; } }
		public ImageBrowserItemType ItemType { get { return itemtype; } }
		public virtual bool IsPreviewLoaded { get { return icon.IsPreviewLoaded; } }
		public bool ShowFullName { set { showfullname = value; } get { return showfullname && (!(icon is PK3FileImage) || !((PK3FileImage)icon).IsBadForLongTextureNames); } }
		public virtual string TextureName { get { return (ShowFullName ? icon.Name : icon.ShortName); } }
		public virtual int TextureNameWidth { get { return ShowFullName ? icon.NameWidth : icon.ShortNameWidth; } }
		public string ToolTip { get { return tooltip; } }

		protected ImageBrowserItem() { }
		public ImageBrowserItem(ImageData icon, string tooltip, bool showfullname)
		{
			this.icon = icon;
			this.itemtype = ImageBrowserItemType.IMAGE;
			this.showfullname = showfullname;
			this.imageloaded = icon.IsPreviewLoaded;
			this.tooltip = tooltip;
		}

		internal bool CheckRedrawNeeded()
		{
			if(icon.IsPreviewLoaded != imageloaded)
			{
				imageloaded = icon.IsPreviewLoaded;
				return true;
			}
			return false;
		}

		public int CompareTo(ImageBrowserItem other)
		{
			if(itemtype != other.itemtype) return ((int)itemtype).CompareTo((int)other.itemtype);
			return this.TextureName.ToUpperInvariant().CompareTo(other.TextureName.ToUpperInvariant());
		}
	}

	internal class ImageBrowserCategoryItem : ImageBrowserItem
	{
		private string groupname;

		public override bool IsPreviewLoaded { get { return true; } }
		public override string TextureName { get { return groupname; } }
		public override int TextureNameWidth { get { return groupname.Length * 7; } }

		public ImageBrowserCategoryItem(ImageBrowserItemType itemtype, string groupname)
		{
			this.groupname = groupname;
			this.itemtype = itemtype;

			switch(itemtype)
			{
				case ImageBrowserItemType.FOLDER:
					icon = General.Map.Data.FolderTexture;
					break;

				case ImageBrowserItemType.FOLDER_UP:
					icon = General.Map.Data.FolderUpTexture;
					break;

				default:
					throw new NotImplementedException("Unsupported ItemType");
			}
		}
	}
}

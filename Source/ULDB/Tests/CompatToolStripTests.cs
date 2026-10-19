using System.Windows.Forms;

namespace ULDB.Tests
{
	// ULDB.Compat's menu/toolbar model: plugins add and remove items through it, and the
	// Avalonia UI follows its Changed events. Ownership must behave as in WinForms.
	public class CompatToolStripTests
	{
		[Fact]
		public void AddingAnItemMovesItFromItsPreviousOwner()
		{
			ToolStrip a = new ToolStrip();
			ToolStrip b = new ToolStrip();
			ToolStripButton button = new ToolStripButton("x");
			a.Items.Add(button);
			b.Items.Add(button);

			Assert.Empty(a.Items);
			Assert.Single(b.Items);
			Assert.Same(b, button.Owner);
		}

		[Fact]
		public void DropDownItemsKnowTheirOwnerItem()
		{
			ToolStripMenuItem parent = new ToolStripMenuItem("Edit");
			ToolStripMenuItem child = new ToolStripMenuItem("Copy");
			parent.DropDownItems.Add(child);

			Assert.Same(parent, child.OwnerItem);
			Assert.True(parent.HasDropDownItems);
		}

		[Fact]
		public void CheckOnClickTogglesAndNotifies()
		{
			ToolStripButton button = new ToolStripButton("lock") { CheckOnClick = true };
			int changed = 0, clicked = 0;
			button.Changed += (s, e) => changed++;
			button.Click += (s, e) => clicked++;

			button.PerformClick();
			Assert.True(button.Checked);
			Assert.Equal(1, clicked);
			Assert.Equal(1, changed);

			button.Enabled = false;
			button.PerformClick(); // Disabled items don't click
			Assert.Equal(1, clicked);
		}

		[Fact]
		public void FindSearchesDropDownsByName()
		{
			MenuStrip menu = new MenuStrip();
			ToolStripMenuItem file = new ToolStripMenuItem("File") { Name = "menufile" };
			file.DropDownItems.Add(new ToolStripMenuItem("Save") { Name = "itemsave" });
			menu.Items.Add(file);

			Assert.Single(menu.Items.Find("itemsave", true));
			Assert.Empty(menu.Items.Find("itemsave", false));
			Assert.Same(file, menu.Items["MENUFILE"]);
		}

		[Fact]
		public void DisposingAnItemRemovesIt()
		{
			ToolStrip strip = new ToolStrip();
			int changes = 0;
			strip.Items.Changed += (s, e) => changes++;
			ToolStripSeparator sep = new ToolStripSeparator();
			strip.Items.Add(sep);
			sep.Dispose();

			Assert.Empty(strip.Items);
			Assert.Equal(2, changes);
		}
	}
}

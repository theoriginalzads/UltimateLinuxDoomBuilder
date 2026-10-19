// ULDB: the Avalonia view of upstream's MainForm. MainForm's own code (Windows/MainForm.cs
// and its designer file) runs unchanged and builds a model of menus, toolbars, status bar
// and display; this window lays them out like UDB and routes input into MainForm's handlers.
//
// Layout (as MainForm.Designer.cs docks it): menu bar and mode controls on one row, the
// main toolbar below; the edit-modes toolbar on the left; the map view in the middle;
// the info panel and status bar at the bottom.

using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Windows;
using ULDB.Input;
using WF = System.Windows.Forms;

namespace ULDB.Shell
{
	internal sealed class MainWindow : Window
	{
		private readonly MainForm form;
		private readonly RenderTargetControl display;
		private readonly ViewportControl viewport;
		private Panel input;
		private WF.Keys modifiers;
		private Point lastpointer;
		private bool closing;

		public MainWindow(MainForm form)
		{
			this.form = form;
			display = form.Display;

			Title = form.Text;
			Width = 1280;
			Height = 860;
			MinWidth = 640;
			MinHeight = 480;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			form.TextChanged += (s, e) => Dispatcher.UIThread.Post(() => Title = form.Text);

			// Top: menu bar with the mode's own controls beside it, then the main toolbar.
			Grid menurow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
			Menu menu = ToolStripViews.CreateMenuBar(Field<WF.MenuStrip>("menumain"));
			Control modecontrols = ToolStripViews.CreateToolBar(Field<WF.ToolStrip>("modecontrolsloolbar"), Orientation.Horizontal);
			Grid.SetColumn(modecontrols, 1);
			menurow.Children.Add(menu);
			menurow.Children.Add(modecontrols);
			Border menuborder = new Border { Child = menurow };
			menuborder.Classes.Add("uldb-bar");

			Control toolbar = ToolStripViews.CreateToolBar(Field<WF.ToolStrip>("toolbar"), Orientation.Horizontal);
			Border toolbarborder = new Border { Child = toolbar, Padding = new Thickness(2, 1) };
			toolbarborder.Classes.Add("uldb-bar");

			// Left: the edit-modes toolbar.
			Control modes = ToolStripViews.CreateToolBar(Field<WF.ToolStrip>("modestoolbar"), Orientation.Vertical);
			Border modesborder = new Border { Child = modes, Padding = new Thickness(1, 2) };
			modesborder.Classes.Add("uldb-bar");

			// Bottom: info panel and status bar.
			Control statusbar = ToolStripViews.CreateToolBar(Field<WF.StatusStrip>("statusbar"), Orientation.Horizontal);
			Border statusborder = new Border { Child = statusbar, Padding = new Thickness(2, 1), MinHeight = 24 };
			statusborder.Classes.Add("uldb-bar");
			Control infopanel = CreateInfoPanel();

			// Middle: the map view, with upstream's splash logo while no map is open.
			viewport = new ViewportControl(display);
			Control view = CreateDisplay(viewport);

			DockPanel root = new DockPanel();
			DockPanel.SetDock(menuborder, Dock.Top);
			DockPanel.SetDock(toolbarborder, Dock.Top);
			DockPanel.SetDock(statusborder, Dock.Bottom);
			DockPanel.SetDock(infopanel, Dock.Bottom);
			DockPanel.SetDock(modesborder, Dock.Left);
			root.Children.Add(menuborder);
			root.Children.Add(toolbarborder);
			root.Children.Add(statusborder);
			root.Children.Add(infopanel);
			root.Children.Add(modesborder);
			root.Children.Add(CreateMiddle(view, modesborder));
			Content = root;

			HookInput();
			HookLifecycle();
		}

		/// <summary>One of MainForm's designer fields (private upstream; names are stable).</summary>
		private T Field<T>(string name) where T : class
		{
			FieldInfo f = typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				?? throw new MissingFieldException(nameof(MainForm), name);
			return (T)f.GetValue(form);
		}

		#region ================== Parts

		// The map view with the dockers beside it. Upstream reserves "dockersspace" (the collapsed
		// tab strip, or the whole docker when it's pinned open) and lays "dockerspanel" over the
		// map at that edge, so an auto-hiding docker expands over the map.
		private Control CreateMiddle(Control view, Control modesborder)
		{
			DockersControl dockersmodel = Field<DockersControl>("dockerspanel");
			WF.Panel dockersspace = Field<WF.Panel>("dockersspace");
			WF.ToolStrip modestoolbar = Field<WF.ToolStrip>("modestoolbar");
			DockersView dockers = new DockersView(dockersmodel);
			Panel middle = new Panel();
			middle.Children.Add(view);
			middle.Children.Add(dockers);

			string last = null;
			void Layout()
			{
				bool right = dockersspace.Dock != WF.DockStyle.Left;
				double space = dockersspace.Visible ? dockersspace.Width : 0;
				string now = right + "|" + space + "|" + modestoolbar.Dock;
				if(now == last) return;
				last = now;
				dockers.HorizontalAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left;
				view.Margin = right ? new Thickness(0, 0, space, 0) : new Thickness(space, 0, 0, 0);
				DockPanel.SetDock(modesborder, modestoolbar.Dock == WF.DockStyle.Right ? Dock.Right : Dock.Left);
			}
			Layout();
			// Upstream changes these without events (SetupInterface, the splitter); check regularly.
			DispatcherTimer poll = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, (s, e) => Layout());
			poll.Start();
			return middle;
		}

		private Control CreateDisplay(ViewportControl gl)
		{
			// Upstream shows a logo on the display until a map is opened (RenderTargetControl.SetSplashLogoDisplay).
			Image splash = new Image { Stretch = Stretch.None, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			Border splashlayer = new Border { Child = splash, IsHitTestVisible = false };
			void UpdateSplash()
			{
				splash.Source = AvaloniaImages.From(display.BackgroundImage);
				splashlayer.IsVisible = display.BackgroundImage != null;
				System.Drawing.Color c = display.BackColor;
				splashlayer.Background = new SolidColorBrush(c.A == 0 ? Colors.Black : Color.FromArgb(255, c.R, c.G, c.B));
			}
			UpdateSplash();
			display.StylesChanged += (s, e) => Dispatcher.UIThread.Post(UpdateSplash);

			// The OpenGL control draws itself and isn't hit-testable, so pointer input is taken
			// by this transparent panel over the same area.
			Panel layers = new Panel { Background = Brushes.Transparent, Focusable = true };
			layers.Children.Add(gl);
			layers.Children.Add(splashlayer);
			input = layers;
			Border frame = new Border { Child = layers, BorderThickness = new Thickness(1) };
			frame.Classes.Add("uldb-display");
			return frame;
		}

		// Info panel heights: expanded (cards) and collapsed (one line), in ULDB's layout.
		private const double InfoPanelExpandedHeight = 126;
		private const double InfoPanelCollapsedHeight = 26;

		private Control CreateInfoPanel()
		{
			// Upstream's info panels (model-ported) are shown by their .axaml views; MainForm shows
			// and hides them, the mode name and the statistics exactly as upstream does.
			ModelBinder binder = new ModelBinder(form);
			WF.Control panelinfo = binder.Watch(Field<WF.Panel>("panelinfo"));
			WF.Label modename = (WF.Label)binder.Watch(Field<WF.Label>("modename"));
			WF.Label collapsed = (WF.Label)binder.Watch(Field<WF.Label>("labelcollapsedinfo"));
			WF.Button toggle = Field<WF.Button>("buttontoggleinfo");
			StatisticsControl statsmodel = (StatisticsControl)binder.Watch(Field<StatisticsControl>("statistics"));
			// Debug builds of upstream show a debug console in place of the mode name; ULDB doesn't
			// show that console, so the mode name stands in for it.
			WF.Control console = binder.Watch(Field<WF.Control>("console"));

			Panel cards = new Panel { Margin = new Thickness(6, 6, 34, 6), ClipToBounds = true };
			void AddCard(WF.Control model, Control view)
			{
				binder.Watch(model);
				binder.Custom(() => view.IsVisible = model.Visible && form.IsInfoPanelExpanded);
				cards.Children.Add(view);
			}
			AddCard(Field<LinedefInfoPanel>("linedefinfo"), new LinedefInfoView(Field<LinedefInfoPanel>("linedefinfo")));
			AddCard(Field<SectorInfoPanel>("sectorinfo"), new SectorInfoView(Field<SectorInfoPanel>("sectorinfo")));
			AddCard(Field<ThingInfoPanel>("thinginfo"), new ThingInfoView(Field<ThingInfoPanel>("thinginfo")));
			AddCard(Field<VertexInfoPanel>("vertexinfo"), new VertexInfoView(Field<VertexInfoPanel>("vertexinfo")));

			// Nothing highlighted: the mode name, and the map statistics on the right.
			TextBlock title = new TextBlock { FontSize = 30, FontWeight = FontWeight.SemiBold, Opacity = 0.3, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0) };
			StatisticsView stats = new StatisticsView(statsmodel) { HorizontalAlignment = HorizontalAlignment.Right };
			Grid moderow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
			Grid.SetColumn(stats, 1);
			moderow.Children.Add(title);
			moderow.Children.Add(stats);
			cards.Children.Add(moderow);

			// Collapsed: one line of info (upstream's labelcollapsedinfo).
			TextBlock collapsedtext = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 34, 0), TextTrimming = TextTrimming.CharacterEllipsis };

			// Expand/collapse (upstream's buttontoggleinfo, which runs the "toggleinfopanel" action).
			PathIcon chevron = new PathIcon { Width = 10, Height = 7 };
			Button togglebutton = new Button
			{
				Content = chevron,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0, 4, 6, 0),
				Width = 24, Height = 20, Padding = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
			};
			togglebutton.Classes.Add("uldb-tool");
			togglebutton.Click += (s, e) => toggle.RaiseClick();

			Panel content = new Panel();
			content.Children.Add(cards);
			content.Children.Add(collapsedtext);
			content.Children.Add(togglebutton);
			Border border = new Border { Child = content, BorderThickness = new Thickness(0, 1, 0, 0) };
			border.Classes.Add("uldb-infopanel");

			binder.Custom(() =>
			{
				bool expanded = form.IsInfoPanelExpanded;
				border.IsVisible = panelinfo.Visible;
				border.Height = expanded ? InfoPanelExpandedHeight : InfoPanelCollapsedHeight;
				cards.IsVisible = expanded;
				bool showname = modename.Visible || console.Visible;
				moderow.IsVisible = showname || statsmodel.Visible;
				title.IsVisible = showname;
				title.Text = modename.Text;
				stats.IsVisible = statsmodel.Visible;
				collapsedtext.IsVisible = collapsed.Visible && !expanded;
				collapsedtext.Text = collapsed.Text;
				chevron.Data = Geometry.Parse(expanded ? "M0,0 L5,5 L10,0 L10,1.8 L5,6.8 L0,1.8 Z" : "M0,6.8 L5,1.8 L10,6.8 L10,5 L5,0 L0,5 Z");
				ToolTip.SetTip(togglebutton, expanded ? "Collapse the info panel" : "Expand the info panel");
			});
			binder.UpdateNow();
			return border;
		}

		#endregion

		#region ================== Input

		// ULDB_TRACE_INPUT=1 logs the input events passed to upstream (for diagnosing input issues).
		private static readonly bool traceinput = Environment.GetEnvironmentVariable("ULDB_TRACE_INPUT") == "1";

		private static void Trace(string what, WF.MouseEventArgs e)
		{
			if(!traceinput) return;
			CodeImp.DoomBuilder.Actions.Action[] actions = General.Actions?.GetActionsByKey(KeyFor(e)) ?? new CodeImp.DoomBuilder.Actions.Action[0];
			General.WriteLogLine("[input] " + what + " button=" + e.Button + " clicks=" + e.Clicks + " at " + e.X + "," + e.Y + " delta=" + e.Delta +
				" actions=" + string.Join(",", Array.ConvertAll(actions, a => a.Name)));
		}

		private static int KeyFor(WF.MouseEventArgs e)
		{
			if(e.Delta > 0) return (int)SpecialKeys.MScrollUp;
			if(e.Delta < 0) return (int)SpecialKeys.MScrollDown;
			switch(e.Button)
			{
				case WF.MouseButtons.Left: return (int)WF.Keys.LButton;
				case WF.MouseButtons.Right: return (int)WF.Keys.RButton;
				case WF.MouseButtons.Middle: return (int)WF.Keys.MButton;
				default: return 0;
			}
		}

		private void HookInput()
		{
			WF.Control.ModifierKeysProvider = () => modifiers;

			// Screen coordinates are device pixels, as in X11. Upstream's visual mode reads the
			// pointer and warps it back to the middle of the window every frame (mouselook).
			display.ScreenMapper = new VisualMapper(viewport);
			form.ScreenMapper = new VisualMapper(this);
			WF.Cursor.PositionProvider = () =>
			{
				if(X11Pointer.GetPosition() is (int x, int y)) return new System.Drawing.Point(x, y);
				PixelPoint p = viewport.PointToScreen(lastpointer);
				return new System.Drawing.Point(p.X, p.Y);
			};
			WF.Cursor.PositionSetter = p => X11Pointer.SetPosition(p.X, p.Y);

			input.PointerEntered += (s, e) => { Track(e); display.RaiseMouseEnter(); };
			input.PointerExited += (s, e) => { Track(e); display.RaiseMouseLeave(); };
			input.PointerMoved += (s, e) => display.RaiseMouseMove(MouseArgs(e, 0, 0));

			input.PointerPressed += (s, e) =>
			{
				input.Focus();
				e.Pointer.Capture(input);
				WF.MouseEventArgs args = MouseArgs(e, e.ClickCount, 0, KeyMapping.ToMouseButton(e.GetCurrentPoint(viewport).Properties.PointerUpdateKind.GetMouseButton()));
				Trace("down", args);
				display.RaiseMouseDown(args);
				if(e.ClickCount == 2) display.RaiseMouseDoubleClick(args);
				e.Handled = true;
			};

			input.PointerReleased += (s, e) =>
			{
				WF.MouseEventArgs args = MouseArgs(e, 1, 0, KeyMapping.ToMouseButton(e.InitialPressMouseButton));
				Trace("up", args);
				display.RaiseMouseUp(args);
				if(new Rect(viewport.Bounds.Size).Contains(e.GetPosition(viewport))) display.RaiseMouseClick(args);
				e.Pointer.Capture(null);
				e.Handled = true;
			};

			// Upstream handles the wheel on the form (MainForm.OnMouseWheel), one notch = 120.
			input.PointerWheelChanged += (s, e) =>
			{
				Track(e);
				int delta = (int)Math.Round(e.Delta.Y * 120);
				WF.MouseEventArgs wheel = MouseArgs(e, 0, delta);
				Trace("wheel", wheel);
				if(delta != 0) form.RaiseMouseWheel(wheel);
				e.Handled = true;
			};

			// Keys go to MainForm (it overrides WinForms' Tab/arrow focus handling), unless
			// a text field has focus.
			AddHandler(KeyDownEvent, (s, e) =>
			{
				modifiers = KeyMapping.ToModifiers(e.KeyModifiers);
				if(IsTextInput(e.Source)) return;
				WF.Keys keys = KeyMapping.ToKeys(e.Key, e.KeyModifiers);
				if((keys & WF.Keys.KeyCode) == WF.Keys.None) return;
				display.RaisePreviewKeyDown(new WF.PreviewKeyDownEventArgs(keys));
				form.RaiseKeyDown(new WF.KeyEventArgs(keys));
				e.Handled = true;
			}, Avalonia.Interactivity.RoutingStrategies.Tunnel);

			AddHandler(KeyUpEvent, (s, e) =>
			{
				modifiers = KeyMapping.ToModifiers(e.KeyModifiers);
				if(IsTextInput(e.Source)) return;
				WF.Keys keys = KeyMapping.ToKeys(e.Key, e.KeyModifiers);
				if((keys & WF.Keys.KeyCode) == WF.Keys.None) return;
				WF.KeyEventArgs args = new WF.KeyEventArgs(keys);
				form.RaiseKeyUp(args);
				if(!args.Handled) display.RaiseKeyUp(args);
				e.Handled = true;
			}, Avalonia.Interactivity.RoutingStrategies.Tunnel);
		}

		private static bool IsTextInput(object source) =>
			source is TextBox || source is NumericUpDown || source is AutoCompleteBox || (source as Visual)?.FindAncestorOfType<TextBox>() != null;

		private void Track(PointerEventArgs e)
		{
			lastpointer = e.GetPosition(viewport);
			modifiers = KeyMapping.ToModifiers(e.KeyModifiers);
		}

		// The renderer works in device pixels, so mouse positions are scaled to match.
		private WF.MouseEventArgs MouseArgs(PointerEventArgs e, int clicks, int delta, WF.MouseButtons? button = null)
		{
			Track(e);
			PointerPoint p = e.GetCurrentPoint(viewport);
			double scale = RenderScaling;
			WF.MouseButtons buttons = button ?? Buttons(p.Properties);
			return new WF.MouseEventArgs(buttons, clicks, (int)(p.Position.X * scale), (int)(p.Position.Y * scale), delta);
		}

		private static WF.MouseButtons Buttons(PointerPointProperties p)
		{
			WF.MouseButtons b = WF.MouseButtons.None;
			if(p.IsLeftButtonPressed) b |= WF.MouseButtons.Left;
			if(p.IsRightButtonPressed) b |= WF.MouseButtons.Right;
			if(p.IsMiddleButtonPressed) b |= WF.MouseButtons.Middle;
			if(p.IsXButton1Pressed) b |= WF.MouseButtons.XButton1;
			if(p.IsXButton2Pressed) b |= WF.MouseButtons.XButton2;
			return b;
		}

		#endregion

		#region ================== Lifecycle

		private void HookLifecycle()
		{
			// Upstream opens command-line maps on Shown, which needs the renderer's OpenGL
			// context: wait for it first.
			Opened += async (s, e) =>
			{
				try { await ViewportContext.InitializeAsync(viewport); }
				catch(Exception) { } // Reported by the viewport; the editor runs without a map view
				form.RaiseShown();
				input.Focus();
			};
			Activated += (s, e) => form.RaiseActivated();
			Deactivated += (s, e) => form.RaiseDeactivate();

			// Same as closing a WinForms form: upstream's OnFormClosing may cancel (unsaved map)
			// or end the program (General.Terminate).
			Closing += (s, e) =>
			{
				if(closing) return;
				if(!form.RequestClose(WF.CloseReason.UserClosing)) e.Cancel = true;
				else closing = true;
			};

			Resized += (s, e) => SyncFormGeometry();
			PositionChanged += (s, e) => SyncFormGeometry();
			PropertyChanged += (s, e) => { if(e.Property == WindowStateProperty) SyncFormGeometry(); };
		}

		/// <summary>Runs before the window is shown, as WinForms raises Load.</summary>
		public void Load()
		{
			form.RaiseLoad();
			if(form.WindowState == WF.FormWindowState.Maximized) WindowState = WindowState.Maximized;
		}

		private void SyncFormGeometry()
		{
			form.Location = new System.Drawing.Point(Position.X, Position.Y);
			form.Size = new System.Drawing.Size((int)(ClientSize.Width * RenderScaling), (int)(ClientSize.Height * RenderScaling));
			form.WindowState = WindowState switch
			{
				WindowState.Maximized => WF.FormWindowState.Maximized,
				WindowState.Minimized => WF.FormWindowState.Minimized,
				_ => WF.FormWindowState.Normal,
			};
		}

		#endregion

		/// <summary>Maps a visual's device-pixel client coordinates to screen pixels and back.</summary>
		private sealed class VisualMapper : WF.IScreenMapper
		{
			private readonly Visual visual;
			public VisualMapper(Visual visual) { this.visual = visual; }

			private double Scale => TopLevel.GetTopLevel(visual)?.RenderScaling ?? 1.0;

			public System.Drawing.Point ToScreen(System.Drawing.Point client)
			{
				PixelPoint p = visual.PointToScreen(new Point(client.X / Scale, client.Y / Scale));
				return new System.Drawing.Point(p.X, p.Y);
			}

			public System.Drawing.Point ToClient(System.Drawing.Point screen)
			{
				Point p = visual.PointToClient(new PixelPoint(screen.X, screen.Y));
				return new System.Drawing.Point((int)Math.Round(p.X * Scale), (int)Math.Round(p.Y * Scale));
			}
		}
	}
}
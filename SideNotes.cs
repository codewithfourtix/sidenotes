using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace SideNotes
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            app.Run(new MainWindow());
        }
    }

    public class TodoItem
    {
        public string Text;
        public bool Done;
        public DateTime Created;
        public DateTime Completed;
    }

    public class MainWindow : Window
    {
        const double PanelWidth = 290;
        const double StripeWidth = 10;
        const double TotalWidth = PanelWidth + StripeWidth;

        static SolidColorBrush Hex(string s)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(s));
        }

        static readonly Brush PanelBg = Hex("#16161C");
        static readonly Brush PanelEdge = Hex("#2A2A33");
        static readonly Brush Accent = Hex("#E8A33D");
        static readonly Brush TextMain = Hex("#ECECF1");
        static readonly Brush TextMuted = Hex("#8B8B93");
        static readonly Brush TextFaint = Hex("#55555E");
        static readonly Brush InputBg = Hex("#1E1E26");
        static readonly Brush RowHover = Hex("#1F1F28");
        static readonly Brush CheckEdge = Hex("#4A4A55");

        readonly List<TodoItem> _items = new List<TodoItem>();

        bool _dockedRight = true;
        bool _expanded = false;
        bool _dialogOpen = false;

        Grid _root;
        Border _panel;
        Border _stripe;
        TranslateTransform _panelShift;
        TextBox _input;
        TextBlock _inputHint;
        TextBlock _countLabel;
        TextBlock _pendingHeader;
        TextBlock _doneHeader;
        StackPanel _pendingPanel;
        StackPanel _donePanel;
        StackPanel _doneSection;

        public MainWindow()
        {
            Title = "SideNotes";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            FontFamily = new FontFamily("Segoe UI");

            Rect wa = SystemParameters.WorkArea;
            Width = TotalWidth;
            Height = wa.Height;
            Top = wa.Top;

            _root = new Grid();
            Content = _root;

            BuildPanel();
            BuildStripe();
            ApplyDockSide();
            Load();
            Rebuild();

            Deactivated += delegate { if (!_dialogOpen) Collapse(); };
            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) Collapse();
            };
        }

        // ---------- UI construction ----------

        void BuildPanel()
        {
            _panelShift = new TranslateTransform(TotalWidth, 0);

            _panel = new Border();
            _panel.Width = PanelWidth;
            _panel.Margin = new Thickness(0, 10, 0, 10);
            _panel.Background = PanelBg;
            _panel.BorderBrush = PanelEdge;
            _panel.BorderThickness = new Thickness(1);
            _panel.CornerRadius = new CornerRadius(12);
            _panel.RenderTransform = _panelShift;

            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 18;
            shadow.ShadowDepth = 0;
            shadow.Opacity = 0.45;
            shadow.Color = Colors.Black;
            _panel.Effect = shadow;

            Grid layout = new Grid();
            layout.Margin = new Thickness(16, 14, 16, 12);
            layout.RowDefinitions.Add(RowAuto());
            layout.RowDefinitions.Add(RowAuto());
            layout.RowDefinitions.Add(RowStar());
            layout.RowDefinitions.Add(RowAuto());
            _panel.Child = layout;

            // Header: title + pending count
            Grid header = new Grid();
            header.Margin = new Thickness(2, 0, 2, 12);
            TextBlock title = new TextBlock();
            title.Text = "SideNotes";
            title.FontSize = 15;
            title.FontWeight = FontWeights.SemiBold;
            title.Foreground = TextMain;
            header.Children.Add(title);
            _countLabel = new TextBlock();
            _countLabel.FontSize = 12;
            _countLabel.Foreground = TextMuted;
            _countLabel.HorizontalAlignment = HorizontalAlignment.Right;
            _countLabel.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(_countLabel);
            Grid.SetRow(header, 0);
            layout.Children.Add(header);

            // Input box with placeholder
            Border inputWrap = new Border();
            inputWrap.Background = InputBg;
            inputWrap.BorderBrush = PanelEdge;
            inputWrap.BorderThickness = new Thickness(1);
            inputWrap.CornerRadius = new CornerRadius(8);
            inputWrap.Padding = new Thickness(10, 7, 10, 7);
            inputWrap.Margin = new Thickness(0, 0, 0, 14);

            Grid inputGrid = new Grid();
            _input = new TextBox();
            _input.Background = Brushes.Transparent;
            _input.BorderThickness = new Thickness(0);
            _input.Foreground = TextMain;
            _input.CaretBrush = TextMain;
            _input.FontSize = 13;
            _input.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) AddTodo();
            };
            _input.TextChanged += delegate { UpdateHint(); };
            _inputHint = new TextBlock();
            _inputHint.Text = "Add a task, press Enter";
            _inputHint.FontSize = 13;
            _inputHint.Foreground = TextFaint;
            _inputHint.IsHitTestVisible = false;
            _inputHint.VerticalAlignment = VerticalAlignment.Center;
            inputGrid.Children.Add(_input);
            inputGrid.Children.Add(_inputHint);
            inputWrap.Child = inputGrid;
            Grid.SetRow(inputWrap, 1);
            layout.Children.Add(inputWrap);

            // Scrollable lists
            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            StackPanel lists = new StackPanel();

            _pendingHeader = SectionHeader("PENDING");
            lists.Children.Add(_pendingHeader);
            _pendingPanel = new StackPanel();
            lists.Children.Add(_pendingPanel);

            _doneSection = new StackPanel();
            _doneSection.Margin = new Thickness(0, 14, 0, 0);
            _doneHeader = SectionHeader("DONE");
            _doneSection.Children.Add(_doneHeader);
            _donePanel = new StackPanel();
            _doneSection.Children.Add(_donePanel);
            lists.Children.Add(_doneSection);

            scroll.Content = lists;
            Grid.SetRow(scroll, 2);
            layout.Children.Add(scroll);

            // Footer: export menu
            Grid footer = new Grid();
            footer.Margin = new Thickness(2, 12, 2, 0);
            TextBlock export = new TextBlock();
            export.Text = "Export ▾";
            export.FontSize = 12;
            export.Foreground = TextMuted;
            export.Cursor = Cursors.Hand;
            export.MouseEnter += delegate { export.Foreground = TextMain; };
            export.MouseLeave += delegate { export.Foreground = TextMuted; };

            ContextMenu menu = new ContextMenu();
            menu.PlacementTarget = export;
            menu.Items.Add(MenuItemFor("Export all", "all"));
            menu.Items.Add(MenuItemFor("Export pending", "pending"));
            menu.Items.Add(MenuItemFor("Export done", "done"));
            export.MouseLeftButtonUp += delegate { menu.IsOpen = true; };

            footer.Children.Add(export);
            Grid.SetRow(footer, 3);
            layout.Children.Add(footer);

            _root.Children.Add(_panel);
        }

        MenuItem MenuItemFor(string label, string mode)
        {
            MenuItem mi = new MenuItem();
            mi.Header = label;
            string m = mode;
            mi.Click += delegate { Export(m); };
            return mi;
        }

        static RowDefinition RowAuto()
        {
            RowDefinition r = new RowDefinition();
            r.Height = GridLength.Auto;
            return r;
        }

        static RowDefinition RowStar()
        {
            RowDefinition r = new RowDefinition();
            r.Height = new GridLength(1, GridUnitType.Star);
            return r;
        }

        static TextBlock SectionHeader(string text)
        {
            TextBlock t = new TextBlock();
            t.Text = text;
            t.FontSize = 10.5;
            t.FontWeight = FontWeights.SemiBold;
            t.Foreground = TextFaint;
            t.Margin = new Thickness(2, 0, 0, 6);
            return t;
        }

        void BuildStripe()
        {
            _stripe = new Border();
            _stripe.Width = StripeWidth - 2;
            _stripe.Height = 150;
            _stripe.VerticalAlignment = VerticalAlignment.Center;
            _stripe.Background = Accent;
            _stripe.CornerRadius = new CornerRadius(4);
            _stripe.Cursor = Cursors.Hand;
            _stripe.Opacity = 0.85;

            _stripe.MouseEnter += delegate { _stripe.Opacity = 1.0; };
            _stripe.MouseLeave += delegate { _stripe.Opacity = 0.85; };
            _stripe.MouseLeftButtonUp += delegate { Toggle(); };

            _root.Children.Add(_stripe);
        }

        // ---------- Persistence ----------
        // One line per item: P|D <tab> createdTicks <tab> completedTicks <tab> text

        static string DataDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SideNotes"); }
        }

        static string DataFile
        {
            get { return Path.Combine(DataDir, "notes.txt"); }
        }

        void Save()
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                List<string> lines = new List<string>();
                foreach (TodoItem it in _items)
                {
                    string text = it.Text.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
                    lines.Add((it.Done ? "D" : "P") + "\t" + it.Created.Ticks + "\t" + it.Completed.Ticks + "\t" + text);
                }
                File.WriteAllLines(DataFile, lines.ToArray());
            }
            catch { }
        }

        void Load()
        {
            try
            {
                if (!File.Exists(DataFile)) return;
                foreach (string line in File.ReadAllLines(DataFile))
                {
                    string[] parts = line.Split(new char[] { '\t' }, 4);
                    if (parts.Length < 4 || parts[3].Length == 0) continue;
                    TodoItem it = new TodoItem();
                    it.Done = parts[0] == "D";
                    long created, completed;
                    long.TryParse(parts[1], out created);
                    long.TryParse(parts[2], out completed);
                    it.Created = new DateTime(created);
                    it.Completed = new DateTime(completed);
                    it.Text = parts[3];
                    _items.Add(it);
                }
            }
            catch { }
        }

        // ---------- Export ----------

        void Export(string mode)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "sidenotes-" + mode + "-" + DateTime.Now.ToString("yyyy-MM-dd") + ".md";
            dlg.Filter = "Markdown (*.md)|*.md|Text (*.txt)|*.txt";
            _dialogOpen = true;
            bool? ok = dlg.ShowDialog(this);
            _dialogOpen = false;
            if (ok != true) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# SideNotes — " + DateTime.Now.ToString("MMM d, yyyy h:mm tt"));
            sb.AppendLine();
            if (mode == "all" || mode == "pending")
            {
                sb.AppendLine("## Pending");
                sb.AppendLine();
                int n = 0;
                foreach (TodoItem it in _items)
                    if (!it.Done) { sb.AppendLine("- [ ] " + it.Text); n++; }
                if (n == 0) sb.AppendLine("_(nothing pending)_");
                sb.AppendLine();
            }
            if (mode == "all" || mode == "done")
            {
                sb.AppendLine("## Done");
                sb.AppendLine();
                int n = 0;
                foreach (TodoItem it in _items)
                    if (it.Done) { sb.AppendLine("- [x] " + it.Text + "  — " + it.Completed.ToString("MMM d")); n++; }
                if (n == 0) sb.AppendLine("_(nothing done yet)_");
                sb.AppendLine();
            }
            try { File.WriteAllText(dlg.FileName, sb.ToString()); }
            catch (Exception ex) { MessageBox.Show("Could not save: " + ex.Message, "SideNotes"); }
        }

        // ---------- Todo logic ----------

        void AddTodo()
        {
            string text = _input.Text.Trim();
            if (text.Length == 0) return;
            TodoItem it = new TodoItem();
            it.Text = text;
            it.Done = false;
            it.Created = DateTime.Now;
            _items.Insert(0, it);
            _input.Clear();
            Save();
            Rebuild();
        }

        void UpdateHint()
        {
            _inputHint.Visibility = _input.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        void Rebuild()
        {
            _pendingPanel.Children.Clear();
            _donePanel.Children.Clear();

            int pending = 0, done = 0;
            foreach (TodoItem it in _items)
            {
                if (it.Done) { done++; _donePanel.Children.Add(MakeRow(it)); }
                else { pending++; _pendingPanel.Children.Add(MakeRow(it)); }
            }

            _pendingHeader.Text = "PENDING";
            _doneHeader.Text = "DONE · " + done;
            _countLabel.Text = pending == 0 ? "all clear" : pending + " left";
            _doneSection.Visibility = done > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (pending == 0)
            {
                TextBlock empty = new TextBlock();
                empty.Text = "Nothing pending.";
                empty.FontSize = 12.5;
                empty.Foreground = TextFaint;
                empty.Margin = new Thickness(2, 2, 0, 0);
                _pendingPanel.Children.Add(empty);
            }
        }

        FrameworkElement MakeRow(TodoItem it)
        {
            TodoItem item = it;

            Border row = new Border();
            row.CornerRadius = new CornerRadius(8);
            row.Padding = new Thickness(8, 6, 8, 6);
            row.Margin = new Thickness(0, 0, 0, 2);
            row.Background = Brushes.Transparent;

            Grid g = new Grid();
            g.ColumnDefinitions.Add(ColAuto());
            g.ColumnDefinitions.Add(ColStar());
            g.ColumnDefinitions.Add(ColAuto());
            row.Child = g;

            // check box
            Border check = new Border();
            check.Width = 18;
            check.Height = 18;
            check.CornerRadius = new CornerRadius(5);
            check.BorderThickness = new Thickness(1.5);
            check.VerticalAlignment = VerticalAlignment.Top;
            check.Margin = new Thickness(0, 1, 10, 0);
            check.Cursor = Cursors.Hand;
            check.Background = item.Done ? Accent : Brushes.Transparent;
            check.BorderBrush = item.Done ? Accent : CheckEdge;
            TextBlock tick = new TextBlock();
            tick.Text = "✓";
            tick.FontSize = 11;
            tick.FontWeight = FontWeights.Bold;
            tick.Foreground = Hex("#16161C");
            tick.HorizontalAlignment = HorizontalAlignment.Center;
            tick.VerticalAlignment = VerticalAlignment.Center;
            tick.Visibility = item.Done ? Visibility.Visible : Visibility.Collapsed;
            check.Child = tick;
            check.MouseLeftButtonUp += delegate
            {
                item.Done = !item.Done;
                item.Completed = item.Done ? DateTime.Now : DateTime.MinValue;
                Save();
                Rebuild();
            };
            Grid.SetColumn(check, 0);
            g.Children.Add(check);

            // text
            TextBlock txt = new TextBlock();
            txt.Text = item.Text;
            txt.FontSize = 13;
            txt.TextWrapping = TextWrapping.Wrap;
            txt.Foreground = item.Done ? TextMuted : TextMain;
            if (item.Done) txt.TextDecorations = TextDecorations.Strikethrough;
            Grid.SetColumn(txt, 1);
            g.Children.Add(txt);

            // delete (shows on hover)
            TextBlock del = new TextBlock();
            del.Text = "✕";
            del.FontSize = 11;
            del.Foreground = TextFaint;
            del.Margin = new Thickness(8, 2, 0, 0);
            del.VerticalAlignment = VerticalAlignment.Top;
            del.Cursor = Cursors.Hand;
            del.Visibility = Visibility.Hidden;
            del.MouseLeftButtonUp += delegate
            {
                _items.Remove(item);
                Save();
                Rebuild();
            };
            Grid.SetColumn(del, 2);
            g.Children.Add(del);

            row.MouseEnter += delegate
            {
                row.Background = RowHover;
                del.Visibility = Visibility.Visible;
            };
            row.MouseLeave += delegate
            {
                row.Background = Brushes.Transparent;
                del.Visibility = Visibility.Hidden;
            };

            return row;
        }

        static ColumnDefinition ColAuto()
        {
            ColumnDefinition c = new ColumnDefinition();
            c.Width = GridLength.Auto;
            return c;
        }

        static ColumnDefinition ColStar()
        {
            ColumnDefinition c = new ColumnDefinition();
            c.Width = new GridLength(1, GridUnitType.Star);
            return c;
        }

        // ---------- Docking / slide ----------

        void ApplyDockSide()
        {
            Rect wa = SystemParameters.WorkArea;
            if (_dockedRight)
            {
                Left = wa.Right - TotalWidth;
                _panel.HorizontalAlignment = HorizontalAlignment.Left;
                _stripe.HorizontalAlignment = HorizontalAlignment.Right;
                _stripe.Margin = new Thickness(0, 0, 2, 0);
            }
            else
            {
                Left = wa.Left;
                _panel.HorizontalAlignment = HorizontalAlignment.Right;
                _stripe.HorizontalAlignment = HorizontalAlignment.Left;
                _stripe.Margin = new Thickness(2, 0, 0, 0);
            }
            SetShiftInstant(_expanded ? 0 : HiddenX());
        }

        double HiddenX()
        {
            return _dockedRight ? TotalWidth : -TotalWidth;
        }

        // Kills any running animation so X can be set directly.
        void SetShiftInstant(double x)
        {
            _panelShift.BeginAnimation(TranslateTransform.XProperty, null);
            _panelShift.X = x;
        }

        void SlideTo(double x)
        {
            DoubleAnimation a = new DoubleAnimation(x, TimeSpan.FromMilliseconds(240));
            CubicEase ease = new CubicEase();
            ease.EasingMode = EasingMode.EaseOut;
            a.EasingFunction = ease;
            _panelShift.BeginAnimation(TranslateTransform.XProperty, a);
        }

        void Toggle()
        {
            if (_expanded) Collapse(); else Expand();
        }

        void Expand()
        {
            _expanded = true;
            SlideTo(0);
            Activate();
            _input.Focus();
        }

        void Collapse()
        {
            if (!_expanded) return;
            _expanded = false;
            SlideTo(HiddenX());
        }
    }
}

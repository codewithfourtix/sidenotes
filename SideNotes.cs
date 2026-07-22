using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        bool _dockedRight = true;
        bool _expanded = false;

        Grid _root;
        Border _panel;
        Border _stripe;
        TranslateTransform _panelShift;

        public MainWindow()
        {
            Title = "SideNotes";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;

            Rect wa = SystemParameters.WorkArea;
            Width = TotalWidth;
            Height = wa.Height;
            Top = wa.Top;

            _root = new Grid();
            Content = _root;

            BuildPanel();
            BuildStripe();
            ApplyDockSide();

            Deactivated += delegate { Collapse(); };
            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) Collapse();
            };
        }

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

            _root.Children.Add(_panel);
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

        // Positions panel + stripe for the current dock side.
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
            _panelShift.X = _expanded ? 0 : HiddenX();
        }

        double HiddenX()
        {
            return _dockedRight ? TotalWidth : -TotalWidth;
        }

        void Toggle()
        {
            if (_expanded) Collapse(); else Expand();
        }

        void Expand()
        {
            _expanded = true;
            _panelShift.X = 0;
            Activate();
        }

        void Collapse()
        {
            if (!_expanded) return;
            _expanded = false;
            _panelShift.X = HiddenX();
        }
    }
}

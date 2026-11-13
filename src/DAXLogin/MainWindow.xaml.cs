using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DAXLogin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            VersionText.Text = "v" + UpdateService.CurrentVersion;
            Refresh();
        }

        public void Refresh()
        {
            var q = Search.Text.Trim();
            var items = Store.Profiles
                .Where(p => q.Length == 0
                    || p.Name.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || p.Account.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            List.ItemsSource = items;
            Empty.Visibility = Store.Profiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            NoMatch.Visibility = Store.Profiles.Count > 0 && items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        void SetStatus(string? text)
        {
            StatusText.Text = text ?? "";
            StatusText.Visibility = text == null ? Visibility.Collapsed : Visibility.Visible;
            HintText.Visibility = text == null ? Visibility.Visible : Visibility.Collapsed;
        }

        void Launch(Profile p)
        {
            SetStatus(null);
            if (App.I.LaunchProfile(p)) SetStatus(Loc.F("Opened", p.Account));
        }

        public void EditProfile(Profile? p)
        {
            var win = new ProfileWindow(p) { Owner = this };
            if (win.ShowDialog() == true) Refresh();
        }

        public void OpenSettings()
        {
            new SettingsWindow { Owner = this }.ShowDialog();
            Refresh();
        }

        static Profile ProfileOf(object sender) => (Profile)((FrameworkElement)sender).DataContext;

        void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchHint == null) return; // fires during InitializeComponent
            SearchHint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            Refresh();
        }

        void Add_Click(object sender, RoutedEventArgs e) => EditProfile(null);
        void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
        void Open_Click(object sender, RoutedEventArgs e) => Launch(ProfileOf(sender));

        void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Ignore double-clicks that land on the row's own buttons.
            for (var d = e.OriginalSource as DependencyObject; d != null && !(d is ListBoxItem);
                 d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d))
            {
                if (d is ButtonBase) return;
            }
            if (((ListBoxItem)sender).DataContext is Profile p) Launch(p);
        }

        void List_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && List.SelectedItem is Profile p)
            {
                Launch(p);
                e.Handled = true;
            }
        }

        void More_Click(object sender, RoutedEventArgs e)
        {
            var p = ProfileOf(sender);
            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Bottom };
            Add(menu, Loc.S("Edit"), () => EditProfile(p));
            Add(menu, Loc.S(p.IsDefault ? "ClearDefault" : "SetDefault"), () =>
            {
                Store.SetDefault(p.IsDefault ? null : p);
                Store.SaveProfiles();
                Refresh();
            });
            menu.Items.Add(new Separator());
            Add(menu, Loc.S("Delete"), () =>
            {
                if (!MessageWindow.Show(this, Loc.F("DeleteConfirm", p.Name), MsgKind.Info,
                        Loc.S("Delete"), Loc.S("Cancel"))) return;
                Store.Profiles.Remove(p);
                Store.SaveProfiles();
                Refresh();
            });
            menu.IsOpen = true;
        }

        void Share_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Bottom };
            var export = Add(menu, Loc.S("Export"), () =>
            {
                new ShareWindow { Owner = this }.ShowDialog();
                Refresh();
            });
            export.IsEnabled = Store.Profiles.Count > 0;
            Add(menu, Loc.S("Import"), () =>
            {
                ShareWindow.ImportInteractive(this);
                Refresh();
            });
            menu.IsOpen = true;
        }

        static MenuItem Add(ContextMenu menu, string header, Action action)
        {
            var item = new MenuItem { Header = header };
            item.Click += (s, e) => action();
            menu.Items.Add(item);
            return item;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Store.Settings.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                App.I.Balloon(Loc.S("TrayRunning"));
            }
            else
            {
                App.I.ExitApp();
            }
            base.OnClosing(e);
        }
    }
}

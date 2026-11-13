using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DAXLogin
{
    public enum MsgKind { Info, Success, Error }

    /// <summary>Small message dialog in the app's style. Returns true when the primary button was chosen.</summary>
    public static class MessageWindow
    {
        public static bool Show(Window? owner, string message, MsgKind kind = MsgKind.Info,
            string? primary = null, string? secondary = null)
        {
            var win = Dialogs.NewWindow(owner, "DAXLogin", 380);
            var root = new StackPanel { Margin = new Thickness(16) };

            var (bg, fg) = kind switch
            {
                MsgKind.Error => ("DangerBg", "DangerText"),
                MsgKind.Success => ("SuccessBg", "SuccessText"),
                _ => ("AccentSoft", "Text"),
            };
            root.Children.Add(new Border
            {
                Background = (Brush)Application.Current.FindResource(bg),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Child = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.FindResource(fg),
                },
            });

            bool result = false;
            var buttons = Dialogs.ButtonRow();
            var close = Dialogs.Button(secondary ?? Loc.S("Close"), false);
            close.IsCancel = true;
            close.IsDefault = primary == null;
            buttons.Children.Add(close);
            if (primary != null)
            {
                var ok = Dialogs.Button(primary, true);
                ok.IsDefault = true;
                ok.Click += (s, e) => { result = true; win.Close(); };
                buttons.Children.Add(ok);
            }
            close.Click += (s, e) => win.Close();
            root.Children.Add(buttons);

            win.Content = root;
            win.ShowDialog();
            return result;
        }
    }

    /// <summary>Asks for a secret value. Returns null when cancelled.</summary>
    public static class PromptWindow
    {
        public static string? AskPassword(Window? owner, string label)
        {
            var win = Dialogs.NewWindow(owner, "DAXLogin", 340);
            var root = new StackPanel { Margin = new Thickness(16) };
            root.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 6) });
            var box = new PasswordBox();
            root.Children.Add(box);

            string? result = null;
            var buttons = Dialogs.ButtonRow();
            var cancel = Dialogs.Button(Loc.S("Cancel"), false);
            cancel.IsCancel = true;
            var ok = Dialogs.Button(Loc.S("OK"), true);
            ok.IsDefault = true;
            ok.Click += (s, e) => { result = box.Password; win.Close(); };
            buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            root.Children.Add(buttons);

            win.Content = root;
            win.Loaded += (s, e) => box.Focus();
            win.ShowDialog();
            return result;
        }
    }

    static class Dialogs
    {
        public static Window NewWindow(Window? owner, string title, double width)
        {
            var win = new Window
            {
                Title = title,
                Width = width,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = owner == null,
                Style = (Style)Application.Current.FindResource("Win"),
            };
            if (owner != null && owner.IsVisible)
            {
                win.Owner = owner;
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                win.Topmost = true;
            }
            return win;
        }

        public static StackPanel ButtonRow() => new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
        };

        public static Button Button(string text, bool primary) => new Button
        {
            Content = text,
            Margin = new Thickness(8, 0, 0, 0),
            Style = (Style)Application.Current.FindResource(primary ? "BtnPrimary" : "Btn"),
        };
    }
}

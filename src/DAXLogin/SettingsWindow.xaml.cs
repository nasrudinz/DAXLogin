using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DAXLogin
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            var s = Store.Settings;
            AxBox.Text = s.Ax32Path;
            AutoLaunchBox.IsChecked = s.AutoLaunchDefault;
            TrayBox.IsChecked = s.MinimizeToTray;
            StartupBox.IsChecked = s.StartWithWindows;
            UpdateBox.IsChecked = s.AutoCheckUpdates;
            LangBox.SelectedIndex = s.Language == "en" ? 1 : 0;
        }

        void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Ax32.exe|Ax32.exe|Programs (*.exe)|*.exe" };
            if (dlg.ShowDialog(this) == true) AxBox.Text = dlg.FileName;
        }

        void ShowMsg(string text)
        {
            Msg.Text = text;
            Msg.Visibility = Visibility.Visible;
        }

        async void Check_Click(object sender, RoutedEventArgs e)
        {
            if (!UpdateService.IsInstalled)
            {
                ShowMsg(Loc.S("NotInstalled"));
                return;
            }
            CheckBtn.IsEnabled = false;
            ShowMsg(Loc.S("Checking"));
            try
            {
                var info = await UpdateService.CheckAsync();
                if (info == null)
                {
                    ShowMsg(Loc.F("UpToDate", UpdateService.CurrentVersion));
                }
                else
                {
                    Msg.Visibility = Visibility.Collapsed;
                    new UpdateWindow(info).ShowFor(this);
                }
            }
            catch (Exception ex)
            {
                ShowMsg(Loc.F("UpdateFailed", ex.Message));
            }
            finally
            {
                CheckBtn.IsEnabled = true;
            }
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
            var s = Store.Settings;
            s.Ax32Path = AxBox.Text.Trim().Trim('"');
            s.AutoLaunchDefault = AutoLaunchBox.IsChecked == true;
            s.MinimizeToTray = TrayBox.IsChecked == true;
            s.StartWithWindows = StartupBox.IsChecked == true;
            s.AutoCheckUpdates = UpdateBox.IsChecked == true;
            s.Language = (string)((ComboBoxItem)LangBox.SelectedItem).Tag;
            Store.SaveSettings();

            try { Store.ApplyStartWithWindows(s.StartWithWindows); }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                MessageWindow.Show(this, ex.Message, MsgKind.Error);
            }

            Loc.Instance.Language = s.Language;
            App.I.ApplySettings();
            DialogResult = true;
        }
    }
}

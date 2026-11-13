using System;
using System.Windows;
using Velopack;

namespace DAXLogin
{
    public partial class UpdateWindow : Window
    {
        readonly UpdateInfo _info;

        public UpdateWindow(UpdateInfo info)
        {
            InitializeComponent();
            _info = info;
            Headline.Text = Loc.F("UpdateReady", info.TargetFullRelease.Version);
            Current.Text = Loc.F("CurrentVersion", UpdateService.CurrentVersion);
            var notes = info.TargetFullRelease.NotesMarkdown;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                Notes.Text = notes.Trim();
                NotesPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>Shows modally over the owner when it is on screen, otherwise as a standalone window.</summary>
        public void ShowFor(Window? owner)
        {
            if (owner != null && owner.IsVisible)
            {
                Owner = owner;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ShowDialog();
            }
            else
            {
                Show();
                Activate();
            }
        }

        void Later_Click(object sender, RoutedEventArgs e) => Close();

        async void Install_Click(object sender, RoutedEventArgs e)
        {
            InstallBtn.IsEnabled = false;
            LaterBtn.IsEnabled = false;
            Msg.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressText.Text = Loc.F("Downloading", 0);
            try
            {
                await UpdateService.DownloadAsync(_info, pct => Dispatcher.BeginInvoke(new Action(() =>
                {
                    Bar.Value = pct;
                    ProgressText.Text = Loc.F("Downloading", pct);
                })));
                UpdateService.ApplyAndRestart(_info);
            }
            catch (Exception ex)
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                Msg.Text = Loc.F("DownloadFailed", ex.Message);
                Msg.Visibility = Visibility.Visible;
                InstallBtn.IsEnabled = true;
                LaterBtn.IsEnabled = true;
            }
        }
    }
}

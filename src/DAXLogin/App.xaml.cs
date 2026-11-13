using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace DAXLogin
{
    public partial class App : Application
    {
        public static App I => (App)Current;

        MainWindow _main = null!;
        WinForms.NotifyIcon? _tray;
        EventWaitHandle? _activate;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Store.Load();
            Loc.Instance.Language = Store.Settings.Language;

            _main = new MainWindow();
            CreateTray();
            StartActivationListener();

            bool trayStart = e.Args.Contains("--tray") && Store.Settings.MinimizeToTray;
            bool skipAuto = (WinForms.Control.ModifierKeys & WinForms.Keys.Shift) != 0;

            if (trayStart)
            {
                // started with Windows: stay in the tray
            }
            else if (!skipAuto && TryAutoLaunch())
            {
                if (!Store.Settings.MinimizeToTray) { ExitApp(); return; }
            }
            else
            {
                ShowMain();
            }

            if (Store.Settings.AutoCheckUpdates) _ = CheckUpdatesQuietlyAsync();
        }

        /// <summary>Opens the default profile when that option is on. False when nothing was launched.</summary>
        bool TryAutoLaunch()
        {
            var def = Store.Default;
            if (!Store.Settings.AutoLaunchDefault || def == null) return false;
            if (!LaunchProfile(def)) return true; // the error dialog already brought up the main window
            Balloon(Loc.F("Opened", def.Account));
            return true;
        }

        /// <summary>Starts AX as the profile's user. On failure shows what went wrong and offers the fix.</summary>
        public bool LaunchProfile(Profile p)
        {
            LaunchException error;
            try
            {
                if (string.IsNullOrEmpty(p.EncPassword)) throw new LaunchException("Err_NoPassword", true);
                AxLauncher.Launch(p.Domain, p.User, Store.Unprotect(p.EncPassword),
                    Store.Settings.Ax32Path, p.AxcPath);
                return true;
            }
            catch (LaunchException ex) { error = ex; }
            catch (CryptographicException) { error = new LaunchException("Err_Decrypt", true); }

            ShowMain();
            bool pathProblem = error.Key == "Err_AxNotFound";
            string? action = error.IsCredential ? Loc.S("EditPassword") : pathProblem ? Loc.S("OpenSettings") : null;
            if (MessageWindow.Show(_main, Loc.F(error.Key, p.Account, error.Detail), MsgKind.Error, action))
            {
                if (error.IsCredential) _main.EditProfile(p);
                else _main.OpenSettings();
            }
            return false;
        }

        public void ShowMain()
        {
            _main.Refresh();
            if (!_main.IsVisible) _main.Show();
            if (_main.WindowState == WindowState.Minimized) _main.WindowState = WindowState.Normal;
            _main.Activate();
        }

        public void ApplySettings()
        {
            if (_tray != null) _tray.Visible = Store.Settings.MinimizeToTray;
        }

        public void ExitApp()
        {
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _tray = null;
            }
            Shutdown();
        }

        public void Balloon(string text)
        {
            if (_tray != null && _tray.Visible)
                _tray.ShowBalloonTip(3000, "DAXLogin", text, WinForms.ToolTipIcon.Info);
        }

        void CreateTray()
        {
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
            _tray = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(exe),
                Text = "DAXLogin",
                Visible = Store.Settings.MinimizeToTray,
                ContextMenuStrip = new WinForms.ContextMenuStrip(),
            };
            _tray.DoubleClick += (s, e) => ShowMain();
            _tray.ContextMenuStrip.Opening += (s, e) =>
            {
                var menu = _tray.ContextMenuStrip;
                menu.Items.Clear();
                foreach (var p in Store.Profiles.OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name))
                {
                    var profile = p;
                    var item = menu.Items.Add(p.Name + "  (" + p.Account + ")");
                    if (p.IsDefault) item.Font = new System.Drawing.Font(item.Font, System.Drawing.FontStyle.Bold);
                    item.Click += (s2, e2) =>
                    {
                        if (LaunchProfile(profile)) Balloon(Loc.F("Opened", profile.Account));
                    };
                }
                if (Store.Profiles.Count > 0) menu.Items.Add(new WinForms.ToolStripSeparator());
                menu.Items.Add(Loc.S("OpenMain")).Click += (s2, e2) => ShowMain();
                menu.Items.Add(Loc.S("Exit")).Click += (s2, e2) => ExitApp();
                e.Cancel = false;
            };
        }

        /// <summary>A second start of the app signals this event instead of opening another instance.</summary>
        void StartActivationListener()
        {
            _activate = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ActivateEventName);
            var thread = new Thread(() =>
            {
                while (_activate.WaitOne())
                    Dispatcher.BeginInvoke(new Action(() => { if (!TryAutoLaunch()) ShowMain(); }));
            })
            { IsBackground = true, Name = "activation" };
            thread.Start();
        }

        async Task CheckUpdatesQuietlyAsync()
        {
            try
            {
                if (!UpdateService.IsInstalled) return;
                var info = await UpdateService.CheckAsync();
                if (info != null) new UpdateWindow(info).ShowFor(_main);
            }
            catch (Exception)
            {
                // Background check only: offline or unreachable GitHub is not worth interrupting the user.
            }
        }
    }
}

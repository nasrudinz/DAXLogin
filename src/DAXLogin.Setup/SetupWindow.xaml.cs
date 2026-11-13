using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DAXLogin.Setup
{
    public partial class SetupWindow : Window
    {
        static readonly Dictionary<string, (string th, string en)> T = new Dictionary<string, (string, string)>
        {
            ["Tagline"] = ("เข้า Dynamics AX ได้ในคลิกเดียว", "Open Dynamics AX in one click"),
            ["Desktop"] = ("สร้าง shortcut บน Desktop", "Create a desktop shortcut"),
            ["StartMenu"] = ("เพิ่มใน Start menu", "Add to the Start menu"),
            ["Startup"] = ("เปิดพร้อม Windows", "Start with Windows"),
            ["NoAdmin"] = ("ติดตั้งเฉพาะผู้ใช้นี้ ไม่ต้องใช้สิทธิ์ admin", "Installs for this user only. No admin rights needed."),
            ["Install"] = ("ติดตั้ง", "Install"),
            ["Installing"] = ("กำลังติดตั้ง…", "Installing…"),
            ["InstallingHint"] = ("ใช้เวลาไม่กี่วินาที", "This takes a few seconds."),
            ["DoneTitle"] = ("ติดตั้งเรียบร้อย", "DAXLogin is installed"),
            ["DoneBody"] = ("เพิ่มโปรไฟล์แรกของคุณ แล้วเปิด AX ได้เลย", "Add your first profile and open AX."),
            ["Open"] = ("เปิด DAXLogin", "Open DAXLogin"),
            ["Close"] = ("ปิด", "Close"),
            ["ErrorTitle"] = ("ติดตั้งไม่สำเร็จ", "Couldn't install DAXLogin"),
            ["Retry"] = ("ลองอีกครั้ง", "Try again"),
            ["Version"] = ("เวอร์ชัน {0}", "Version {0}"),
            ["OtherLang"] = ("English", "ไทย"),
        };

        // Thai first, like the app itself; the header button switches to English.
        string _lang = "th";
        bool _busy;

        public SetupWindow()
        {
            InitializeComponent();
            ApplyLanguage();
        }

        string S(string key) => _lang == "en" ? T[key].en : T[key].th;

        void ApplyLanguage()
        {
            Tagline.Text = S("Tagline");
            DesktopBox.Content = S("Desktop");
            StartMenuBox.Content = S("StartMenu");
            StartupBox.Content = S("Startup");
            NoAdmin.Text = S("NoAdmin");
            InstallBtn.Content = S("Install");
            Installing.Text = S("Installing");
            InstallingHint.Text = S("InstallingHint");
            DoneTitle.Text = S("DoneTitle");
            DoneBody.Text = S("DoneBody");
            OpenBtn.Content = S("Open");
            DoneCloseBtn.Content = S("Close");
            ErrorTitle.Text = S("ErrorTitle");
            RetryBtn.Content = S("Retry");
            VersionText.Text = string.Format(S("Version"), Installer.Version);
            LangBtn.Content = S("OtherLang");
        }

        void Show(UIElement panel)
        {
            foreach (var p in new UIElement[] { OptionsPanel, ProgressPanel, DonePanel, ErrorPanel })
                p.Visibility = ReferenceEquals(p, panel) ? Visibility.Visible : Visibility.Collapsed;
        }

        void Hero_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        void Lang_Click(object sender, RoutedEventArgs e)
        {
            _lang = _lang == "en" ? "th" : "en";
            ApplyLanguage();
        }

        void Close_Click(object sender, RoutedEventArgs e) => Close();

        void Retry_Click(object sender, RoutedEventArgs e) => Show(OptionsPanel);

        async void Install_Click(object sender, RoutedEventArgs e)
        {
            var options = new InstallOptions
            {
                DesktopShortcut = DesktopBox.IsChecked == true,
                StartMenuShortcut = StartMenuBox.IsChecked == true,
                StartWithWindows = StartupBox.IsChecked == true,
                Language = _lang,
            };
            _busy = true;
            CloseBtn.IsEnabled = false;
            LangBtn.IsEnabled = false;
            Show(ProgressPanel);
            try
            {
                await Task.Run(() => Installer.Run(options));
                Show(DonePanel);
                OpenBtn.Focus();
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
                Show(ErrorPanel);
            }
            finally
            {
                _busy = false;
                CloseBtn.IsEnabled = true;
                LangBtn.IsEnabled = true;
            }
        }

        void Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Installer.LaunchApp();
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
                Show(ErrorPanel);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_busy) e.Cancel = true; // closing mid-install would leave a half-installed app
            base.OnClosing(e);
        }
    }
}

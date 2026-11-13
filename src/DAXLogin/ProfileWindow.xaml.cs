using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace DAXLogin
{
    public partial class ProfileWindow : Window
    {
        readonly Profile? _profile;

        public ProfileWindow(Profile? profile)
        {
            InitializeComponent();
            _profile = profile;
            Title = Loc.S(profile == null ? "AddProfile" : "EditProfile");

            if (profile != null)
            {
                NameBox.Text = profile.Name;
                DomainBox.Text = profile.Domain;
                UserBox.Text = profile.User;
                AxcBox.Text = profile.AxcPath;
                DefaultBox.IsChecked = profile.IsDefault;
                try { PwBox.Password = Store.Unprotect(profile.EncPassword); }
                catch (CryptographicException) { /* unreadable on this account: leave empty to re-enter */ }
            }
            else
            {
                DefaultBox.IsChecked = Store.Profiles.Count == 0;
            }
            Loaded += (s, e) => (profile == null ? NameBox : (UIElement)PwBox).Focus();
        }

        string Password => PwText.Visibility == Visibility.Visible ? PwText.Text : PwBox.Password;

        void Eye_Click(object sender, RoutedEventArgs e)
        {
            if (PwText.Visibility == Visibility.Visible)
            {
                PwBox.Password = PwText.Text;
                PwText.Visibility = Visibility.Collapsed;
                PwBox.Visibility = Visibility.Visible;
            }
            else
            {
                PwText.Text = PwBox.Password;
                PwBox.Visibility = Visibility.Collapsed;
                PwText.Visibility = Visibility.Visible;
            }
        }

        void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "AX config (*.axc)|*.axc|All files (*.*)|*.*" };
            if (dlg.ShowDialog(this) == true) AxcBox.Text = dlg.FileName;
        }

        void ShowMsg(string text, bool ok)
        {
            Msg.Text = text;
            Msg.Foreground = (Brush)FindResource(ok ? "SuccessText" : "DangerText");
            Msg.Visibility = Visibility.Visible;
        }

        /// <summary>Reads domain and user; "domain\user" typed into the user box is split.</summary>
        void ReadAccount(out string domain, out string user)
        {
            domain = DomainBox.Text.Trim();
            user = UserBox.Text.Trim();
            int slash = user.IndexOf('\\');
            if (slash > 0)
            {
                domain = user.Substring(0, slash);
                user = user.Substring(slash + 1);
            }
        }

        bool Validate(bool needName)
        {
            ReadAccount(out _, out var user);
            string? key = needName && NameBox.Text.Trim().Length == 0 ? "Need_Name"
                : user.Length == 0 ? "Need_User"
                : Password.Length == 0 ? "Need_Password" : null;
            if (key == null) return true;
            ShowMsg(Loc.S(key), false);
            return false;
        }

        async void Test_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate(false)) return;
            ReadAccount(out var domain, out var user);
            var password = Password;
            var account = domain.Length == 0 ? user : domain + "\\" + user;
            TestBtn.IsEnabled = false;
            Msg.Visibility = Visibility.Collapsed;
            try
            {
                await Task.Run(() => AxLauncher.TestLogin(domain, user, password));
                ShowMsg(Loc.S("TestOk"), true);
            }
            catch (LaunchException ex)
            {
                ShowMsg(Loc.F(ex.Key, account, ex.Detail), false);
            }
            finally
            {
                TestBtn.IsEnabled = true;
            }
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate(true)) return;
            ReadAccount(out var domain, out var user);

            var p = _profile ?? new Profile();
            p.Name = NameBox.Text.Trim();
            p.Domain = domain;
            p.User = user;
            p.AxcPath = AxcBox.Text.Trim().Trim('"');
            p.EncPassword = Store.Protect(Password);
            if (_profile == null) Store.Profiles.Add(p);

            if (DefaultBox.IsChecked == true) Store.SetDefault(p);
            else p.IsDefault = false;

            Store.SaveProfiles();
            DialogResult = true;
        }
    }
}

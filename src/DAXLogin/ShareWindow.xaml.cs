using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DAXLogin
{
    public partial class ShareWindow : Window
    {
        public ShareWindow()
        {
            InitializeComponent();
            foreach (var p in Store.Profiles.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
                Items.Children.Add(new CheckBox { Content = p.Name + "  (" + p.Account + ")", Tag = p, IsChecked = true });
        }

        void ShowError(string key)
        {
            Msg.Text = Loc.S(key);
            Msg.Visibility = Visibility.Visible;
        }

        void Export_Click(object sender, RoutedEventArgs e)
        {
            var selected = Items.Children.OfType<CheckBox>().Where(c => c.IsChecked == true)
                .Select(c => (Profile)c.Tag).ToList();
            if (selected.Count == 0) { ShowError("Need_Select"); return; }
            if (Pass1.Password.Length < 8) { ShowError("Need_Pass8"); return; }
            if (Pass1.Password != Pass2.Password) { ShowError("Need_PassMatch"); return; }

            var dlg = new SaveFileDialog
            {
                Filter = Loc.S("ShareFilter"),
                FileName = "DAXLogin-profiles.daxprofile",
            };
            if (dlg.ShowDialog(this) != true) return;

            try
            {
                ShareService.Export(dlg.FileName, selected, IncludePw.IsChecked == true, Pass1.Password);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is CryptographicException)
            {
                MessageWindow.Show(this, ex.Message, MsgKind.Error);
                return;
            }
            MessageWindow.Show(this, Loc.F("Exported", selected.Count), MsgKind.Success);
            DialogResult = true;
        }

        void Import_Click(object sender, RoutedEventArgs e)
        {
            if (ImportInteractive(this)) DialogResult = true;
        }

        /// <summary>Picks a .daxprofile file, asks for its passphrase and merges it. True when profiles changed.</summary>
        public static bool ImportInteractive(Window owner)
        {
            var dlg = new OpenFileDialog { Filter = Loc.S("ShareFilter") };
            if (dlg.ShowDialog(owner) != true) return false;

            var pass = PromptWindow.AskPassword(owner, Loc.S("EnterFilePass"));
            if (pass == null) return false;

            try
            {
                var incoming = ShareService.Import(dlg.FileName, pass);
                var (added, updated) = ShareService.Merge(Store.Profiles, incoming);
                Store.SaveProfiles();
                var text = Loc.F("Imported", added, updated);
                if (incoming.Any(x => x.Password == null)) text += "\n" + Loc.S("ImportedNoPass");
                MessageWindow.Show(owner, text, MsgKind.Success);
                return added + updated > 0;
            }
            catch (ShareException ex)
            {
                MessageWindow.Show(owner, Loc.S(ex.Key), MsgKind.Error);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageWindow.Show(owner, ex.Message, MsgKind.Error);
            }
            return false;
        }
    }
}

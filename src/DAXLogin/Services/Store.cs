using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace DAXLogin
{
    /// <summary>Profiles and settings on disk under %AppData%\DAXLogin.</summary>
    public static class Store
    {
        static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DAXLogin.v1.profile-password");
        static readonly JsonSerializerOptions Json = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>Data folder; the DAXLOGIN_DATA environment variable overrides it (used for testing).</summary>
        public static string Dir { get; set; } =
            Environment.GetEnvironmentVariable("DAXLOGIN_DATA") is { Length: > 0 } custom
                ? custom
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DAXLogin");

        static string ProfilesFile => Path.Combine(Dir, "profiles.json");
        static string SettingsFile => Path.Combine(Dir, "settings.json");

        public static List<Profile> Profiles { get; private set; } = new List<Profile>();
        public static AppSettings Settings { get; private set; } = new AppSettings();

        public static Profile? Default => Profiles.FirstOrDefault(p => p.IsDefault);

        public static void Load()
        {
            Profiles = Read<List<Profile>>(ProfilesFile) ?? new List<Profile>();
            Settings = Read<AppSettings>(SettingsFile) ?? new AppSettings();
        }

        public static void SaveProfiles() => Write(ProfilesFile, Profiles);
        public static void SaveSettings() => Write(SettingsFile, Settings);

        public static void SetDefault(Profile? profile)
        {
            foreach (var p in Profiles) p.IsDefault = ReferenceEquals(p, profile);
        }

        static T? Read<T>(string file) where T : class
        {
            try
            {
                return File.Exists(file) ? JsonSerializer.Deserialize<T>(File.ReadAllText(file)) : null;
            }
            catch (JsonException)
            {
                // Keep the unreadable file for inspection instead of silently overwriting it.
                try { File.Copy(file, file + ".corrupt", true); } catch (IOException) { }
                return null;
            }
        }

        static void Write<T>(string file, T value)
        {
            Directory.CreateDirectory(Dir);
            var tmp = file + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(value, Json));
            if (File.Exists(file)) File.Replace(tmp, file, null);
            else File.Move(tmp, file);
        }

        public static string Protect(string plain) =>
            Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser));

        /// <exception cref="CryptographicException">Data was encrypted by another Windows user or machine.</exception>
        public static string Unprotect(string enc)
        {
            if (string.IsNullOrEmpty(enc)) return "";
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    Convert.FromBase64String(enc), Entropy, DataProtectionScope.CurrentUser));
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Stored password is not valid.", ex);
            }
        }

        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void ApplyStartWithWindows(bool enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;
            if (enabled)
            {
                var exe = Process.GetCurrentProcess().MainModule!.FileName;
                key.SetValue("DAXLogin", "\"" + exe + "\" --tray");
            }
            else
            {
                key.DeleteValue("DAXLogin", false);
            }
        }
    }
}

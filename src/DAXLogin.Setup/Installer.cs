using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace DAXLogin.Setup
{
    public class InstallOptions
    {
        public bool DesktopShortcut { get; set; } = true;
        public bool StartMenuShortcut { get; set; } = true;
        public bool StartWithWindows { get; set; }
        public string Language { get; set; } = "th";
    }

    /// <summary>Runs the embedded Velopack installer silently, then applies the chosen options.</summary>
    public static class Installer
    {
        const string AppName = "DAXLogin";
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static string InstallDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

        public static string AppExe => Path.Combine(InstallDir, "current", AppName + ".exe");

        static string DesktopLink => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk");

        static string StartMenuLink => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk");

        public static string Version
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v.Major + "." + v.Minor + "." + v.Build;
            }
        }

        public static void Run(InstallOptions options)
        {
            CloseRunningApp();
            RunPayload();
            if (!File.Exists(AppExe))
                throw new InvalidOperationException("DAXLogin.exe was not found after installing (" + AppExe + ").");

            if (!options.DesktopShortcut) DeleteIfExists(DesktopLink);
            if (!options.StartMenuShortcut) DeleteIfExists(StartMenuLink);
            ApplyStartWithWindows(options.StartWithWindows);
            WriteSettings(options);
        }

        public static void LaunchApp() => Process.Start(new ProcessStartInfo(AppExe) { UseShellExecute = true });

        /// <summary>An installed copy that is still running would keep its files locked.</summary>
        static void CloseRunningApp()
        {
            foreach (var p in Process.GetProcessesByName(AppName))
            {
                try
                {
                    var file = p.MainModule?.FileName ?? "";
                    if (!file.StartsWith(InstallDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                    p.Kill();
                    p.WaitForExit(5000);
                }
                catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
                {
                    // Already exited, or a process we cannot inspect: not ours to close.
                }
                finally
                {
                    p.Dispose();
                }
            }
        }

        static void RunPayload()
        {
            var temp = Path.Combine(Path.GetTempPath(), "DAXLoginSetup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            var exe = Path.Combine(temp, "DAXLogin-win-Setup.exe");
            try
            {
                using (var src = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.exe")
                                 ?? throw new InvalidOperationException("Installer payload is missing."))
                using (var dst = File.Create(exe))
                {
                    src.CopyTo(dst);
                }

                var psi = new ProcessStartInfo(exe, "--silent") { UseShellExecute = false, CreateNoWindow = true };
                using var proc = Process.Start(psi)!;
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                    throw new InvalidOperationException("The installer exited with code " + proc.ExitCode + ".");
            }
            finally
            {
                try { Directory.Delete(temp, true); }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
            }
        }

        static void DeleteIfExists(string file)
        {
            if (File.Exists(file)) File.Delete(file);
        }

        static void ApplyStartWithWindows(bool enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;
            if (enabled) key.SetValue(AppName, "\"" + AppExe + "\" --tray");
            else key.DeleteValue(AppName, false);
        }

        /// <summary>Carries the installer choices into the app's settings.json, keeping any other values.</summary>
        static void WriteSettings(InstallOptions options)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
            var file = Path.Combine(dir, "settings.json");
            var json = new JavaScriptSerializer();
            Dictionary<string, object> settings;
            try
            {
                settings = File.Exists(file)
                    ? json.Deserialize<Dictionary<string, object>>(File.ReadAllText(file)) ?? new Dictionary<string, object>()
                    : new Dictionary<string, object>();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                return; // unreadable settings: leave the file for the app to deal with
            }
            settings["StartWithWindows"] = options.StartWithWindows;
            settings["Language"] = options.Language;
            Directory.CreateDirectory(dir);
            File.WriteAllText(file, json.Serialize(settings));
        }
    }
}

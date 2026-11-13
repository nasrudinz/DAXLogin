using System;
using System.Reflection;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace DAXLogin
{
    /// <summary>Updates from GitHub Releases of the public repository.</summary>
    public static class UpdateService
    {
        public const string RepoUrl = "https://github.com/nasrudinz/DAXLogin";

        static UpdateManager Create() => new UpdateManager(new GithubSource(RepoUrl, null, false));

        public static bool IsInstalled
        {
            get
            {
                try { return Create().IsInstalled; }
                catch (Exception) { return false; }
            }
        }

        public static string CurrentVersion
        {
            get
            {
                try
                {
                    var mgr = Create();
                    if (mgr.IsInstalled && mgr.CurrentVersion != null) return mgr.CurrentVersion.ToString();
                }
                catch (Exception) { }
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v.Major + "." + v.Minor + "." + v.Build;
            }
        }

        /// <summary>Returns null when already up to date.</summary>
        public static Task<UpdateInfo?> CheckAsync() => Create().CheckForUpdatesAsync();

        public static Task DownloadAsync(UpdateInfo info, Action<int> progress) =>
            Create().DownloadUpdatesAsync(info, progress);

        public static void ApplyAndRestart(UpdateInfo info) => Create().ApplyUpdatesAndRestart(info);
    }
}

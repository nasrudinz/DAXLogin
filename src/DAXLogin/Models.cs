using System;
using System.Text.Json.Serialization;

namespace DAXLogin
{
    public class Profile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";
        public string Domain { get; set; } = "";
        public string User { get; set; } = "";
        /// <summary>Password encrypted with DPAPI (current Windows user), base64.</summary>
        public string EncPassword { get; set; } = "";
        public string AxcPath { get; set; } = "";
        public bool IsDefault { get; set; }

        [JsonIgnore]
        public string Account => string.IsNullOrEmpty(Domain) ? User : Domain + "\\" + User;

        [JsonIgnore]
        public string Initials
        {
            get
            {
                var s = string.IsNullOrWhiteSpace(Name) ? User : Name.Trim();
                if (s.Length == 0) return "?";
                var parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var text = parts.Length >= 2
                    ? parts[0].Substring(0, 1) + parts[1].Substring(0, 1)
                    : s.Substring(0, Math.Min(2, s.Length));
                return text.ToUpperInvariant();
            }
        }
    }

    public class AppSettings
    {
        public const string DefaultAx32Path =
            @"C:\Program Files (x86)\Microsoft Dynamics AX\60\Client\Bin\Ax32.exe";

        public string Ax32Path { get; set; } = DefaultAx32Path;
        public bool AutoLaunchDefault { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
        public bool StartWithWindows { get; set; }
        public bool AutoCheckUpdates { get; set; } = true;
        public string Language { get; set; } = "th";
    }
}

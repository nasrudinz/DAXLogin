using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DAXLogin
{
    public class LaunchException : Exception
    {
        /// <summary>Localization key of the message.</summary>
        public string Key { get; }
        /// <summary>True when fixing the saved user name or password is the likely remedy.</summary>
        public bool IsCredential { get; }

        public LaunchException(string key, bool isCredential, string? detail = null) : base(detail ?? key)
        {
            Key = key;
            IsCredential = isCredential;
            Detail = detail;
        }

        public string? Detail { get; }
    }

    /// <summary>Starts Ax32.exe as another Windows user, the same way runas.exe does.</summary>
    public static class AxLauncher
    {
        const int LOGON_WITH_PROFILE = 1;
        const int CREATE_UNICODE_ENVIRONMENT = 0x400;
        const int LOGON32_LOGON_INTERACTIVE = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public int cb;
            public string? lpReserved, lpDesktop, lpTitle;
            public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
            public short wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess, hThread;
            public int dwProcessId, dwThreadId;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessWithLogonW(string user, string? domain, string password, int logonFlags,
            string? applicationName, StringBuilder commandLine, int creationFlags, IntPtr environment,
            string? currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInfo);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool LogonUserW(string user, string? domain, string password, int logonType,
            int logonProvider, out IntPtr token);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        static string? DomainFor(string domain, string user)
        {
            if (user.Contains("@")) return null;
            return string.IsNullOrWhiteSpace(domain) ? "." : domain;
        }

        public static void Launch(string domain, string user, string password, string ax32Path, string? axcPath)
        {
            if (!File.Exists(ax32Path)) throw new LaunchException("Err_AxNotFound", false, ax32Path);
            if (!string.IsNullOrWhiteSpace(axcPath) && !File.Exists(axcPath))
                throw new LaunchException("Err_AxcNotFound", false, axcPath);

            var cmd = new StringBuilder("\"" + ax32Path + "\"");
            if (!string.IsNullOrWhiteSpace(axcPath)) cmd.Append(" \"" + axcPath + "\"");

            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            if (!CreateProcessWithLogonW(user, DomainFor(domain, user), password, LOGON_WITH_PROFILE, null, cmd,
                    CREATE_UNICODE_ENVIRONMENT, IntPtr.Zero, Path.GetDirectoryName(ax32Path), ref si, out var pi))
            {
                throw FromWin32(Marshal.GetLastWin32Error());
            }
            CloseHandle(pi.hThread);
            CloseHandle(pi.hProcess);
        }

        /// <summary>Checks the credentials without starting AX. A wrong password counts toward account lockout.</summary>
        public static void TestLogin(string domain, string user, string password)
        {
            if (!LogonUserW(user, DomainFor(domain, user), password, LOGON32_LOGON_INTERACTIVE, 0, out var token))
                throw FromWin32(Marshal.GetLastWin32Error());
            CloseHandle(token);
        }

        static LaunchException FromWin32(int code)
        {
            switch (code)
            {
                case 1326: return new LaunchException("Err_BadPassword", true);
                case 1330:
                case 1907: return new LaunchException("Err_Expired", true);
                case 1909: return new LaunchException("Err_Locked", false);
                case 1331: return new LaunchException("Err_Disabled", false);
                case 1311:
                case 1355:
                case 1789: return new LaunchException("Err_NoDomain", false);
                case 1385: return new LaunchException("Err_LogonType", false);
                case 1058: return new LaunchException("Err_SecLogon", false);
                default:
                    return new LaunchException("Err_Generic", false,
                        new Win32Exception(code).Message + " (" + code + ")");
            }
        }
    }
}

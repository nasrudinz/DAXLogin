using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Markup;

namespace DAXLogin
{
    /// <summary>Thai/English strings. Bindings made with {s:L Key} update live when the language changes.</summary>
    public sealed class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new Loc();

        string _lang = "th";
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Language
        {
            get => _lang;
            set
            {
                _lang = value == "en" ? "en" : "th";
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            }
        }

        public string this[string key] =>
            T.TryGetValue(key, out var v) ? (_lang == "en" ? v.en : v.th) : key;

        public static string S(string key) => Instance[key];
        public static string F(string key, params object?[] args) => string.Format(Instance[key], args);

        static readonly Dictionary<string, (string th, string en)> T = new Dictionary<string, (string, string)>
        {
            // main window
            ["SearchHint"] = ("ค้นหาโปรไฟล์", "Search profiles"),
            ["AddProfile"] = ("เพิ่มโปรไฟล์", "Add profile"),
            ["Share"] = ("แชร์โปรไฟล์", "Share profiles"),
            ["Settings"] = ("ตั้งค่า", "Settings"),
            ["OpenAx"] = ("เปิด AX", "Open AX"),
            ["Default"] = ("เริ่มต้น", "Default"),
            ["More"] = ("เพิ่มเติม", "More"),
            ["Edit"] = ("แก้ไข", "Edit"),
            ["SetDefault"] = ("ตั้งเป็นโปรไฟล์เริ่มต้น", "Set as default"),
            ["ClearDefault"] = ("ยกเลิกโปรไฟล์เริ่มต้น", "Remove default"),
            ["Delete"] = ("ลบ", "Delete"),
            ["DeleteConfirm"] = ("ลบโปรไฟล์ \"{0}\" ใช่ไหม รหัสผ่านที่บันทึกไว้จะถูกลบไปด้วย", "Delete profile \"{0}\"? Its saved password is removed too."),
            ["FooterHint"] = ("ดับเบิลคลิกเพื่อเปิด AX", "Double-click to open AX"),
            ["EmptyTitle"] = ("เพิ่มโปรไฟล์แรกของคุณ", "Add your first profile"),
            ["EmptyBody"] = ("บันทึก user และรหัสผ่านไว้ แล้วเปิด Dynamics AX ได้ในคลิกเดียว", "Save a user and password, then open Dynamics AX in one click."),
            ["NoMatch"] = ("ไม่พบโปรไฟล์ที่ตรงกับคำค้น", "No profiles match your search."),
            ["Export"] = ("ส่งออกโปรไฟล์", "Export profiles"),
            ["Import"] = ("นำเข้าไฟล์", "Import file"),
            ["Opened"] = ("เปิด AX ด้วย {0} แล้ว", "Opened AX as {0}"),
            ["TrayRunning"] = ("DAXLogin ยังทำงานอยู่ที่ system tray", "DAXLogin is still running in the system tray"),

            // common
            ["Save"] = ("บันทึก", "Save"),
            ["Cancel"] = ("ยกเลิก", "Cancel"),
            ["Close"] = ("ปิด", "Close"),
            ["OK"] = ("ตกลง", "OK"),
            ["Browse"] = ("เลือกไฟล์", "Browse"),

            // profile window
            ["EditProfile"] = ("แก้ไขโปรไฟล์", "Edit profile"),
            ["DisplayName"] = ("ชื่อที่แสดง", "Display name"),
            ["Domain"] = ("Domain", "Domain"),
            ["User"] = ("User", "User"),
            ["Password"] = ("Password", "Password"),
            ["ShowPassword"] = ("แสดงรหัสผ่าน", "Show password"),
            ["AxcFile"] = ("ไฟล์ config (.axc) ไม่บังคับ", "Config file (.axc), optional"),
            ["MakeDefault"] = ("ตั้งเป็นโปรไฟล์เริ่มต้น", "Set as default profile"),
            ["TestLogin"] = ("ทดสอบ login", "Test sign-in"),
            ["TestOk"] = ("User และรหัสผ่านใช้งานได้", "User and password work."),
            ["Need_Name"] = ("ใส่ชื่อที่แสดง", "Enter a display name."),
            ["Need_User"] = ("ใส่ user", "Enter a user."),
            ["Need_Password"] = ("ใส่รหัสผ่าน", "Enter a password."),

            // settings
            ["Ax32Path"] = ("ตำแหน่ง Ax32.exe", "Ax32.exe location"),
            ["AutoLaunch"] = ("เปิดโปรไฟล์เริ่มต้นทันทีเมื่อเปิดแอป", "Open the default profile when the app starts"),
            ["AutoLaunchHint"] = ("กด Shift ค้างตอนเปิดแอปเพื่อข้าม", "Hold Shift while starting to skip."),
            ["MinToTray"] = ("ย่อไปที่ system tray เมื่อปิดหน้าต่าง", "Keep running in the system tray when the window closes"),
            ["StartWithWindows"] = ("เปิดพร้อม Windows", "Start with Windows"),
            ["AutoUpdate"] = ("ตรวจหาอัปเดตอัตโนมัติ", "Check for updates automatically"),
            ["Language"] = ("ภาษา", "Language"),
            ["CheckUpdate"] = ("ตรวจหาอัปเดต", "Check for updates"),
            ["Checking"] = ("กำลังตรวจหาอัปเดต…", "Checking for updates…"),
            ["UpToDate"] = ("คุณใช้เวอร์ชันล่าสุดแล้ว ({0})", "You're on the latest version ({0})."),
            ["NotInstalled"] = ("อัปเดตได้เฉพาะแอปที่ติดตั้งผ่านตัวติดตั้ง", "Updates work only when the app was installed with the installer."),
            ["UpdateFailed"] = ("ตรวจหาอัปเดตไม่สำเร็จ: {0}", "Couldn't check for updates: {0}"),

            // share
            ["IncludePasswords"] = ("รวมรหัสผ่านในไฟล์", "Include passwords in the file"),
            ["FilePass"] = ("รหัสป้องกันไฟล์", "File passphrase"),
            ["FilePassConfirm"] = ("ยืนยันรหัสป้องกันไฟล์", "Confirm file passphrase"),
            ["FilePassHint"] = ("ผู้รับต้องใส่รหัสนี้ตอนนำเข้าไฟล์ .daxprofile ส่งรหัสให้ผู้รับคนละช่องทางกับไฟล์", "The recipient needs this passphrase to import the .daxprofile file. Send it separately from the file."),
            ["DoExport"] = ("ส่งออก", "Export"),
            ["Need_Select"] = ("เลือกอย่างน้อยหนึ่งโปรไฟล์", "Select at least one profile."),
            ["Need_Pass8"] = ("รหัสป้องกันไฟล์ต้องยาวอย่างน้อย 8 ตัวอักษร", "The passphrase needs at least 8 characters."),
            ["Need_PassMatch"] = ("รหัสป้องกันไฟล์สองช่องไม่ตรงกัน", "The two passphrases don't match."),
            ["Exported"] = ("ส่งออก {0} โปรไฟล์แล้ว", "Exported {0} profiles."),
            ["EnterFilePass"] = ("ใส่รหัสป้องกันไฟล์", "Enter the file passphrase"),
            ["Imported"] = ("นำเข้าแล้ว: เพิ่มใหม่ {0} อัปเดต {1}", "Imported: {0} added, {1} updated."),
            ["ImportedNoPass"] = ("โปรไฟล์ที่ไม่มีรหัสผ่านต้องแก้ไขเพื่อใส่รหัสก่อนใช้งาน", "Profiles without a password need one before you can use them."),
            ["Share_BadFile"] = ("ไฟล์นี้ไม่ใช่ไฟล์ .daxprofile ที่ใช้ได้", "This isn't a valid .daxprofile file."),
            ["Share_WrongPass"] = ("รหัสป้องกันไฟล์ไม่ถูกต้อง", "That passphrase is wrong."),
            ["ShareFilter"] = ("ไฟล์โปรไฟล์ DAXLogin (*.daxprofile)|*.daxprofile", "DAXLogin profile file (*.daxprofile)|*.daxprofile"),

            // update
            ["UpdateTitle"] = ("มีเวอร์ชันใหม่", "Update available"),
            ["UpdateReady"] = ("DAXLogin {0} พร้อมให้ติดตั้ง", "DAXLogin {0} is ready to install"),
            ["CurrentVersion"] = ("เวอร์ชันปัจจุบัน {0}", "Current version {0}"),
            ["WhatsNew"] = ("มีอะไรใหม่", "What's new"),
            ["Later"] = ("ไว้ทีหลัง", "Later"),
            ["InstallRestart"] = ("ติดตั้งและเปิดใหม่", "Install and restart"),
            ["Downloading"] = ("กำลังดาวน์โหลด {0}%", "Downloading {0}%"),
            ["DownloadFailed"] = ("ดาวน์โหลดอัปเดตไม่สำเร็จ: {0}", "Couldn't download the update: {0}"),

            // tray
            ["OpenMain"] = ("เปิดหน้าต่างหลัก", "Open main window"),
            ["Exit"] = ("ออกจากโปรแกรม", "Exit"),

            // launch errors ({0} = account)
            ["EditPassword"] = ("แก้ไขรหัสผ่าน", "Edit password"),
            ["OpenSettings"] = ("เปิดตั้งค่า", "Open settings"),
            ["Err_BadPassword"] = ("User หรือรหัสผ่านของ {0} ไม่ถูกต้อง", "The user name or password for {0} is wrong."),
            ["Err_Expired"] = ("รหัสผ่านของ {0} หมดอายุหรือต้องเปลี่ยนก่อนใช้งาน", "The password for {0} has expired or must be changed."),
            ["Err_Locked"] = ("บัญชี {0} ถูกล็อก ติดต่อผู้ดูแลระบบ", "The account {0} is locked. Contact your administrator."),
            ["Err_Disabled"] = ("บัญชี {0} ถูกปิดใช้งาน ติดต่อผู้ดูแลระบบ", "The account {0} is disabled. Contact your administrator."),
            ["Err_NoDomain"] = ("ติดต่อ domain ของ {0} ไม่ได้ ตรวจการเชื่อมต่อเครือข่ายหรือ VPN", "Couldn't reach the domain for {0}. Check your network or VPN."),
            ["Err_LogonType"] = ("บัญชี {0} ไม่ได้รับสิทธิ์ให้ login บนเครื่องนี้", "The account {0} isn't allowed to sign in on this computer."),
            ["Err_SecLogon"] = ("บริการ Secondary Logon ของ Windows ถูกปิดอยู่ ติดต่อผู้ดูแลระบบ", "The Windows Secondary Logon service is turned off. Contact your administrator."),
            ["Err_AxNotFound"] = ("ไม่พบ Ax32.exe ที่ {1} ตรวจตำแหน่งในหน้าตั้งค่า", "Ax32.exe wasn't found at {1}. Check the location in Settings."),
            ["Err_AxcNotFound"] = ("ไม่พบไฟล์ config ที่ {1}", "The config file wasn't found at {1}."),
            ["Err_Decrypt"] = ("อ่านรหัสผ่านที่บันทึกไว้ของ {0} ไม่ได้ ใส่รหัสผ่านใหม่อีกครั้ง", "The saved password for {0} can't be read. Enter it again."),
            ["Err_NoPassword"] = ("โปรไฟล์ {0} ยังไม่มีรหัสผ่าน", "The profile {0} has no password yet."),
            ["Err_Generic"] = ("เปิด AX ด้วย {0} ไม่สำเร็จ: {1}", "Couldn't open AX as {0}: {1}"),
        };
    }

    /// <summary>XAML: Text="{s:L Key}".</summary>
    public class L : MarkupExtension
    {
        public string Key { get; set; }
        public L(string key) { Key = key; }

        public override object ProvideValue(IServiceProvider serviceProvider) =>
            new Binding("[" + Key + "]") { Source = Loc.Instance, Mode = BindingMode.OneWay }
                .ProvideValue(serviceProvider);
    }
}

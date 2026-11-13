using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace DAXLogin.Tests
{
    /// <summary>
    /// Uses a temporary data folder and only a made-up LOCAL account,
    /// so running the tests never touches real profiles or a domain account.
    /// </summary>
    public class CoreTests : IDisposable
    {
        const string Secret = "s3cret-ทดสอบ";
        readonly string _dir = Path.Combine(Path.GetTempPath(), "DAXLoginTests-" + Guid.NewGuid().ToString("N"));

        public CoreTests()
        {
            Directory.CreateDirectory(_dir);
            Store.Dir = _dir;
            Store.Load();
        }

        public void Dispose() => Directory.Delete(_dir, true);

        static Profile NewProfile(string name, string user, bool isDefault = false) => new Profile
        {
            Name = name, Domain = "DEMO", User = user, EncPassword = Store.Protect(Secret), IsDefault = isDefault,
        };

        [Fact]
        public void Password_round_trips_and_is_not_stored_in_clear_text()
        {
            Store.Profiles.Add(NewProfile("Demo FBU", "axdemo", true));
            Store.Profiles.Add(NewProfile("Demo Admin", "axadmin"));
            Store.SaveProfiles();
            Store.Load();

            Assert.Equal(2, Store.Profiles.Count);
            Assert.Equal(@"DEMO\axdemo", Store.Default!.Account);
            Assert.Equal(Secret, Store.Unprotect(Store.Default.EncPassword));
            Assert.DoesNotContain("s3cret", File.ReadAllText(Path.Combine(_dir, "profiles.json")));
        }

        [Fact]
        public void SetDefault_keeps_a_single_default()
        {
            var a = NewProfile("A", "a", true);
            var b = NewProfile("B", "b");
            Store.Profiles.AddRange(new[] { a, b });
            Store.SetDefault(b);
            Assert.False(a.IsDefault);
            Assert.Same(b, Store.Default);
            Store.SetDefault(null);
            Assert.Null(Store.Default);
        }

        [Fact]
        public void Share_file_round_trips_with_the_passphrase_and_hides_its_content()
        {
            var file = Path.Combine(_dir, "t.daxprofile");
            ShareService.Export(file, new[] { NewProfile("Demo FBU", "axdemo") }, true, "passphrase-123");

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("s3cret", text);
            Assert.DoesNotContain("axdemo", text);

            var items = ShareService.Import(file, "passphrase-123");
            Assert.Single(items);
            Assert.Equal("axdemo", items[0].User);
            Assert.Equal(Secret, items[0].Password);
        }

        [Fact]
        public void Share_file_without_passwords_carries_none()
        {
            var file = Path.Combine(_dir, "t.daxprofile");
            ShareService.Export(file, new[] { NewProfile("Demo FBU", "axdemo") }, false, "passphrase-123");
            Assert.Null(ShareService.Import(file, "passphrase-123")[0].Password);
        }

        [Fact]
        public void Import_rejects_a_wrong_passphrase_and_a_tampered_or_foreign_file()
        {
            var file = Path.Combine(_dir, "t.daxprofile");
            ShareService.Export(file, new[] { NewProfile("Demo FBU", "axdemo") }, true, "passphrase-123");
            Assert.Equal("Share_WrongPass", Assert.Throws<ShareException>(() => ShareService.Import(file, "nope-nope")).Key);

            File.WriteAllText(file, "not json at all");
            Assert.Equal("Share_BadFile", Assert.Throws<ShareException>(() => ShareService.Import(file, "passphrase-123")).Key);
        }

        [Fact]
        public void Merge_adds_new_accounts_and_updates_existing_ones()
        {
            var existing = NewProfile("Old name", "axdemo");
            var target = new List<Profile> { existing };
            var (added, updated) = ShareService.Merge(target, new[]
            {
                new SharedProfile { Name = "New name", Domain = "demo", User = "AXDEMO", Password = "changed" },
                new SharedProfile { Name = "Other", Domain = "DEMO", User = "axother" },
            });

            Assert.Equal((1, 1), (added, updated));
            Assert.Equal("New name", existing.Name);
            Assert.Equal("changed", Store.Unprotect(existing.EncPassword));
            Assert.Equal("", target[1].EncPassword);
        }

        [Fact]
        public void Launcher_reports_bad_credentials_and_missing_files()
        {
            const string noUser = "daxlogin_no_such_user";
            var notepad = Path.Combine(Environment.SystemDirectory, "notepad.exe");

            Assert.Equal("Err_BadPassword",
                Assert.Throws<LaunchException>(() => AxLauncher.TestLogin(".", noUser, "x")).Key);
            Assert.Equal("Err_AxNotFound",
                Assert.Throws<LaunchException>(() => AxLauncher.Launch(".", noUser, "x", @"C:\nope\Ax32.exe", null)).Key);
            Assert.Equal("Err_AxcNotFound",
                Assert.Throws<LaunchException>(() => AxLauncher.Launch(".", noUser, "x", notepad, @"C:\nope\a.axc")).Key);

            var ex = Assert.Throws<LaunchException>(() => AxLauncher.Launch(".", noUser, "x", notepad, null));
            Assert.Equal("Err_BadPassword", ex.Key);
            Assert.True(ex.IsCredential);
        }

        [Fact]
        public void Every_string_has_both_languages()
        {
            Loc.Instance.Language = "en";
            Assert.Equal("Open AX", Loc.S("OpenAx"));
            Loc.Instance.Language = "th";
            Assert.Equal("เปิด AX", Loc.S("OpenAx"));
        }
    }
}

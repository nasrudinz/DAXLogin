using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DAXLogin
{
    public class SharedProfile
    {
        public string Name { get; set; } = "";
        public string Domain { get; set; } = "";
        public string User { get; set; } = "";
        public string? Password { get; set; }
        public string AxcPath { get; set; } = "";
    }

    public class ShareFile
    {
        public string Format { get; set; } = "daxprofile";
        public int Version { get; set; } = 1;
        public int Iterations { get; set; }
        public string Salt { get; set; } = "";
        public string Iv { get; set; } = "";
        public string Data { get; set; } = "";
        public string Mac { get; set; } = "";
    }

    public class ShareException : Exception
    {
        public string Key { get; }
        public ShareException(string key) : base(key) { Key = key; }
    }

    /// <summary>
    /// .daxprofile files: profiles encrypted with a passphrase
    /// (PBKDF2-SHA256 -> AES-256-CBC + HMAC-SHA256, encrypt-then-MAC).
    /// </summary>
    public static class ShareService
    {
        const int Iterations = 200_000;

        public static void Export(string file, IEnumerable<Profile> profiles, bool includePasswords, string passphrase)
        {
            var items = profiles.Select(p => new SharedProfile
            {
                Name = p.Name,
                Domain = p.Domain,
                User = p.User,
                AxcPath = p.AxcPath,
                Password = includePasswords ? Store.Unprotect(p.EncPassword) : null,
            }).ToList();
            var plain = JsonSerializer.SerializeToUtf8Bytes(items);

            var salt = RandomBytes(16);
            var iv = RandomBytes(16);
            DeriveKeys(passphrase, salt, Iterations, out var encKey, out var macKey);

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = encKey; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                using var enc = aes.CreateEncryptor();
                cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
            }

            var share = new ShareFile
            {
                Iterations = Iterations,
                Salt = Convert.ToBase64String(salt),
                Iv = Convert.ToBase64String(iv),
                Data = Convert.ToBase64String(cipher),
                Mac = Convert.ToBase64String(ComputeMac(macKey, iv, cipher)),
            };
            File.WriteAllText(file, JsonSerializer.Serialize(share, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static List<SharedProfile> Import(string file, string passphrase)
        {
            ShareFile? share;
            byte[] salt, iv, cipher, mac;
            try
            {
                share = JsonSerializer.Deserialize<ShareFile>(File.ReadAllText(file));
                if (share == null || share.Format != "daxprofile" || share.Version != 1 || share.Iterations < 1)
                    throw new ShareException("Share_BadFile");
                salt = Convert.FromBase64String(share.Salt);
                iv = Convert.FromBase64String(share.Iv);
                cipher = Convert.FromBase64String(share.Data);
                mac = Convert.FromBase64String(share.Mac);
            }
            catch (Exception ex) when (ex is JsonException || ex is FormatException)
            {
                throw new ShareException("Share_BadFile");
            }

            DeriveKeys(passphrase, salt, share.Iterations, out var encKey, out var macKey);
            if (!FixedTimeEquals(mac, ComputeMac(macKey, iv, cipher)))
                throw new ShareException("Share_WrongPass");

            try
            {
                using var aes = Aes.Create();
                aes.Key = encKey; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                using var dec = aes.CreateDecryptor();
                var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                return JsonSerializer.Deserialize<List<SharedProfile>>(plain) ?? new List<SharedProfile>();
            }
            catch (Exception ex) when (ex is CryptographicException || ex is JsonException)
            {
                throw new ShareException("Share_BadFile");
            }
        }

        /// <summary>Adds imported profiles; an existing profile with the same account is updated. Returns (added, updated).</summary>
        public static (int added, int updated) Merge(List<Profile> target, IEnumerable<SharedProfile> incoming)
        {
            int added = 0, updated = 0;
            foreach (var s in incoming)
            {
                if (string.IsNullOrWhiteSpace(s.User)) continue;
                var existing = target.FirstOrDefault(p =>
                    string.Equals(p.Domain, s.Domain, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.User, s.User, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    target.Add(new Profile
                    {
                        Name = string.IsNullOrWhiteSpace(s.Name) ? s.User : s.Name,
                        Domain = s.Domain ?? "",
                        User = s.User,
                        AxcPath = s.AxcPath ?? "",
                        EncPassword = s.Password == null ? "" : Store.Protect(s.Password),
                    });
                    added++;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(s.Name)) existing.Name = s.Name;
                    if (!string.IsNullOrWhiteSpace(s.AxcPath)) existing.AxcPath = s.AxcPath!;
                    if (s.Password != null) existing.EncPassword = Store.Protect(s.Password);
                    updated++;
                }
            }
            return (added, updated);
        }

        static byte[] RandomBytes(int n)
        {
            var b = new byte[n];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(b);
            return b;
        }

        static void DeriveKeys(string passphrase, byte[] salt, int iterations, out byte[] encKey, out byte[] macKey)
        {
            using var kdf = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(passphrase), salt, iterations, HashAlgorithmName.SHA256);
            encKey = kdf.GetBytes(32);
            macKey = kdf.GetBytes(32);
        }

        static byte[] ComputeMac(byte[] macKey, byte[] iv, byte[] cipher)
        {
            using var h = new HMACSHA256(macKey);
            h.TransformBlock(iv, 0, iv.Length, null, 0);
            h.TransformFinalBlock(cipher, 0, cipher.Length);
            return h.Hash!;
        }

        static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}

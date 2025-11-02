using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CourseWork_Shchegol.Domain;

namespace CourseWork_Shchegol.Services
{
    public class UserRepository
    {
        private readonly string _path;
        public UserRepository(string path) => _path = path;

        public List<User> LoadAll()
        {
            var list = new List<User>();
            if (!File.Exists(_path)) return list;

            foreach (var line in File.ReadAllLines(_path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                var parts = line.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length < 2) continue;

                var u = new User { Username = parts[0] };
                var pwd = parts[1];

                if (pwd.Length == 64 && pwd.All(IsHex)) u.PasswordHash = pwd.ToLowerInvariant();
                else u.PasswordPlain = pwd;

                for (int i = 2; i < parts.Length; i++)
                {
                    var kv = parts[i].Split('=', 2, StringSplitOptions.TrimEntries);
                    if (kv.Length != 2) continue;
                    var k = kv[0]; var v = kv[1];

                    if (k.Length == 1 && "ABCDE".Contains(k))
                    {
                        u.DiskRights[k[0]] = FilterModes(v);
                    }
                    else if (k == "pwdChanged")
                    {
                        if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                            u.PasswordChangedUtc = dt.ToUniversalTime();
                    }
                    else if (k == "pwdTTL")
                    {
                        if (int.TryParse(v, out var ttl)) u.PasswordTtlDays = Math.Max(0, ttl);
                    }
                }
                list.Add(u);
            }
            return list;
        }

        public void SaveAll(IEnumerable<User> users)
        {
            var lines = users.Select(u =>
            {
                var pwd = !string.IsNullOrEmpty(u.PasswordPlain) ? u.PasswordPlain : u.PasswordHash;
                var rights = string.Join(";", new[] { 'A', 'B', 'C', 'D', 'E' }.Select(d => $"{d}={u.DiskRights.GetValueOrDefault(d, "")}"));
                var meta = $"pwdChanged={(u.PasswordChangedUtc?.ToLocalTime().ToString("yyyy-MM-dd") ?? "")};pwdTTL={u.PasswordTtlDays}";
                return $"{u.Username};{pwd};{rights};{meta}";
            });
            FileService.WriteAllText(_path, string.Join(Environment.NewLine, lines));
        }

        public static string Sha256Hex(string s)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static bool IsHex(char c) =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        private static string FilterModes(string v)
        {
            var set = new HashSet<char>("ERWA"); 
            return new string(v.Where(c => set.Contains(char.ToUpperInvariant(c))).Select(char.ToUpperInvariant).Distinct().ToArray());
        }
        public bool CheckPassword(User u, string password, out string error)
        {
            error = "";
            if (!string.IsNullOrEmpty(u.PasswordPlain))
            {
                if (u.PasswordPlain != password) { error = "Невірний пароль."; return false; }
            }
            else if (!string.IsNullOrEmpty(u.PasswordHash))
            {
                if (u.PasswordHash != Sha256Hex(password)) { error = "Невірний пароль."; return false; }
            }
            else { error = "Пароль не заданий."; return false; }

            if (u.IsPasswordExpiredUtc(DateTime.UtcNow))
            {
                error = "Строк дії пароля вичерпано. Змініть пароль.";
                return false;
            }
            return true;
        }
    }
}

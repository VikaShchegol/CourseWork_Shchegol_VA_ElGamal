using System;
using System.IO;
using System.Linq;

namespace CourseWork_Shchegol.Services
{
    public static class FileService
    {
        public static byte[] ReadAllBytes(string path)
        {
            EnsureDir(path);
            return File.Exists(path) ? File.ReadAllBytes(path) : Array.Empty<byte>();
        }

        public static void WriteAllBytes(string path, byte[] data)
        {
            EnsureDir(path);
            File.WriteAllBytes(path, data ?? Array.Empty<byte>());
        }

        public static string ReadAllText(string path) =>
            File.Exists(path) ? File.ReadAllText(path) : string.Empty;

        public static void WriteAllText(string path, string text)
        {
            EnsureDir(path);
            File.WriteAllText(path, text ?? "");
        }

        public static bool CompareBinary(string a, string b)
        {
            var ab = ReadAllBytes(a);
            var bb = ReadAllBytes(b);
            return ab.SequenceEqual(bb);
        }

        private static void EnsureDir(string path)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}

using System;
using System.IO;

namespace CourseWork_Shchegol.Services
{
    public class Logger
    {
        private readonly string _path;
        public Logger(string path) => _path = path;

        public void Log(string username, string action, string details)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss};{username};{action};{details}";
            File.AppendAllLines(_path, new[] { line });
        }
    }
}

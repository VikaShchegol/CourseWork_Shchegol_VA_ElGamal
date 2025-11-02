using System.Collections.Generic;
using System.Linq;
using CourseWork_Shchegol.Domain;

namespace CourseWork_Shchegol.Services
{
    public class AuthService
    {
        private readonly UserRepository _repo;
        private readonly Logger _logger;
        private readonly Dictionary<string, int> _attempts = new();

        public AuthService(UserRepository repo, Logger logger) { _repo = repo; _logger = logger; }

        public bool TryLogin(string username, string password, out User user, out string message)
        {
            user = null!; message = "";
            if (!_attempts.ContainsKey(username)) _attempts[username] = 0;
            if (_attempts[username] >= VariantConfig.S) { message = $"Доступ заблоковано (S={VariantConfig.S})."; return false; }

            var users = _repo.LoadAll();
            var found = users.FirstOrDefault(x => x.Username == username);
            string pwdErr = "";

            if (found != null && _repo.CheckPassword(found, password, out pwdErr))
            {
                _logger.Log(username, "LOGIN", "OK");
                _attempts[username] = 0; user = found; return true;
            }
            else
            {
                _logger.Log(username, "LOGIN", "FAIL");
                _attempts[username]++;
                message = found == null ? "Користувача не знайдено." : (pwdErr ?? "Невірний пароль.");
                message += $" Залишилось спроб: {VariantConfig.S - _attempts[username]}";
                return false;
            }

        }
    }
}

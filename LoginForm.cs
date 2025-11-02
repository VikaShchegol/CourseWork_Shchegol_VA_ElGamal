using System;
using System.Drawing;
using System.Windows.Forms;
using CourseWork_Shchegol.Services;
using CourseWork_Shchegol.Domain;
using CourseWork_Shchegol.UI;

namespace CourseWork_Shchegol.Forms
{
    public class LoginForm : Form
    {
        private readonly UserRepository _repo;
        private readonly Logger _logger;
        private readonly AuthService _auth;
        private TextBoxX _txtUser, _txtPass;

        public LoginForm()
        {
            Theme.StyleForm(this, "Вхід до системи", 560, 380);

            Controls.Add(Theme.H1("Система доступу", new Point(24, 20)));

            var card = new Card { Location = new Point(24, 70), Size = new Size(512, 240) };
            Controls.Add(card);

            card.Controls.Add(Theme.Body("Увійдіть до системи, використовуючи ваші облікові дані.", new Point(20, 10), Palette.Muted));
            card.Controls.Add(new Divider { Top = 36, Width = card.Width });

            _txtUser = new TextBoxX { Placeholder = "Логін", Location = new Point(20, 60), Width = 460 };
            _txtPass = new TextBoxX { Placeholder = "Пароль", Location = new Point(20, 100), Width = 460, UseSystemPasswordChar = true };
            card.Controls.Add(_txtUser); card.Controls.Add(_txtPass);

            var btnLogin = new ButtonX { Text = "Увійти", Variant = ButtonX.Kind.Primary, Location = new Point(20, 150), Width = 460 };
            btnLogin.Click += BtnLogin_Click;
            card.Controls.Add(btnLogin);

            _repo = new UserRepository(@"Files\nameuser.txt");
            _logger = new Logger(@"Files\us_book.txt");
            _auth = new AuthService(_repo, _logger);
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            var u = _txtUser.Text?.Trim();
            var p = _txtPass.Text ?? "";
            if (u == _txtUser.Placeholder) u = "";
            if (p == _txtPass.Placeholder) p = "";

            if (_auth.TryLogin(u!, p, out User user, out string msg))
            {
                var ws = new WorkspaceForm(user, _logger);
                ws.Show();
                Hide();
            }
            else
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(msg) ? "Невірні дані" : msg);
                if (msg.Contains("блоковано")) Close();
            }
        }

    }
}

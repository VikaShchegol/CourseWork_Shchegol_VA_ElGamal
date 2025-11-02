using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CourseWork_Shchegol.Domain;
using CourseWork_Shchegol.Services;
using CourseWork_Shchegol.UI;
using System.IO;

namespace CourseWork_Shchegol.Forms
{
    public class WorkspaceForm : Form
    {
        private UserRepository _repo = new UserRepository(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Files\nameuser.txt"));

        private readonly User _user;
        private readonly Logger _logger;
        private readonly HandshakeService _handshake;
        private System.Windows.Forms.Timer _timer;
        private Label _lblStatus, _lblNext;
        private CryptoService _crypto = new CryptoService();
        private CryptoService.PublicKey _pub;
        private CryptoService.PrivateKey _priv;

        private DateTime _nextTick;

        public WorkspaceForm(User user, Logger logger)
        {
            _user = user; _logger = logger;

            Theme.StyleForm(this, "Робоче середовище", 920, 520);

            Controls.Add(Theme.H1($"Вітаємо, {_user.Username}!", new Point(24, 20)));
            Controls.Add(Theme.Body("Журнал: Files/us_book.txt", new Point(26, 56), Palette.Muted));

            var cardHS = new Card { Location = new Point(24, 90), Size = new Size(860, 160) };
            Controls.Add(cardHS);
            cardHS.Controls.Add(Theme.Body($"Рукостискання кожні {VariantConfig.T} сек. Формула: Y = lg(4·X)", new Point(20, 10)));
            cardHS.Controls.Add(new Divider { Top = 36, Width = cardHS.Width });

            _lblStatus = Theme.Body("Очікування перевірки…", new Point(20, 60), Palette.Muted);
            _lblNext = Theme.Body("", new Point(20, 85), Palette.Muted);
            var btnNow = new ButtonX { Text = "Перевірити зараз", Variant = ButtonX.Kind.Accent, Location = new Point(680, 60), Width = 150 };
            btnNow.Click += (s, e) => _handshake.AskOnce();
            cardHS.Controls.Add(_lblStatus); cardHS.Controls.Add(_lblNext); cardHS.Controls.Add(btnNow);

            var cardCrypto = new Card { Location = new Point(24, 270), Size = new Size(860, 220) };
            Controls.Add(cardCrypto);
            cardCrypto.Controls.Add(Theme.Body("Криптографія (ElGamal). Файли: input.txt ↔ out.txt; Підпис: ask.txt ↔ close.txt", new Point(20, 10)));
            cardCrypto.Controls.Add(new Divider { Top = 36, Width = cardCrypto.Width });

            var btnGen = new ButtonX { Text = "Згенерувати ключі", Variant = ButtonX.Kind.Accent, Location = new Point(20, 60), Width = 190, Name = "btnGen" };
            var btnSave = new ButtonX { Text = "Зберегти ключі", Variant = ButtonX.Kind.Ghost, Location = new Point(220, 60), Width = 150, Name = "btnSave" };
            var btnLoad = new ButtonX { Text = "Завантажити ключі", Variant = ButtonX.Kind.Ghost, Location = new Point(380, 60), Width = 170, Name = "btnLoad" };

            var btnEnc = new ButtonX { Text = "Зашифрувати input → out", Variant = ButtonX.Kind.Primary, Location = new Point(20, 110), Width = 250, Name = "btnEnc" };
            var btnDec = new ButtonX { Text = "Розшифрувати out → input.dec", Variant = ButtonX.Kind.Primary, Location = new Point(280, 110), Width = 250, Name = "btnDec" };

            var btnSign = new ButtonX { Text = "Підписати ask → close", Variant = ButtonX.Kind.Primary, Location = new Point(20, 160), Width = 250, Name = "btnSign" };
            var btnVerify = new ButtonX { Text = "Перевірити підпис ask/close", Variant = ButtonX.Kind.Primary, Location = new Point(280, 160), Width = 250, Name = "btnVerify" };

            cardCrypto.Controls.AddRange(new Control[] { btnGen, btnSave, btnLoad, btnEnc, btnDec, btnSign, btnVerify });

            var lblCStatus = Theme.Body("Ключі: не завантажені", new Point(570, 65), Palette.Muted);
            cardCrypto.Controls.Add(lblCStatus);

            string Dir(string rel) => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rel);

            btnGen.Click += (s, e) =>
            {
                (_pub, _priv) = _crypto.GenerateKeys(VariantConfig.R);
                _logger.Log(_user.Username, "KEYGEN", $"p={_pub.p.ToString().Length} digits");
                lblCStatus.Text = $"Ключі в пам'яті (p={_pub.p.ToString().Length} цифр)";
                MessageBox.Show("Ключі згенеровано в пам'яті.");
            };

            btnSave.Click += (s, e) =>
            {
                try
                {
                    CryptoService.SavePublic(Dir(@"Files\pubkey.txt"), _pub);
                    CryptoService.SavePrivate(Dir(@"Files\privkey.txt"), _priv);
                    _logger.Log(_user.Username, "KEYSAVE", "OK");
                    lblCStatus.Text = "Ключі збережено у Files";
                    MessageBox.Show("Збережено: Files\\pubkey.txt та Files\\privkey.txt");
                }
                catch { MessageBox.Show("Немає ключів у пам'яті. Згенеруйте або завантажте."); }
            };

            btnLoad.Click += (s, e) =>
            {
                try
                {
                    _pub = CryptoService.LoadPublic(Dir(@"Files\pubkey.txt"));
                    _priv = CryptoService.LoadPrivate(Dir(@"Files\privkey.txt"));
                    _logger.Log(_user.Username, "KEYLOAD", "OK");
                    lblCStatus.Text = "Ключі завантажено із Files";
                    MessageBox.Show("Завантажено: Files\\pubkey.txt та Files\\privkey.txt");
                }
                catch (Exception ex) { MessageBox.Show("Не вдалося завантажити ключі: " + ex.Message); }
            };

            btnEnc.Click += (s, e) =>
            {
                try
                {
                    _crypto.EncryptFile(Dir(@"Files\input.txt"), Dir(@"Files\out.txt"), _pub);
                    _logger.Log(_user.Username, "ENC", "input->out");
                    MessageBox.Show("Файл зашифровано: out.txt");
                }
                catch { MessageBox.Show("Потрібен відкритий ключ (згенеруй/завантаж)."); }
            };

            btnDec.Click += (s, e) =>
            {
                try
                {
                    _crypto.DecryptFile(Dir(@"Files\out.txt"), Dir(@"Files\input.dec"), _priv);
                    _logger.Log(_user.Username, "DEC", "out->input.dec");
                    MessageBox.Show("Файл розшифровано: input.dec");
                }
                catch (Exception ex) { MessageBox.Show("Потрібен закритий ключ або не валідні дані: " + ex.Message); }
            };

            btnSign.Click += (s, e) =>
            {
                try
                {
                    _crypto.SignFile(Dir(@"Files\ask.txt"), Dir(@"Files\close.txt"), _priv);
                    _logger.Log(_user.Username, "SIGN", "ask->close");
                    MessageBox.Show("Підпис збережено: close.txt");
                }
                catch (Exception ex) { MessageBox.Show("Потрібен закритий ключ: " + ex.Message); }
            };

            btnVerify.Click += (s, e) =>
            {
                try
                {
                    var ok = _crypto.VerifyFile(Dir(@"Files\ask.txt"), Dir(@"Files\close.txt"), _pub);
                    _logger.Log(_user.Username, "VERIFY", ok ? "OK" : "FAIL");
                    MessageBox.Show(ok ? "Підпис ВАЛІДНИЙ" : "Підпис НЕвалідний");
                }
                catch (Exception ex) { MessageBox.Show("Помилка перевірки: " + ex.Message); }
            };

            var btnAdmin = new UI.ButtonX { Text = "Адмін-панель", Variant = UI.ButtonX.Kind.Ghost, Location = new Point(700, 20), Width = 130 };
            btnAdmin.Click += (s, e) =>
            {
                if (_user.Username.ToLowerInvariant() != "admin")
                {
                    MessageBox.Show("Доступ дозволений лише адміністратору.");
                    return;
                }
                using var f = new Forms.AdminForm(_repo);
                f.ShowDialog(this);
            };
            Controls.Add(btnAdmin);

            _handshake = new HandshakeService(this, _logger, VariantConfig.T);
            _handshake.OnStatus += s => _lblStatus.Text = s;

            _timer = _handshake.Start();
            _nextTick = DateTime.Now.AddSeconds(VariantConfig.T);

            var uiTimer = new System.Windows.Forms.Timer { Interval = 250 };
            uiTimer.Tick += (_, __) =>
            {
                var rem = _nextTick - DateTime.Now;
                if (rem.TotalMilliseconds <= 0)
                    _nextTick = DateTime.Now.AddSeconds(VariantConfig.T);
                _lblNext.Text = $"Наступна перевірка через: {Math.Max(0, (int)rem.TotalSeconds)} с";
            };
            uiTimer.Start();

            ApplyAccessRights(_user);
        }

        private void ApplyAccessRights(User user)
        {
            bool hasE = user.AccessRights.Contains('E');
            bool hasR = user.AccessRights.Contains('R');
            bool hasW = user.AccessRights.Contains('W');
            bool hasA = user.AccessRights.Contains('A');

            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is Card card)
                {
                    foreach (Control sub in card.Controls)
                    {
                        if (sub is ButtonX btn)
                        {
                            switch (btn.Name)
                            {
                                case "btnGen":
                                case "btnSave":
                                    btn.Enabled = hasW || hasA; break;

                                case "btnLoad":
                                    btn.Enabled = hasR || hasE; break;

                                case "btnEnc":
                                case "btnDec":
                                    btn.Enabled = hasW; break;

                                case "btnSign":
                                    btn.Enabled = hasA || hasW; break;

                                case "btnVerify":
                                    btn.Enabled = hasE || hasR; break;
                            }
                        }
                    }
                }
            }

            if (hasE && hasR && hasW && hasA)
            {
                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl is Card card)
                        foreach (Control sub in card.Controls)
                            if (sub is ButtonX btn)
                                btn.Enabled = true;
                }
            }
        }
    }
}

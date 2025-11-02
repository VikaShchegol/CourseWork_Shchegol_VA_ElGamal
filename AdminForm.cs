using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CourseWork_Shchegol.Domain;
using CourseWork_Shchegol.Services;
using CourseWork_Shchegol.UI;

namespace CourseWork_Shchegol.Forms
{
    public class AdminForm : Form
    {
        private readonly UserRepository _repo;
        private DataGridView _grid;
        private ButtonX _btnAdd, _btnDel, _btnSave, _btnReload;

        public AdminForm(UserRepository repo)
        {
            _repo = repo;

            Theme.StyleForm(this, "Адмін-панель користувачів", 880, 540, center: true);

            var card = new Card { Location = new Point(20, 20), Size = new Size(840, 460) };
            Controls.Add(card);

            card.Controls.Add(Theme.Body("Користувачі (формат прав: {E,R,W,A})", new Point(16, 10)));
            card.Controls.Add(new Divider { Top = 36, Width = card.Width });

            _grid = new DataGridView
            {
                Location = new Point(16, 50),
                Size = new Size(808, 340),
                ReadOnly = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            card.Controls.Add(_grid);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ім’я", Name = "colUser", FillWeight = 140 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Пароль / SHA256", Name = "colPwd", FillWeight = 180 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "A", Name = "colA", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "B", Name = "colB", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "C", Name = "colC", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "D", Name = "colD", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "E", Name = "colE", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TTL (дн.)", Name = "colTTL", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Змінено (YYYY-MM-DD)", Name = "colChanged", FillWeight = 120 });

            _btnAdd = new ButtonX { Text = "Додати", Variant = ButtonX.Kind.Accent, Location = new Point(16, 402), Width = 120 };
            _btnDel = new ButtonX { Text = "Видалити", Variant = ButtonX.Kind.Danger, Location = new Point(146, 402), Width = 120 };
            _btnSave = new ButtonX { Text = "Зберегти", Variant = ButtonX.Kind.Primary, Location = new Point(636, 402), Width = 90 };
            _btnReload = new ButtonX { Text = "Оновити", Variant = ButtonX.Kind.Ghost, Location = new Point(736, 402), Width = 90 };
            card.Controls.AddRange(new Control[] { _btnAdd, _btnDel, _btnSave, _btnReload });

            _btnAdd.Click += (s, e) =>
            {
                if (_grid == null) { MessageBox.Show("Таблиця не ініціалізована."); return; }
                if (_grid.Rows.Count >= VariantConfig.N)
                {
                    MessageBox.Show($"За умовою варіанта N={VariantConfig.N}. Більше користувачів додати не можна.");
                    return;
                }
                _grid.Rows.Add("user", "", "", "", "", "", "", "0", DateTime.Now.ToString("yyyy-MM-dd"));
            };

            _btnDel.Click += (s, e) => { foreach (DataGridViewRow r in _grid.SelectedRows) _grid.Rows.Remove(r); };
            _btnReload.Click += (s, e) => LoadData();
            _btnSave.Click += (s, e) => SaveData();

            LoadData();
        }


        private void LoadData()
        {
            _grid.Rows.Clear();
            foreach (var u in _repo.LoadAll())
            {
                var pwd = !string.IsNullOrEmpty(u.PasswordPlain) ? u.PasswordPlain : u.PasswordHash;
                string ch = u.PasswordChangedUtc?.ToLocalTime().ToString("yyyy-MM-dd") ?? "";
                _grid.Rows.Add(
                    u.Username, pwd,
                    u.DiskRights.GetValueOrDefault('A', ""),
                    u.DiskRights.GetValueOrDefault('B', ""),
                    u.DiskRights.GetValueOrDefault('C', ""),
                    u.DiskRights.GetValueOrDefault('D', ""),
                    u.DiskRights.GetValueOrDefault('E', ""),
                    u.PasswordTtlDays.ToString(CultureInfo.InvariantCulture),
                    ch
                );
            }
        }

        private static string FilterModes(string v)
        {
            var ok = "ERWA";
            return new string((v ?? "")
                .ToUpperInvariant()
                .Where(c => ok.Contains(c))
                .Distinct()
                .ToArray());
        }

        private void SaveData()
        {
            if (_grid.Rows.Count > VariantConfig.N)
            {
                MessageBox.Show($"У журналі не може бути більше ніж {VariantConfig.N} користувачів.");
                return;
            }
            var list = _repo.LoadAll();
            list.Clear();

            foreach (DataGridViewRow r in _grid.Rows)
            {
                if (r.IsNewRow) continue;
                var u = new User();
                string user = (r.Cells["colUser"].Value ?? "").ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(user))
                {
                    MessageBox.Show("Порожнє ім’я користувача недопустиме."); return;
                }
                u.Username = user;

                var pwd = (r.Cells["colPwd"].Value ?? "").ToString() ?? "";

                bool isHash = pwd.Length == 64 && pwd.All(c =>
                    (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
                if (isHash) u.PasswordHash = pwd.ToLowerInvariant();
                else u.PasswordPlain = pwd;

                u.DiskRights['A'] = FilterModes((r.Cells["colA"].Value ?? "").ToString() ?? "");
                u.DiskRights['B'] = FilterModes((r.Cells["colB"].Value ?? "").ToString() ?? "");
                u.DiskRights['C'] = FilterModes((r.Cells["colC"].Value ?? "").ToString() ?? "");
                u.DiskRights['D'] = FilterModes((r.Cells["colD"].Value ?? "").ToString() ?? "");
                u.DiskRights['E'] = FilterModes((r.Cells["colE"].Value ?? "").ToString() ?? "");

                var ttlStr = (r.Cells["colTTL"].Value ?? "0").ToString() ?? "0";
                if (!int.TryParse(ttlStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ttl) || ttl < 0) ttl = 0;
                u.PasswordTtlDays = ttl;

                var chStr = (r.Cells["colChanged"].Value ?? "").ToString() ?? "";
                if (DateTime.TryParse(chStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                    u.PasswordChangedUtc = dt.ToUniversalTime();

                list.Add(u);
            }

            _repo.SaveAll(list);
            MessageBox.Show("Збережено `nameuser.txt`.");
        }
    }
}

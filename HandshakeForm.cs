using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CourseWork_Shchegol.UI;

namespace CourseWork_Shchegol.Forms
{
    public class HandshakeForm : Form
    {
        public double AnswerY { get; private set; }
        public bool Skipped { get; private set; } = false;

        private readonly double _x;
        private UI.TextBoxX _txtY;

        public HandshakeForm(double x)
        {
            _x = x;
            Theme.StyleForm(this, "Перевірка автентичності", 520, 260, center: true);

            var card = new UI.Card { Location = new Point(24, 24), Size = new Size(460, 180) };
            Controls.Add(card);

            var title = Theme.Body($"X = {_x.ToString(CultureInfo.InvariantCulture)}; введіть Y = lg(4·X)", new Point(20, 10));
            card.Controls.Add(title);
            card.Controls.Add(new UI.Divider { Top = 36, Width = card.Width });

            _txtY = new UI.TextBoxX { Placeholder = "Введіть Y (десяткова крапка)", Location = new Point(20, 56), Width = 410 };
            card.Controls.Add(_txtY);

            var btnOK = new UI.ButtonX { Text = "ОК", Variant = UI.ButtonX.Kind.Primary, Location = new Point(20, 110), Width = 130 };
            var btnSkip = new UI.ButtonX { Text = "Відкласти", Variant = UI.ButtonX.Kind.Accent, Location = new Point(170, 110), Width = 130 };
            var btnCancel = new UI.ButtonX { Text = "Відміна", Variant = UI.ButtonX.Kind.Danger, Location = new Point(320, 110), Width = 130 };

            card.Controls.Add(btnOK);
            card.Controls.Add(btnSkip);
            card.Controls.Add(btnCancel);

            btnOK.Click += (s, e) =>
            {
                if (double.TryParse(_txtY.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    AnswerY = y;
                    DialogResult = DialogResult.OK;
                }
                else
                    MessageBox.Show("Некоректне число");
            };

            btnSkip.Click += (s, e) =>
            {
                Skipped = true;
                DialogResult = DialogResult.OK;
            };

            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
        }
    }
}

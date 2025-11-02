using System.Drawing;
using System.Windows.Forms;
using CourseWork_Shchegol.UI;

namespace CourseWork_Shchegol
{
    public static class Theme
    {
        public static void StyleForm(Form f, string title, int w, int h, bool center = true)
        {
            f.Text = title;
            f.BackColor = Palette.Bg;
            f.ForeColor = Palette.Text;
            f.Font = Palette.Body;
            f.FormBorderStyle = FormBorderStyle.FixedSingle;
            f.MaximizeBox = false;
            f.ClientSize = new Size(w, h);
            if (center) f.StartPosition = FormStartPosition.CenterScreen;
            WinForms.Smooth(f);
        }

        public static Label H1(string text, Point p)
            => new Label { Text = text, Location = p, AutoSize = true, Font = Palette.H1, ForeColor = Palette.Text };

        public static Label Body(string text, Point p, Color? c = null)
            => new Label { Text = text, Location = p, AutoSize = true, Font = Palette.Body, ForeColor = c ?? Palette.Text };
    }
}

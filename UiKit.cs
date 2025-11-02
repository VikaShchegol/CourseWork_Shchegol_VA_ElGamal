using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CourseWork_Shchegol.UI
{
    public static class Palette
    {
        public static readonly Color Bg = ColorTranslator.FromHtml("#F7F9FC");
        public static readonly Color Card = Color.White;
        public static readonly Color Text = ColorTranslator.FromHtml("#2B2D42");
        public static readonly Color Muted = ColorTranslator.FromHtml("#6B7280");
        public static readonly Color Primary = ColorTranslator.FromHtml("#D8E6FF");
        public static readonly Color Mint = ColorTranslator.FromHtml("#E2F0D9");
        public static readonly Color Rose = ColorTranslator.FromHtml("#FDE2E4");
        public static readonly Color Line = ColorTranslator.FromHtml("#E5E7EB");

        public static readonly Font H1 = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static readonly Font Body = new Font("Segoe UI", 10f, FontStyle.Regular);
    }

    public static class WinForms
    {
        public static void Smooth(Form f)
        {
            var prop = typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            prop?.SetValue(f, true, null);
        }
    }


    public class Card : Panel
    {
        public int Radius { get; set; } = 16;
        public int Shadow { get; set; } = 10;

        public Card()
        {
            BackColor = Palette.Card;
            Padding = new Padding(20);
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var pathShadow = RoundedPath(new Rectangle(Shadow / 2, Shadow / 2, Width - Shadow, Height - Shadow), Radius))
            using (var brush = new PathGradientBrush(pathShadow)
            { CenterColor = Color.FromArgb(40, Color.Black), SurroundColors = new[] { Color.FromArgb(0, Color.Black) } })
            { g.FillPath(brush, pathShadow); }

            using (var pathBody = RoundedPath(new Rectangle(0, 0, Width - Shadow, Height - Shadow), Radius))
            using (var body = new SolidBrush(Palette.Card))
            { g.FillPath(body, pathBody); }
        }

        private GraphicsPath RoundedPath(Rectangle r, int rad)
        {
            int d = rad * 2; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }

    public class ButtonX : Button
    {
        public enum Kind { Primary, Accent, Danger, Ghost }
        public Kind Variant { get; set; } = Kind.Primary;
        public int Radius { get; set; } = 12;

        public ButtonX()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Height = 38;
            Font = new Font("Segoe UI", 10f, FontStyle.Bold); 
            Cursor = Cursors.Hand;
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color bg = Palette.Primary, fg = Palette.Text, border = Color.Transparent;
            if (Variant == Kind.Accent) bg = Palette.Mint;
            if (Variant == Kind.Danger) bg = Palette.Rose;
            if (Variant == Kind.Ghost) { bg = Color.Transparent; border = Palette.Line; }

            using (var path = Rounded(rect, Radius))
            {
                using (var br = new SolidBrush(bg)) g.FillPath(br, path);
                using (var pen = new Pen(border)) g.DrawPath(pen, path);
                TextRenderer.DrawText(g, Text, Font, rect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private GraphicsPath Rounded(Rectangle r, int rad)
        {
            int d = rad * 2; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }
    public class TextBoxX : TextBox
    {
        public string Placeholder { get; set; } = "";
        protected override void OnCreateControl() { base.OnCreateControl(); SetPh(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); if (Text == Placeholder) { Text = ""; ForeColor = Palette.Text; } }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); SetPh(); }
        private void SetPh() { if (string.IsNullOrWhiteSpace(Text)) { Text = Placeholder; ForeColor = Palette.Muted; } }
    }
    public class Divider : Control
    {
        public Divider() { Height = 1; Dock = DockStyle.Top; }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.FillRectangle(new SolidBrush(Palette.Line), new Rectangle(0, 0, Width, Height)); }
    }
}

using System;
using System.Globalization;
using System.Windows.Forms;

namespace CourseWork_Shchegol.Services
{
    public class HandshakeService
    {
        private readonly Logger _logger;
        private readonly double _periodSec;
        private readonly Random _rnd = new Random();
        private readonly Form _owner;

        public event Action<string>? OnStatus;

        public HandshakeService(Form owner, Logger logger, double periodSec)
        {
            _owner = owner;
            _logger = logger;
            _periodSec = periodSec;
        }

        public System.Windows.Forms.Timer Start()
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = (int)(_periodSec * 1000);
            timer.Tick += (_, __) => AskOnce();
            timer.Start();
            return timer;
        }

        public async void AskOnce()
        {
            if (_owner.OwnedForms.Length > 0)
                return;

            var x = Math.Round(0.25 + _rnd.NextDouble() * 1.75, 2);
            using var dlg = new Forms.HandshakeForm(x);
            var dr = dlg.ShowDialog(_owner);

            if (dr == DialogResult.OK)
            {
                if (dlg.Skipped)
                {
                    _logger.Log(Environment.UserName, "HANDSHAKE", $"SKIPPED (X={x})");
                    OnStatus?.Invoke("Перевірку відкладено користувачем");
                    return;
                }

                var expected = VariantConfig.F(x);
                var ok = Math.Abs(dlg.AnswerY - expected) <= 1e-6;
                _logger.Log(Environment.UserName, "HANDSHAKE", ok ? $"OK (X={x}, Y={dlg.AnswerY})" : $"FAIL (X={x}, Y={dlg.AnswerY}, need {expected})");
                OnStatus?.Invoke(ok ? $"Перевірка пройдена (X={x})" : $"Помилка перевірки (X={x})");

                if (!ok)
                {
                    MessageBox.Show(
                        $"Невірно. Потрібно: lg(4·{x.ToString(CultureInfo.InvariantCulture)}) = {expected.ToString("0.######", CultureInfo.InvariantCulture)}",
                        "Помилка рукостискання");
                }
            }
            else
            {
                _logger.Log(Environment.UserName, "HANDSHAKE", $"CANCEL (X={x})");
                OnStatus?.Invoke("Перевірку скасовано користувачем");
            }
            await System.Threading.Tasks.Task.Delay(2000);
        }

    }
}

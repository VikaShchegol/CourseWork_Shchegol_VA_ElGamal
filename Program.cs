using System;
using System.Windows.Forms;

namespace CourseWork_Shchegol
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.Run(new Forms.LoginForm());
        }
    }
}

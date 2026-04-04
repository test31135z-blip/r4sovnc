using System;
using System.Windows.Forms;
using R4SoVNC.Server.Forms;

namespace R4SoVNC.Server
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}

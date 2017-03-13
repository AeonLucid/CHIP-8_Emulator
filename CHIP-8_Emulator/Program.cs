using System;
using System.Windows.Forms;
using CHIP_8_Emulator.Forms;

namespace CHIP_8_Emulator
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmGame());
        }
    }
}

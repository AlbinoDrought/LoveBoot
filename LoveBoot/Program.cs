using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoveBoot
{
    static class Program
    {
        private const string PROCESS_NAME = "LoveBeat";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            BotLogic botLogic = new BotLogic(PROCESS_NAME);

            Application.Run(botLogic.GameOverlay);
        }
    }
}

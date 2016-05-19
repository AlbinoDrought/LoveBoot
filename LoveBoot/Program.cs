using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoveBoot
{
    static class Program
    {
        private static readonly string[] PROCESS_NAMES = { "LoveBeat", "LoveRitmo" };

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            BotLogic botLogic = new BotLogic(PROCESS_NAMES);

            if(botLogic.GameOverlay != null) Application.Run(botLogic.GameOverlay);
        }
    }
}

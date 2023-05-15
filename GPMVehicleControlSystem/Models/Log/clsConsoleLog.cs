using System;
using System.Diagnostics;

namespace GPMVehicleControlSystem.Models.Log
{
    public class clsConsoleLog
    {
        private Process consoleLogProcess;
        public clsConsoleLog()
        {
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                OpenConsole();
            });
        }

        public void Log(string msg, ConsoleColor color = ConsoleColor.Red)
        {
            try
            {

                consoleLogProcess.StandardInput.WriteLine(msg);
                //consoleLogProcess.StandardInput.Close();
            }
            catch (Exception ex)
            {
            }
        }

        private void OpenConsole()
        {
            consoleLogProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c start cmd.exe";
            startInfo.RedirectStandardInput = true;
            consoleLogProcess.StartInfo = startInfo;
            consoleLogProcess.Start();
            consoleLogProcess.WaitForExit();

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SqlMana
{
    public class ManaProcess
    {
        public static int returnCode = 0;
        public static List<string> output = new List<string>();

        public static List<string> runExe(string exePath, string arguments, bool silent = false)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exePath;
            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            if(silent)
            {
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }
            process.Start();

            string temp = process.StandardOutput.ReadLine();
            while (temp != null)
            {
                output.Add(temp);
                temp = process.StandardOutput.ReadLine();
            }
            
            process.WaitForExit();
            returnCode = process.ExitCode;
            process.Close();

            return output;
        }
    }
}

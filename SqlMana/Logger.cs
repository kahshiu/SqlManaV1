using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SqlMana
{
    class Logger
    {
        private List<string> logs;
        private string logTemplate;
        private string culture = "en-GB";
        private Config c;

        public Logger(Config origC)
        {
            logTemplate = "Timestamp {{{0}}}: {1} {2}";
            logs = new List<string>();
            c = origC;
            c.Log = this;

            Init();
        }

        public string GetLogs()
        {
            string compiled = "";
            foreach (string log in logs)
            {
                compiled = compiled + log;
            }
            return compiled;
        }

        public string GetLogPath()
        {
            string temp;

            if (c.LogPath.Length > 0)
            {
                temp = c.LogPath;
            }
            else
            {
                temp = c.ExePath;
                temp = string.Format(@"{0}\Log", temp);
            }
            return temp;
        }

        public string GetLogDateTime()
        {
            return DateTime.Now.ToString(new CultureInfo(this.culture));
        }

        public string GetLogDateTime2()
        {
            return DateTime.Now.ToOADate().ToString();
        }

        public void Init()
        {
            this.AppendLog("Logger initiated");
        }

        public void AppendLog(string message)
        {
            string log = string.Format(
                logTemplate
                , GetLogDateTime()
                , message
                , Environment.NewLine
                );
            this.logs.Add(log);
        }

        public void WriteLog()
        {
            Directory.CreateDirectory(GetLogPath());

            List<string> fileName = new List<string>();
            fileName.Add(c.Server);
            fileName.Add(c.Database);
            if (c.LogSuffix != "") fileName.Add(c.LogSuffix);
            fileName.Add(GetLogDateTime2());  

            string filePath = string.Format(@"{0}\{1}.log", GetLogPath(), string.Join("_",fileName));

            if(c.LogKill == 0) File.WriteAllText(filePath, GetLogs());
        }

        public void PurgeLog()
        {
            logs.Clear();
        }
    }
}

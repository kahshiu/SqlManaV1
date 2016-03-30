using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace SqlMana
{
    class Config
    {
        // program use ONLY
        private Logger progLogger;
        private ManaTap progTapper;
        private ManaFile progFilr;

        // program use ONLY: Must be directory
        private string progPath = "";
        public string progLogPath = "";
        public string progLogSuffix = "";
        public int progLogKill = 0;

        // seqAction: sequence: db,file <-- default seqence
        // dbAction: {SQL: select,update; SSP:selectSSP,updateSSP}
        // fileAction: {generic: read,write; SSP:readSSP,writeSSP,compareSSP}
        private string progSeqAction = "";
        private string progDBAction = "";
        private string progFileAction = "";
        private string progSQL = "";
        private string progSQLWhere = "";

        private string dbServer = "";
        private string dbName = "";
        private string dbAuthType = "";
        private string dbUsername = "";
        private string dbPassword = "";
        private string dbConnString = "";

        //repoPath to read ssp from/ write ssp into
        private string repoPath = "";
        //repo2Path to compare with repoPath
        private string repo2Path = "";
        //path to store result of comparison 
        private string cOutPath = "";
        //path to extract all SSPs for uploading
        private string cInPath = "";
        //name of SSPs for uploading
        private string cInData = "";

        //ignore operating on the following sproc
        //private string ignoreSSP;
        //private string ignoreFile;

        public Config()
        {
            SetDefaults();
        }

        public void Trace()
        {
            Console.WriteLine("progPath      : " + this.progPath);
            Console.WriteLine("progLogPath   : " + this.progLogPath);
            Console.WriteLine("dbUsername    : " + this.dbUsername);
            Console.WriteLine("dbPassword    : " + this.dbPassword);
            Console.WriteLine("progSeqAction : " + this.progSeqAction);
            Console.WriteLine("progDBAction  : " + this.progDBAction);
            Console.WriteLine("progFileAction: " + this.progFileAction);
            Console.WriteLine("progSQL       : " + this.progSQL);
            Console.WriteLine("progSQLWhere  : " + this.progSQLWhere);
            Console.WriteLine("dbServer      : " + this.dbServer);
            Console.WriteLine("dbName        : " + this.dbName);
            Console.WriteLine("dbAuthType    : " + this.dbAuthType);
            Console.WriteLine("dbConnString  : " + this.dbConnString);
            Console.WriteLine("repoPath      : " + this.repoPath);
            Console.WriteLine("repo2Path     : " + this.repo2Path);
            Console.WriteLine("cOutPath      : " + this.cOutPath);
            Console.WriteLine("cInPath       : " + this.cInPath);
            Console.WriteLine("cInData       : " + this.cInData);
            Console.WriteLine("progLogKill   : " + this.progLogKill.ToString());
        }

        public void SetDefaults()
        {
            this.progPath = Path.GetDirectoryName(
                System.Reflection.Assembly.GetEntryAssembly().Location.ToString()
            );
        }

        public void ConfigFromFile(string configPath)
        {
            string configLine;
            using (StreamReader file = new StreamReader(configPath))
            {
                while ((configLine = file.ReadLine()) != null)
                {
                    ConfigSynch(configLine);
                }
            }
        }

        public void ConfigData(string[] data)
        {
            for (int i = 1; i < data.Length; i++)
            {
                ConfigSynch(data[i]);
            }
        }

        public void ConfigSynch(string line)
        {
            string name = "";
            string val = "";
            string[] temp;

            Regex reg = new Regex(@"\/\/.+");
            line = reg.Replace(line, "");
            if (line == "")
            {
                return;
            }

            temp = line.Split('|');
            name = temp[0];
            for (int i = 1; i < temp.Length; i++) val = val + temp[i];

            // lowercase names to eliminate case sensitivity
            name = name.Trim().ToLower();
            val = val.Trim();

            if (name == "exepath") { progPath = val != "" ? val : ""; }
            else if (name == "logpath") { progLogPath = val != "" ? val : ""; }
            else if (name == "logsuffix") { progLogSuffix = val != "" ? val : ""; }
            else if (name == "logkill")
            {
                int killFlag;
                if (Int32.TryParse(val, out killFlag))
                {
                    killFlag = Int32.Parse(val);
                    progLogKill = killFlag == 1 ? 1 : 0;
                }
            }

            else if (name == "seqaction") { progSeqAction = val != "" ? val : ""; }
            else if (name == "dbaction") { progDBAction = val != "" ? val : ""; }
            else if (name == "fileaction") { progFileAction = val != "" ? val : ""; }
            else if (name == "sql") { progSQL = val != "" ? val : ""; }
            else if (name == "sqlwhere") { progSQLWhere = val != "" ? val : ""; }
            else if (name == "server") { dbServer = val != "" ? val : ""; }
            else if (name == "database") { dbName = val != "" ? val : ""; }
            else if (name == "authtype") { dbAuthType = val != "" ? val : ""; }
            else if (name == "authstring") { AuthString = val != "" ? val : ""; }

            else if (name == "repopath") { repoPath = val != "" ? val : ""; }
            else if (name == "repo2path") { repo2Path = val != "" ? val : ""; }
            else if (name == "outpath") { cOutPath = val != "" ? val : ""; }
            else if (name == "inpath") { cInPath = val != "" ? val : ""; }
            else if (name == "indata") { cInData = val != "" ? val : ""; }

            else if (name == "username") { dbUsername = val != "" ? val : ""; }
            else if (name == "password") { dbPassword = val != "" ? val : ""; }
        }

        //reset minimal variables
        public void SoftReset()
        {
            progLogger.PurgeLog();
            progTapper.ResetCache();
            progFilr.ResetCache();

            progSeqAction = "";
            progDBAction = "";
            progFileAction = "";
            progSQL = "";
            progSQLWhere = "";
            progLogKill = 0;

            dbServer = "";
            dbName = "";
            dbAuthType = "";
            dbUsername = "";
            dbPassword = "";
            dbConnString = "";

            repoPath = "";
            repo2Path = "";
            cOutPath = "";
            cInPath = "";
            cInData = "";
        }

        //reset everthing
        public void HardReset()
        {
            SoftReset();
            progPath = "";
            progLogPath = "";
        }

        // rudimentary accessors
        public Logger Log
        {
            get { return progLogger; }
            set { progLogger = value; }
        }
        public ManaTap Tapper
        {
            get { return progTapper; }
            set { progTapper = value; }
        }
        public ManaFile Filr
        {
            get { return progFilr; }
            set { progFilr = value; }
        }
        public string ExePath
        {
            get { return progPath; }
            set { progPath = value; }
        }
        public string LogPath
        {
            get { return progLogPath; }
            set { progLogPath = value; }
        }
        public string LogSuffix
        {
            get { return progLogSuffix; }
            set { progLogSuffix = value; }
        }
        public int LogKill
        {
            get { return progLogKill; }
        }
        public string SeqAction
        {
            get { return progSeqAction; }
            set { progSeqAction = value; }
        }
        public string DBAction
        {
            get { return progDBAction; }
            set { progDBAction = value; }
        }
        public string ObjType
        {
            get { return DBAction.Substring(Math.Max(0, DBAction.Length - 3)); }
        }
        public string FileAction
        {
            get { return progFileAction; }
            set { progFileAction = value; }
        }
        public string SQL
        {
            get { return progSQL; }
            set { progSQL = value; }
        }
        public string SQLWhere
        {
            get { return progSQLWhere; }
            set { progSQLWhere = value; }
        }
        public string Server
        {
            get { return dbServer; }
            set { dbServer = value; }
        }
        public string Database
        {
            get { return dbName; }
            set { dbName = value; }
        }
        public string AuthType
        {
            get { return dbAuthType; }
            set { dbAuthType = value; }
        }
        public string AuthString
        {
            get { return dbConnString; }
            set
            {
                dbConnString = value;

                //deduce and repopulate the variables
                foreach (string pair in value.Split(';'))
                {
                    string[] breaker = pair.Split('=');
                    if (breaker[0] == "Data Source")
                    {
                        string[] fsdelimited = breaker[1].Split('\\');
                        if (fsdelimited.Length > 0) Server = fsdelimited[0];
                        if (fsdelimited.Length > 1) Database = fsdelimited[1];
                    }
                    else if (breaker[0] == "Server")
                    {
                        Server = Server == "" ? breaker[1] : Server;
                    }
                    else if (breaker[0] == "Database" || breaker[0] == "Initial Catalog")
                    {
                        Database = Database == "" ? breaker[1] : Database;
                    }
                }
            }
        }
        public string Username
        {
            get { return dbUsername; }
            set { dbUsername = value; }
        }
        public string Password
        {
            get { return dbPassword; }
            set { dbPassword = value; }
        }
        public string RepoPath
        {
            get { return repoPath; }
            set { repoPath = value; }
        }
        public string Repo2Path
        {
            get { return repo2Path; }
            set { repo2Path = value; }
        }
        public string OutPath
        {
            get { return cOutPath; }
            set { cOutPath = value; }
        }
        public string InPath
        {
            get { return cInPath; }
            set { cInPath = value; }
        }
        public string InData
        {
            get { return cInData; }
            set { cInData = value; }
        }
    }
}

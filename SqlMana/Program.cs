using System;
using System.IO;

namespace SqlMana
{
    class Program
    {
        private static Config config;

        static int Main(string[] args)
        {
            int status = 1;

            // Begin: Singletons across Runtime
            config = new Config();
            new Logger(config);
            new ManaTap(config);
            new ManaFile(config);
            // End  : Singletons across Runtime

            if (args.Length <= 0)
            {
                Console.WriteLine("[Main] Undefined input parameters");
                return -1;
            }

            // eliminate case sensitivity
            string progType = args[0].ToLower();
            if (progType == "file" || progType == "files")
            {
                status = ProgPrettify(ProgConfigFiles, args);
            }
            else if (progType == "data")
            {
                status = ProgConfigData(args);
            }
            else
            {
                status = -1;
                Console.WriteLine("[Main] Unsupported program type");
            }
            Environment.Exit(status);
            return status;
        }

        private static int ProgPrettify(Func<string[], int> theProg, string[] args)
        {
            int status = 1;
            //begin process
            Console.WriteLine("[Main] ------------------ Begin process ------------------");
            //exe process
            if (theProg(args) < 0)
            {
                return -1;
            }
            //post process
            Console.WriteLine("");
            Console.WriteLine("[Main] ------------------ End process ------------------");

            //Console.WriteLine("[Exit] L key: Explore logs & exit");
            //Console.WriteLine("[Exit] Any key: Exit");
            //string option = Console.ReadLine();
            //if (option == "L" || option == "l") ProgPost();

            return status;
        }

        private static int ProgConfigData(string[] args)
        {
            int status = 1;
            config.SoftReset();
            config.ConfigData(args);
            // run sequence of actions
            if (RunSeqAction() < 0) status = -1;
            config.Log.WriteLog();
            return status;
        }

        private static int ProgConfigFiles(string[] args)
        {
            // sanitise arguments first
            int status = 1;
            for (int i = 1; i < args.Length; i++)
            {
                if (!File.Exists(args[i]))
                {
                    Console.WriteLine("[Main] Cannot find path: " + args[i]);
                    status = -1;
                    break;
                }
            }
            if (status < 0)
            {
                return status;
            }

            // pass arguments and run process
            for (int j = 1; j < args.Length; j++)
            {
                Console.WriteLine("");
                Console.WriteLine("[Main] Run configuration in file: " + args[j]);
                if (RunConfigFile(args[j]) < 0)
                {
                    status = -1;
                    break;
                }
            }
            return status;
        }

        private static int RunConfigFile(string file)
        {
            int status = 1;
            // setup configuration
            config.SoftReset();
            config.ConfigFromFile(file);
            // request for database credentials
            RunReqCredentials();
            // run sequence of actions
            if (RunSeqAction() < 0) status = -1;
            // write logs
            if (status < 0)
            {
                config.Log.AppendLog("Job completed with errors");
            }
            else
            {
                config.Log.AppendLog("Job completed successfully");
            }
            config.Log.WriteLog();

            return status;
        }

        private static void RunReqCredentials()
        {
            string temp;

            if (config.Server != "" && config.AuthType == "serverauth")
            {
                temp = "";
                Console.WriteLine(string.Format(@"[Main] Credentials to access {0}\{1}", config.Server, config.Database));
                while (temp == "")
                {
                    Console.Write("Username: ");
                    temp = Console.ReadLine();
                }
                config.Username = temp;

                temp = "";
                while (temp == "")
                {
                    Console.Write("Password: ");
                    temp = Console.ReadLine();
                }
                config.Password = temp;
            }
        }

        private static int RunSeqAction()
        {
            string[] supportedSeq = { "file", "db" };
            int status = 1;
            int statusFatal = 1;

            if (config.SeqAction != "")
            {
                // 1. sanitise all terms
                foreach (string seq in config.SeqAction.Split(','))
                {
                    status = -1;
                    foreach (string supported in supportedSeq)
                    {
                        if (seq == supported) status = 1;
                    }
                    if (status < 0)
                    {
                        return status;
                    }
                }

                //2. operate base on terms
                foreach (string seq in config.SeqAction.Split(','))
                {
                    if (seq == "db")
                    {
                        statusFatal = config.Tapper.EstConnection() < 0 ? -1 : statusFatal;
                        status = config.Tapper.DoDBAction() < 0 ? -1 : status;
                        statusFatal = config.Tapper.KillConnection() < 0 ? -1 : statusFatal;
                    }
                    else if (seq == "file")
                    {
                        status = config.Filr.DoFileAction();
                    }
                    if (statusFatal < 0)
                    {
                        status = statusFatal;
                        break;
                    }
                }
            }
            return status;
        }

        private static void ProgPost()
        {
            // Review log in directory
            if (config.LogPath != "")
            {
                ManaProcess.runExe("explorer", config.LogPath, false);
            }
        }
    }
}

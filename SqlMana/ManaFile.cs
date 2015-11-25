using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace SqlMana
{
    class ManaFile
    {
        private Config c;
        public List<SSP> sspCache;

        public ManaFile(Config origC)
        {
            sspCache = new List<SSP>();
            c = origC;
            c.Filr = this;
        }

        public void ResetCache()
        {
            sspCache.Clear();
        }

        public string GetSourceDirectory()
        {
            string temp = "";
            if (c.RepoPath.Length > 0)
            {
                temp = c.RepoPath;
            }
            else
            {
                temp = string.Format(@"{0}\{1}", c.ExePath, "SSP");
            }
            return temp;
        }

        public void BuildDirectory()
        {
            Directory.CreateDirectory(GetSourceDirectory());
        }

        // referenced from https://support.microsoft.com/en-us/kb/320348
        private bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2, FileMode.Open);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        public int DoFileAction()
        {
            int status = 1;
            if (c.FileAction == "writeSSP")
            {
                DataTable temp = c.Tapper.GetDataTable();
                string sspFilePath = "";
                string sspFileAction = "";

                if (c.Tapper.GetRowsAffected() <= 0)
                {
                    c.Log.AppendLog("[Filr] No SSP to write");
                    return 0;
                }

                int counter = 1;
                BuildDirectory();
                c.Log.AppendLog("[Filr] Initiating SSP writing: " + GetSourceDirectory());

                for (int i = 0; i < sspCache.Count; i++)
                {
                    sspFilePath = string.Format(@"{0}\{1}.sql", GetSourceDirectory(), sspCache[i].sspName);
                    sspFileAction = File.Exists(sspFilePath) ? "Overwrite" : "Written";

                    //write to physical file
                    using (StreamWriter file = new StreamWriter(sspFilePath, false))
                    {
                        file.WriteLine(sspCache[i].sspDef);
                        file.Close();
                    }
                    c.Log.AppendLog(string.Format(@"[Filr] {2}: {0}. {1}", counter, sspCache[i].sspName, sspFileAction));
                    counter = counter + 1;
                }

                //foreach (DataRow row in temp.Rows)
                //{
                //    sspName = row["ROUTINE_NAME"].ToString();
                //    sspDef = row["ROUTINE_DEFINITION"].ToString();
                //    sspFilePath = string.Format(@"{0}\{1}.sql", GetSourceDirectory(), sspName);
                //    sspFileAction = File.Exists(sspFilePath) ? "Overwrite" : "Written";

                //    //write to physical file
                //    using (StreamWriter file = new StreamWriter(sspFilePath, false))
                //    {
                //        file.WriteLine(sspDef);
                //        file.Close();
                //    }
                //    c.Log.AppendLog(string.Format(@"[Filr] {2}: {0}. {1}", counter, sspName, sspFileAction));
                //    counter = counter + 1;
                //}

                c.Log.AppendLog("[Filr] Done SSP writing: " + GetSourceDirectory() + " files");
            }
            else if (c.FileAction == "readSSP")
            {
                string diffFile;
                string filePath;
                int counter = 0;

                // read straight from string 
                if (c.InPath.Length <= 0)
                {
                    c.Log.AppendLog("[Filr] Undefined InPath (file with SSP to read)");
                    if (c.InData == "")
                    {
                        c.Log.AppendLog("[Filr] InData NOT exists");
                        return -1;
                    }
                    c.Log.AppendLog("[Filr] InData exists proceed to reading SSP");
                    foreach (string targetSSP in c.InData.Split(','))
                    {
                        diffFile = targetSSP.Trim();
                        if (diffFile != "")
                        {
                            filePath = string.Format(@"{0}\{1}", c.RepoPath, diffFile);

                            if (!File.Exists(filePath))
                            {
                                c.Log.AppendLog("[Filr] File not exists: " + filePath);
                                break;
                            }

                            if (diffFile.Split('.').Length != 2)
                            {
                                c.Log.AppendLog(string.Format(
                                    @"[Filr] Incorrect entry in line {0}, file {1}"
                                    , counter + 1
                                    , c.InPath));
                                break;
                            }

                            SSP temp = new SSP();
                            temp.sspDef = new StreamReader(filePath).ReadToEnd();
                            temp.sspFilePath = diffFile;
                            temp.sspName = diffFile.Split('.')[0];
                            sspCache.Add(temp);
                            counter++;
                        }
                    }
                }

                // read sproc from file
                else
                {
                    c.Log.AppendLog("[Filr] Initiating SSP reading: " + GetSourceDirectory());
                    try
                    {
                        if (!File.Exists(c.InPath))
                        {
                            c.Log.AppendLog("[Filr] File not exists: " + c.InPath);
                            return 0;
                        }
                        using (StreamReader file = new StreamReader(c.InPath))
                        {
                            while ((diffFile = file.ReadLine()) != null)
                            {
                                if (diffFile != "")
                                {
                                    filePath = string.Format(@"{0}\{1}", c.RepoPath, diffFile);
                                    if (!File.Exists(filePath))
                                    {
                                        c.Log.AppendLog("[Filr] File not exists: " + filePath);
                                        break;
                                    }
                                    if (diffFile.Split('.').Length != 2)
                                    {
                                        c.Log.AppendLog(string.Format(
                                            @"[Filr] Incorrect entry in line {0}, file {1}"
                                            , counter + 1
                                            , c.InPath));
                                        break;
                                    }

                                    SSP temp = new SSP();
                                    temp.sspDef = new StreamReader(filePath).ReadToEnd();
                                    temp.sspFilePath = diffFile;
                                    temp.sspName = diffFile.Split('.')[0];
                                    sspCache.Add(temp);
                                    counter++;
                                }
                            }
                            file.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        c.Log.AppendLog("[Filr] Error in reading file(s)");
                        c.Log.AppendLog(e.ToString());
                        status = -1;
                    }
                    c.Log.AppendLog("[Filr] Done SSP reading: " + counter + " files");
                }
            }
            else if (c.FileAction == "compareSSP")
            {
                if (c.RepoPath.Length <= 0 || c.Repo2Path.Length <= 0)
                {
                    c.Log.AppendLog("[Filr] Undefined RepoPath (source) or Repo2Path (target)");
                    return 0;
                }

                string filename = "";
                List<string> messages = new List<string>();
                List<string> missings = new List<string>();
                List<string> justnames = new List<string>();
                string temp = "";

                c.Log.AppendLog(string.Format(
                    @"[Filr] Initiating SSP comparison: {0} (Base) {1} (Target)"
                    , c.RepoPath
                    , c.Repo2Path));

                foreach (string filePath in Directory.GetFiles(c.RepoPath))
                {
                    filename = Path.GetFileName(filePath);
                    string filePath2 = string.Format(@"{0}\{1}", c.Repo2Path, filename);

                    //checking inconsistent existance of files
                    if (!File.Exists(filePath2))
                    {
                        temp = "[Filr] Extra file in base: " + filename;
                        missings.Add(temp);
                        continue;
                    }
                    //diff file 
                    if (!FileCompare(filePath, filePath2))
                    {
                        temp = "[Filr] Diff: " + filename;
                        messages.Add(temp);
                        justnames.Add(filename);
                    }
                }
                foreach (string filePath2 in Directory.GetFiles(c.Repo2Path))
                {
                    filename = Path.GetFileName(filePath2);
                    string filePath = string.Format(@"{0}\{1}", c.RepoPath, filename);

                    //checking inconsistent existance of files
                    if (!File.Exists(filePath))
                    {
                        temp = "[Filr] Extra file in target: " + filename;
                        missings.Add(temp);
                        continue;
                    }
                }

                List<string> total = new List<string>(messages.Count + missings.Count);
                total.AddRange(missings);
                total.AddRange(messages);
                foreach (string entry in total)
                {
                    c.Log.AppendLog(entry);
                    //Console.WriteLine(entry);
                }
                if (missings.Count > 0) c.Log.AppendLog(string.Format("[Filr] Total of {0} extra SSP in source", missings.Count));
                if (messages.Count > 0) c.Log.AppendLog(string.Format("[Filr] Total of {0} diff SSP in source", messages.Count));


                if (c.OutPath.Length > 0)
                {
                    temp = "";
                    foreach (string entry in justnames)
                    {
                        temp = temp + string.Format(@"{0} {1}", entry, Environment.NewLine);
                    }
                    File.WriteAllText(c.OutPath, temp);
                }
            }
            return status;
        }
    }
}

using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace SqlMana
{
    class ManaTap
    {
        private Config c;
        private SqlConnection connection;
        private SqlDataReader reader;
        private DataTable dt;
        private int rowsAffected;
        private int countSSP;

        public ManaTap(Config origC)
        {
            c = origC;
            c.Tapper = this;
            reader = null;
            dt = new DataTable();
            rowsAffected = -1;
        }

        public DataTable GetDataTable()
        {
            return dt;
        }

        public int GetRowsAffected()
        {
            return rowsAffected;
        }

        public void ResetCache()
        {
            dt.Clear();
            rowsAffected = -1;
        }

        public string BuildAuthString()
        {
            string auth = "";

            if (c.AuthType == "winauth")
            {
                auth = string.Format(
               "Server={0};Database={1};Integrated Security=True;"
                , c.Server
                , c.Database
                );
            }
            else if (c.AuthType == "serverauth")
            {
                auth = string.Format(
                "Server={0};Database={1};uid={2};pwd={3};Integrated Security=False;"
                , c.Server
                , c.Database
                , c.Username
                , c.Password
                );
            }
            else
            {
                auth = c.AuthString;
            }
            return auth;
        }

        public int EstConnection()
        {
            int status = 1;
            if (c.DBAction == "")
            {
                return 0;
            }
            try
            {
                c.Log.AppendLog("[Connection] Opening: " + BuildAuthString());
                connection = new SqlConnection(BuildAuthString());
                connection.Open();
            }
            catch (Exception e)
            {
                c.Log.AppendLog("[Connection] Error in opening");
                c.Log.AppendLog(e.ToString());
                status = -1;
            }

            if (connection.State == ConnectionState.Open)
            {
                c.Log.AppendLog("[Connection] Opened");
            }
            else
            {
                c.Log.AppendLog("[Connection] Failed to open connection");
                status = -1;
            }
            return status;
        }

        public int KillConnection()
        {
            int status = 1;
            if (c.DBAction == "")
            {
                return 0;
            }
            try
            {
                connection.Close();
            }
            catch (Exception e)
            {
                c.Log.AppendLog("[Connection] Error in closing");
                c.Log.AppendLog(e.ToString());
                status = -1;
            }

            if (connection.State == ConnectionState.Closed)
            {
                c.Log.AppendLog("[Connection] Closed connection");
            }
            else
            {
                c.Log.AppendLog("[Connection] Failed to close connection");
                status = -1;
            }
            return status;
        }

        public List<string> BuildSQLString()
        {
            List<string> sqlString = new List<string>();

            // forming the where clause 
            string temp = "";
            if (c.DBAction == "selectSSP" || c.DBAction == "selectFNS" || c.DBAction == "selectFNT")
            {
                // forming the where clause
                if (c.DBAction == "selectFNT") temp = ManaStore.StealFNTable;
                else if (c.DBAction == "selectFNS") temp = ManaStore.StealFNScalar;
                else temp = ManaStore.Steal;

                if (c.SQLWhere.Length > 0)
                {
                    string targetSSPString = "";
                    foreach (string targetSSP in c.SQLWhere.Split(','))
                    {
                        targetSSPString = targetSSPString + string.Format(@",'{0}'", targetSSP);
                    }
                    temp = string.Format(ManaStore.StealWhere, temp, targetSSPString.Substring(1));
                }
                sqlString.Add(temp);
            }
            else if (c.DBAction == "updateSSP" || c.DBAction == "updateFNT" || c.DBAction == "updateFNS")
            {
                if (SSP.currIndex >= c.Filr.sspCache.Count)
                {
                    c.Log.AppendLog("[SQL string] exceeded index");
                    return sqlString;
                }

                // forming the where clause
                if (c.DBAction == "updateFNT") temp = ManaStore.PoisonFNT;
                else if (c.DBAction == "updateFNS") temp = ManaStore.PoisonFNS;
                else temp = ManaStore.PoisonSSP;

                SSP tempSSP = c.Filr.sspCache[SSP.currIndex];
                sqlString.Add(string.Format(
                    ManaStore.Poison
                    , tempSSP.sspName
                    , temp
                    ));
                sqlString.Add(string.Format(
                    ManaStore.Healing
                    , tempSSP.sspDef
                    ));
                sqlString.Add(string.Format(
                    ManaStore.LevelUp
                    , tempSSP.sspName
                    ));
            }
            else
            {
                sqlString.Add(c.SQL);
            }

            return sqlString;
        }

        public int DoDBAction()
        {
            int status = 1;
            List<string> SQLStore;
            List<string> tempDisplay = new List<string>();
            string logHead = string.Format("[SQL {0}]", c.DBAction);
            string objType = c.ObjType;

            if (c.DBAction == "selectSSP" || c.DBAction == "selectFNS" || c.DBAction == "selectFNT")
            {
                rowsAffected = 0;
                SQLStore = BuildSQLString();
                // only 1 SQL in SSP selection
                if (SQLStore[0].Length == 0)
                {
                    c.Log.AppendLog(string.Format("{0} Undefined SQL string", logHead));
                    return 0;
                }
                try
                {
                    using (SqlCommand cmd = new SqlCommand(SQLStore[0], connection))
                    {
                        c.Log.AppendLog(string.Format("{0} Read start: {1}", logHead, SQLStore[0]));
                        reader = cmd.ExecuteReader();
                        c.Log.AppendLog(string.Format("{0} Read end", logHead));

                        dt.Load(reader);

                        rowsAffected = rowsAffected + dt.Rows.Count;
                        c.Log.AppendLog(string.Format("{0} Loaded datastore: {1} rows", logHead, dt.Rows.Count));
                    }
                }
                catch (Exception e)
                {
                    c.Log.AppendLog(string.Format("{0} Read error: {1}", logHead, SQLStore[0]));
                    c.Log.AppendLog(e.ToString());
                    status = -1;
                }

                //storing into cache, SSP + FNs
                foreach (DataRow row in dt.Rows)
                {
                    //write to physical file
                    SSP temp = new SSP();
                    temp.sspName = row["ROUTINE_NAME"].ToString();
                    temp.sspDef = row["ROUTINE_DEFINITION"].ToString();
                    temp.sspFilePath = "";
                    c.Filr.sspCache.Add(temp);
                }

                //TODO: 
                //Extract portions for custom sql
                //remember to clear cache when soft/hard reset
            }
            else if (c.DBAction == "updateSSP" || c.DBAction == "updateFNS" || c.DBAction == "updateFNT")
            {
                if (c.Filr.sspCache.Count <= 0)
                {
                    c.Log.AppendLog(string.Format("{0} Undefined SSP cache, Exit", logHead));
                    return 0;
                }

                //reset variables
                SSP.currIndex = 0;
                countSSP = 0;
                rowsAffected = 0;
                SSP temp;

                //start looping cache
                c.Log.AppendLog(string.Format("{0} Initiating SSP update procedure", logHead));
                for (int i = 0; i < c.Filr.sspCache.Count; i++)
                {
                    SSP.currIndex = i;
                    temp = c.Filr.sspCache[SSP.currIndex];
                    SQLStore = BuildSQLString();

                    if (SQLStore[1].Length == 0)
                    {
                        c.Log.AppendLog(string.Format("{0} Undefined SSP content: {1}", logHead, temp.sspName));
                        continue;
                    }

                    tempDisplay.Add(string.Format("New {0}:", objType));
                    tempDisplay.Add(string.Format("Detected existing {0}:", objType));
                    tempDisplay.Add(string.Format("Created {0}:", objType));
                    tempDisplay.Add(string.Format("Updated {0}:", objType));

                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(SQLStore[0], connection))
                        using (SqlCommand cmd2 = new SqlCommand())
                        using (SqlCommand cmd3 = new SqlCommand(SQLStore[2], connection))
                        {
                            // checking existance of SSP
                            countSSP = (int)cmd.ExecuteScalar();
                            c.Log.AppendLog(string.Format("{0} {1} {2}.{3}"
                                , logHead
                                , (countSSP == 0 ? tempDisplay[0] : tempDisplay[1])
                                , c.Database
                                , temp.sspName)
                            );

                            // decide create/ alter SSP
                            cmd2.CommandText = ManaStore.Swap(
                                SQLStore[1]
                                , countSSP > 0
                                , objType == "SSP" ? "PROCEDURE" : "FUNCTION"
                            );
                            cmd2.Connection = connection;
                            cmd2.ExecuteNonQuery();
                            c.Log.AppendLog(string.Format("{0} {1} {2}.{3}"
                               , logHead
                               , (countSSP == 0 ? tempDisplay[2] : tempDisplay[3])
                               , c.Database
                               , temp.sspName)
                            );

                            cmd3.ExecuteNonQuery();
                            c.Log.AppendLog(string.Format("{0} Marked {1} (sp_recompile): {2}.{3}"
                                , logHead
                                , objType
                                , c.Database
                                , temp.sspName)
                            );
                            rowsAffected = rowsAffected + 1;
                        }
                    }
                    catch (Exception e)
                    {
                        c.Log.AppendLog(string.Format(@"{0} Error: {1} {{{2}}}", logHead, objType, temp.sspName));
                        c.Log.AppendLog(e.ToString());

                        if (status > 0) status = -1;
                        else status = status - 1;

                    }
                }
                c.Log.AppendLog(string.Format("{0} Updated {1} entries of {2}", logHead, rowsAffected, objType));
            }
            //TODO: Addon/ enable other SQL statements
            else
            {
                c.Log.AppendLog(string.Format("{0} No Action", logHead));
            }
            return status;
        }
    }
}

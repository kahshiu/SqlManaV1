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

            if (c.DBAction == "selectSSP")
            {
                string temp = "";
                temp = ManaStore.Steal;
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
            else if (c.DBAction == "updateSSP")
            {
                if (SSP.currIndex >= c.Filr.sspCache.Count)
                {
                    c.Log.AppendLog("[SQL string] exceeded index");
                    return sqlString;
                }
                SSP tempSSP = c.Filr.sspCache[SSP.currIndex];
                sqlString.Add(string.Format(
                    ManaStore.Poison
                    , tempSSP.sspName
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

            if (c.DBAction == "selectSSP")
            {
                rowsAffected = 0;
                SQLStore = BuildSQLString();
                // only 1 SQL in SSP selection
                if (SQLStore[0].Length == 0)
                {
                    c.Log.AppendLog("[SQL selectSSP] Undefined SQL string");
                    return 0;
                }
                try
                {
                    using (SqlCommand cmd = new SqlCommand(SQLStore[0], connection))
                    {
                        c.Log.AppendLog("[SQL selectSSP] Read start: " + SQLStore[0]);
                        reader = cmd.ExecuteReader();
                        c.Log.AppendLog("[SQL selectSSP] Read end");

                        dt.Load(reader);

                        rowsAffected = rowsAffected + dt.Rows.Count;
                        c.Log.AppendLog(string.Format("[SQL selectSSP] Loaded datastore: {0} rows", dt.Rows.Count));
                    }
                }
                catch (Exception e)
                {
                    c.Log.AppendLog("[SQL selectSSP] Read error: " + SQLStore[0]);
                    c.Log.AppendLog(e.ToString());
                    status = -1;
                }

                //storing into cache
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
            else if (c.DBAction == "updateSSP")
            {
                if (c.Filr.sspCache.Count <= 0)
                {
                    c.Log.AppendLog("[SQL updateSSP] Undefined SSP cache, Exit");
                    return 0;
                }

                //reset variables
                SSP.currIndex = 0;
                rowsAffected = 0;
                SSP temp;

                //start looping cache
                c.Log.AppendLog("[SQL updateSSP] Initiating SSP update procedure");
                for (int i = 0; i < c.Filr.sspCache.Count; i++)
                {
                    SSP.currIndex = i;
                    temp = c.Filr.sspCache[SSP.currIndex];
                    SQLStore = BuildSQLString();

                    if (SQLStore[1].Length == 0)
                    {
                        c.Log.AppendLog("[SQL updateSSP] Undefined SSP content: " + temp.sspName);
                        continue;
                    }

                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(SQLStore[0], connection))
                        using (SqlCommand cmd2 = new SqlCommand(SQLStore[1], connection))
                        using (SqlCommand cmd3 = new SqlCommand(SQLStore[2], connection))
                        {
                            cmd.ExecuteNonQuery();
                            c.Log.AppendLog("[SQL updateSSP] Dropped SSP: " + c.Database + "." + temp.sspName);

                            cmd2.ExecuteNonQuery();
                            c.Log.AppendLog("[SQL updateSSP] Updated SSP: " + c.Database + "." + temp.sspName);

                            cmd3.ExecuteNonQuery();
                            c.Log.AppendLog("[SQL updateSSP] Marked SSP (sp_recompile): " + c.Database + "." + temp.sspName);

                            rowsAffected = rowsAffected + 1;
                        }
                    }
                    catch (Exception e)
                    {
                        c.Log.AppendLog(string.Format(@"[SQL updateSSP] Error: SSP {{{0}}}", temp.sspName));
                        c.Log.AppendLog(e.ToString());

                        if (status > 0) status = -1;
                        else status = status - 1;

                    }
                }
                c.Log.AppendLog(string.Format("[SQL updateSSP] Updated {0} procedures", rowsAffected));
            }
            //TODO: Addon/ enable other SQL statements
            else
            {
                c.Log.AppendLog(string.Format("[SQL] No Action"));
            }
            return status;
        }
    }
}

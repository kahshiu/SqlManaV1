using System.Text.RegularExpressions;
namespace SqlMana
{
    class ManaStore
    {
        //-----------------------
        //Begin: Clauses for SSP Extraction
        //-----------------------
        public static string StealBase =
        @"SELECT 
            ROUTINE_NAME
            , OBJECT_DEFINITION(OBJECT_ID(ROUTINE_NAME)) as ROUTINE_DEFINITION 
        FROM 
            sys.objects O 
            LEFT JOIN sys.extended_properties E ON O.object_id = E.major_id
            INNER JOIN INFORMATION_SCHEMA.ROUTINES R
            on R.ROUTINE_NAME = O.name
		WHERE 
			O.name IS NOT NULL 
			AND ISNULL(O.is_ms_shipped,0) = 0
            AND ISNULL(E.name, '') <> 'microsoft_database_tools_support'";

        public static string StealObjType = @"{0} AND O.type_desc = '{1}'";
        public static string StealWhere = @"{0} AND ROUTINE_NAME in ({1})";
        public static string Steal = string.Format(StealObjType, StealBase, "SQL_STORED_PROCEDURE");
        public static string StealFNScalar = string.Format(StealObjType, StealBase, "SQL_SCALAR_FUNCTION");
        public static string StealFNTable = string.Format(StealObjType, StealBase, "SQL_TABLE_VALUED_FUNCTION");
        public static string LevelUp = @"EXEC sp_recompile N'{0}'";

        //-----------------------
        //End  : Clauses for SSP Extraction
        //-----------------------


        //-----------------------
        //Begin: Clauses for SSP Drop/ Restore (from file)
        //-----------------------
        public static string Poison =
        @"SELECT COUNT(*)
            FROM sys.objects
            WHERE object_id = OBJECT_ID(N'{0}')
            AND type IN ( N'P', N'PC' )";

        public static string Healing =
        @"{0}";

        public static string Swap(string SSPContent, bool Activate)
        {
            string temp = SSPContent;
            if(Activate)
            {
                temp = new Regex("CREATE PROCEDURE").Replace(temp,"ALTER PROCEDURE",1);
            }
            return temp;
        }
        //-----------------------
        //End  : Clauses for SSP Restore (from file)
        //-----------------------
    }
}

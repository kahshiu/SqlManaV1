namespace SqlMana
{
    class ManaStore
    {
        //-----------------------
        //Begin: Clauses for SSP Extraction
        //-----------------------
        public static string Steal =
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
            AND ISNULL(E.name, '') <> 'microsoft_database_tools_support'
			AND O.type_desc = 'SQL_STORED_PROCEDURE'";

        public static string StealWhere =
        @"{0} AND ROUTINE_NAME in ({1})";
        //-----------------------
        //End  : Clauses for SSP Extraction
        //-----------------------


        //-----------------------
        //Begin: Clauses for SSP Restore (from file)
        //-----------------------
        public static string Poison =
        @"IF EXISTS ( 
            SELECT *
            FROM sys.objects
            WHERE object_id = OBJECT_ID(N'{0}')
            AND type IN ( N'P', N'PC' ) 
        ) 
        drop procedure {0}";

        public static string Healing =
        @"{0}";
        //-----------------------
        //End  : Clauses for SSP Restore (from file)
        //-----------------------
    }
}

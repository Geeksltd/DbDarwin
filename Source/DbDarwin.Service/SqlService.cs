using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace DbDarwin.Service
{
    public class SqlService
    {
        public static List<T> LoadData<T>(SqlConnection connection, string tableName, string sqlScript) where T : class, new()
        {
            using (var da = new SqlDataAdapter())
            {
                da.SelectCommand = new SqlCommand { Connection = connection };
                var dataTable = new DataTable(tableName);
                da.SelectCommand.CommandText = sqlScript;
                da.Fill(dataTable);
                return dataTable.DataTableToList<T>();
            }
        }
        public static List<string> LoadDataAsString(SqlConnection connection, string tableName, string sqlScript)
        {
            using (var da = new SqlDataAdapter())
            {
                da.SelectCommand = new SqlCommand { Connection = connection };
                var dataTable = new DataTable(tableName);
                da.SelectCommand.CommandText = sqlScript;
                da.Fill(dataTable);
                return dataTable.DataTableToListAsString();
            }
        }
    }
}

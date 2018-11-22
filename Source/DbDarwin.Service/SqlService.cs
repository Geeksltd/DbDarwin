using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using GCop.Core;

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
        public static DataTable LoadData(SqlConnection connection, string tableName, string sqlScript)
        {
            using (var da = new SqlDataAdapter())
            {
                da.SelectCommand = new SqlCommand { Connection = connection };
                var dataTable = new DataTable(tableName);
                da.SelectCommand.CommandText = sqlScript;
                da.Fill(dataTable);
                return dataTable;
            }
        }
        public static List<dynamic> LoadDataDynamic(SqlConnection connection, string tableName, string sqlScript)
        {
            using (var da = new SqlDataAdapter())
            {
                da.SelectCommand = new SqlCommand { Connection = connection };
                var dataTable = new DataTable(tableName);
                da.SelectCommand.CommandText = sqlScript;
                da.Fill(dataTable);
                var dataResult = new List<dynamic>();
                foreach (DataRow row in dataTable.Rows)
                {
                    dynamic expando = new ExpandoObject();
                    foreach (DataColumn column in dataTable.Columns)
                        AddProperty(expando, column.ColumnName, row[column.ColumnName]);
                    dataResult.Add(expando);
                }
                return dataResult;
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

        public static T LoadFirstData<T>(SqlConnection connection, string sqlScript)
        {
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = sqlScript;
                var ob = cmd.ExecuteScalar();
                return (T)ob;
            }
        }

        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
    }
}

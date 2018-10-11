using DbDarwin.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Serialization;
using DbDarwin.Model.Schema;

namespace DbDarwin.Service
{
    public class ExtractSchemaService
    {
        /// <summary>
        /// extract table schema
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="fileOutput"></param>
        /// <returns></returns>
        public static bool ExtractSchema(string connectionString, string fileOutput)
        {
            // Create Connection to database
            try
            {
                System.Data.SqlClient.SqlConnection sql = new System.Data.SqlClient.SqlConnection(connectionString);
                List<DbDarwin.Model.Table> tables = new List<DbDarwin.Model.Table>();

                sql.Open();
                var allTables = sql.GetSchema("Tables");
                SqlDataAdapter da = new SqlDataAdapter
                {
                    SelectCommand = new SqlCommand
                    {
                        Connection = sql,
                    }
                };
                // Get All Columns Database
                DataTable allColumns = new DataTable("Columns");
                DataTable allIndex = new DataTable("Indexes");
                DataTable allSqlObjects = new DataTable("Objects");
                DataTable allReferences = new DataTable("References");

                /// fetch COLUMNS schema
                da.SelectCommand.CommandText = "select * from INFORMATION_SCHEMA.COLUMNS";
                da.Fill(allColumns);
                var columnsMapped = allColumns.DataTableToList<Column>();
                /// Fetch All Index from SQL
                da.SelectCommand.CommandText = "SELECT * FROM sys.indexes";
                da.Fill(allIndex);
                var indexMapped = allIndex.DataTableToList<Index>();

                /// Fetch All Objects from SQL
                da.SelectCommand.CommandText = "SELECT * FROM sys.objects";
                da.Fill(allSqlObjects);
                var objectMapped = allSqlObjects.DataTableToList<SqlObject>();

                /// Fetch All Refrences from SQL
                da.SelectCommand.CommandText = @"SELECT  
  RC.CONSTRAINT_SCHEMA,
  RC.CONSTRAINT_NAME,
  RC.UNIQUE_CONSTRAINT_NAME,
  RC.MATCH_OPTION,
  RC.UPDATE_RULE,
  RC.DELETE_RULE,

  KCU1.Table_Name,
  KCU1.COLUMN_NAME,
  KCU1.ORDINAL_POSITION,

  KCU2.TABLE_SCHEMA as Ref_TABLE_SCHEMA,
  KCU2.TABLE_NAME as Ref_TABLE_NAME,
  KCU2.COLUMN_NAME as Ref_COLUMN_NAME,
  KCU2.ORDINAL_POSITION as Ref_ORDINAL_POSITION

FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS RC 

INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU1 
    ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG  
    AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
    AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 

INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU2 
    ON KCU2.CONSTRAINT_CATALOG = RC.UNIQUE_CONSTRAINT_CATALOG  
    AND KCU2.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA 
    AND KCU2.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME 
    AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION ";
                da.Fill(allReferences);
                var referencesMapped = allReferences.DataTableToList<REFERENTIAL_CONSTRAINTS>();


                // Create Table Model
                foreach (DataRow r in allTables.Rows)
                {
                    var schema_Table = r["TABLE_SCHEMA"].ToString();
                    var tableName = r["TABLE_NAME"].ToString();
                    Console.WriteLine(schema_Table + "." + tableName);
                    var tableId = objectMapped.Where(x => x.name == tableName).Select(x => x.object_id).FirstOrDefault();

                    DbDarwin.Model.Table myDt = new DbDarwin.Model.Table()
                    {
                        Name = r["TABLE_NAME"].ToString(),
                        Column = columnsMapped.Where(x =>
                                x.TABLE_NAME == r["TABLE_NAME"].ToString() &&
                                x.TABLE_SCHEMA == r["TABLE_SCHEMA"].ToString())
                            .ToList(),
                        Index = indexMapped.Where(x => x.object_id == tableId).ToList(),
                        ForeignKey = referencesMapped.Where(x => x.CONSTRAINT_SCHEMA == schema_Table && x.TABLE_NAME == tableName).ToList()

                    };
                    tables.Add(myDt);
                }

                // Create Serialize Object and save as XML file
                var ser = new XmlSerializer(typeof(List<DbDarwin.Model.Table>));
                StringWriter sw2 = new StringWriter();
                ser.Serialize(sw2, tables);
                var xml = sw2.ToString();
                var path = AppDomain.CurrentDomain.BaseDirectory + "\\" + fileOutput;
                File.WriteAllText(path, xml);
                Console.WriteLine("Saving To xml");
                sql.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }
            finally
            {
                GC.Collect();
            }

            return true;
        }
    }
}

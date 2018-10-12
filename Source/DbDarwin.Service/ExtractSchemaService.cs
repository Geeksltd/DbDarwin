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
                DataTable allIndex_columns = new DataTable("index_columns");
                DataTable allSys_columns = new DataTable("sys.columns");



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


                /// Fetch All index_columns from SQL
                da.SelectCommand.CommandText = "SELECT * FROM sys.index_columns";
                da.Fill(allIndex_columns);
                var index_columnsMapped = allIndex_columns.DataTableToList<index_columns>();

                /// Fetch All sys.columns from SQL
                da.SelectCommand.CommandText = "SELECT * FROM sys.columns";
                da.Fill(allSys_columns);
                var system_columnsMapped = allSys_columns.DataTableToList<system_columns>();

                /// Fetch All Refrences from SQL
                da.SelectCommand.CommandText = Properties.Resources.REFERENTIAL_CONSTRAINTS;
                da.Fill(allReferences);
                var referencesMapped = allReferences.DataTableToList<REFERENTIAL_CONSTRAINTS>();


                // Create Table Model
                foreach (DataRow r in allTables.Rows)
                {
                    var schema_Table = r["TABLE_SCHEMA"].ToString();
                    var tableName = r["TABLE_NAME"].ToString();
                    Console.WriteLine(schema_Table + "." + tableName);
                    var tableId = objectMapped.Where(x => x.name == tableName).Select(x => x.object_id).FirstOrDefault();


                    var result = (from ind in indexMapped
                                  join ic in index_columnsMapped on new { ind.object_id, ind.index_id } equals new
                                  { ic.object_id, ic.index_id }
                                  join col in system_columnsMapped on new { ic.object_id, ic.column_id } equals new
                                  { col.object_id, col.column_id }
                                  where ind.object_id == tableId
                                  select new { ind, ic, col }).ToList().GroupBy(x => x.ind.name);

                    List<Index> ExistsIndex = new List<Index>();
                    foreach (var index in result)
                    {
                        var resultIndex = index.FirstOrDefault()?.ind;
                        if (resultIndex != null)
                            resultIndex.Columns = index.ToList().OrderBy(x => x.ic.key_ordinal).Select(x => x.col.name)
                                .Aggregate((x, y) => x + "|" + y).Trim('|');
                        ExistsIndex.Add(resultIndex);
                    }




                    DbDarwin.Model.Table myDt = new DbDarwin.Model.Table()
                    {
                        Name = r["TABLE_NAME"].ToString(),
                        Column = columnsMapped.Where(x =>
                                x.TABLE_NAME == r["TABLE_NAME"].ToString() &&
                                x.TABLE_SCHEMA == r["TABLE_SCHEMA"].ToString())
                            .ToList(),
                        Index = ExistsIndex,
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

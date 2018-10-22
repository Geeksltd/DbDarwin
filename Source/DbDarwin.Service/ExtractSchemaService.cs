using DbDarwin.Model.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DbDarwin.Service
{
    public class ExtractSchemaService
    {
        /// <summary>
        /// extract table schema
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="fileOutput">output file</param>
        /// <returns>can be successful it is true</returns>
        public static bool ExtractSchema(string connectionString, string fileOutput)
        {
            // Create Connection to database
            var tables = new List<DbDarwin.Model.Table>();
            try
            {
                using (var sql = new System.Data.SqlClient.SqlConnection(connectionString))
                {

                    sql.Open();
                    var allTables = sql.GetSchema("Tables");
                    var referencesMapped = new List<ForeignKey>();
                    var systemColumnsMapped = new List<SystemColumns>();
                    var indexColumnsMapped = new List<IndexColumns>();
                    var columnsMapped = new List<Column>();
                    var indexMapped = new List<Index>();
                    var objectMapped = new List<SqlObject>();

                    using (var da = new SqlDataAdapter())
                    {
                        da.SelectCommand = new SqlCommand { Connection = sql };

                        // Get All Columns Database
                        var allColumns = new DataTable("Columns");
                        var allIndex = new DataTable("Indexes");
                        var allSqlObjects = new DataTable("Objects");
                        var allReferences = new DataTable("References");
                        var allIndexColumns = new DataTable("index_columns");
                        var allSysColumns = new DataTable("sys.columns");

                        /// fetch COLUMNS schema
                        da.SelectCommand.CommandText = "select * from INFORMATION_SCHEMA.COLUMNS";
                        da.Fill(allColumns);
                        columnsMapped = allColumns.DataTableToList<Column>();
                        /// Fetch All Index from SQL
                        da.SelectCommand.CommandText = "SELECT * FROM sys.indexes";
                        da.Fill(allIndex);
                        indexMapped = allIndex.DataTableToList<Index>();

                        /// Fetch All Objects from SQL
                        da.SelectCommand.CommandText = "SELECT * FROM sys.objects";
                        da.Fill(allSqlObjects);
                        objectMapped = allSqlObjects.DataTableToList<SqlObject>();

                        /// Fetch All index_columns from SQL
                        da.SelectCommand.CommandText = "SELECT * FROM sys.index_columns";
                        da.Fill(allIndexColumns);
                        indexColumnsMapped = allIndexColumns.DataTableToList<IndexColumns>();

                        /// Fetch All sys.columns from SQL
                        da.SelectCommand.CommandText = "SELECT * FROM sys.columns";
                        da.Fill(allSysColumns);
                        systemColumnsMapped = allSysColumns.DataTableToList<SystemColumns>();

                        /// Fetch All Refrences from SQL
                        da.SelectCommand.CommandText = Properties.Resources.REFERENTIAL_CONSTRAINTS;
                        da.Fill(allReferences);
                        referencesMapped = allReferences.DataTableToList<ForeignKey>();
                    }
                    // Create Table Model
                    foreach (DataRow row in allTables.Rows)
                    {
                        var schemaTable = row["TABLE_SCHEMA"].ToString();
                        var tableName = row["TABLE_NAME"].ToString();
                        Console.WriteLine(schemaTable + @"." + tableName);
                        var tableId = objectMapped.Where(x => x.name == tableName).Select(x => x.object_id)
                            .FirstOrDefault();

                        var result = (from ind in indexMapped
                                      join ic in indexColumnsMapped on new { ind.object_id, ind.index_id } equals new
                                      { ic.object_id, ic.index_id }
                                      join col in systemColumnsMapped on new { ic.object_id, ic.column_id } equals new
                                      { col.object_id, col.column_id }
                                      where ind.object_id == tableId
                                      select new { ind, ic, col }).ToList().GroupBy(x => x.ind.name);

                        var existsIndex = new List<Index>();
                        foreach (var index in result)
                        {
                            var resultIndex = index.FirstOrDefault()?.ind;
                            if (resultIndex != null)
                                resultIndex.Columns = index.ToList().OrderBy(x => x.ic.key_ordinal)
                                    .Select(x => x.col.name)
                                    .Aggregate((x, y) => x + "|" + y).Trim('|');
                            existsIndex.Add(resultIndex);
                        }

                        var myDt = new DbDarwin.Model.Table
                        {
                            Name = row["TABLE_NAME"].ToString(),
                            Column = columnsMapped.Where(x =>
                                    x.TABLE_NAME == row["TABLE_NAME"].ToString() &&
                                    x.TABLE_SCHEMA == row["TABLE_SCHEMA"].ToString())
                                .ToList(),
                            Index = existsIndex,
                            ForeignKey = referencesMapped.Where(x =>
                                x.CONSTRAINT_SCHEMA == schemaTable && x.TABLE_NAME == tableName).ToList()

                        };
                        tables.Add(myDt);
                    }

                    // Create Serialize Object and save as XML file
                    SaveToFile(tables, fileOutput);
                }
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

        private static void SaveToFile(IEnumerable<Model.Table> tables, string fileOutput)
        {
            var ser = new XmlSerializer(typeof(List<DbDarwin.Model.Table>));
            var sw2 = new StringWriter();
            ser.Serialize(sw2, tables);
            var xml = sw2.ToString();
            var path = AppDomain.CurrentDomain.BaseDirectory + "\\" + fileOutput;
            File.WriteAllText(path, xml);
            Console.WriteLine("Saving To xml");
        }
    }
}
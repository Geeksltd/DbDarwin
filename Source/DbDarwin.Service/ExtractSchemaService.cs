using DbDarwin.Model.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using DbDarwin.Model.Command;

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
        public static bool ExtractSchema(ExtractSchema model)
        {

            // Create Connection to database
            var tables = new List<DbDarwin.Model.Table>();
            try
            {
                using (var sql = new System.Data.SqlClient.SqlConnection(model.ConnectionString))
                {

                    sql.Open();
                    var allTables = sql.GetSchema("Tables");

                    /// Fetch All Refrences from SQL
                    var referencesMapped = LoadData<ForeignKey>(sql, "References", Properties.Resources.REFERENTIAL_CONSTRAINTS);
                    /// Fetch All index_columns from SQL
                    var indexColumnsMapped = LoadData<IndexColumns>(sql, "index_columns", "SELECT * FROM sys.index_columns");
                    /// fetch COLUMNS schema
                    var columnsMapped = LoadData<Column>(sql, "Columns", "select * from INFORMATION_SCHEMA.COLUMNS");
                    /// Fetch All Index from SQL
                    var indexMapped = LoadData<Index>(sql, "index_columns", "SELECT * FROM sys.indexes");
                    /// Fetch All sys.columns from SQL
                    var systemColumnsMapped = LoadData<SystemColumns>(sql, "allSysColumns", "SELECT * FROM sys.columns");
                    /// Fetch All Objects from SQL
                    var objectMapped = LoadData<SqlObject>(sql, "sys.object", "SELECT * FROM sys.objects");


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
                    SaveToFile(tables, model.OutputFile);
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
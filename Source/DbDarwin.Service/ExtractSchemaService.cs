using DbDarwin.Model.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using DbDarwin.Model.Command;
using PowerMapper;

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
            var database = new Database();
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


                    var constraintInformation = (from ind in indexMapped
                                                 join ic in indexColumnsMapped on new { ind.object_id, ind.index_id } equals new
                                                 { ic.object_id, ic.index_id }
                                                 join col in systemColumnsMapped on new { ic.object_id, ic.column_id } equals new
                                                 { col.object_id, col.column_id }
                                                 select new ConstraintInformationModel { Index = ind, IndexColumn = ic, SystemColumn = col }).ToList();
                    // Create Table Model
                    if (allTables.Rows.Count > 0)
                        database.Tables = new List<Table>();
                    foreach (DataRow row in allTables.Rows)
                    {
                        var schemaTable = row["TABLE_SCHEMA"].ToString();
                        var tableName = row["TABLE_NAME"].ToString();
                        Console.WriteLine(schemaTable + @"." + tableName);
                        var tableId = objectMapped.Where(x => x.name == tableName).Select(x => x.object_id)
                            .FirstOrDefault();

                        var indexes = FetchIndexes(constraintInformation, tableId);
                        var primaryKey = FetchPrimary(constraintInformation, tableId);

                        var myDt = new DbDarwin.Model.Schema.Table
                        {
                            Name = row["TABLE_NAME"].ToString(),
                            Columns = columnsMapped.Where(x =>
                                    x.TABLE_NAME == row["TABLE_NAME"].ToString() &&
                                    x.TABLE_SCHEMA == row["TABLE_SCHEMA"].ToString())
                                .ToList(),
                            Indexes = indexes,
                            PrimaryKey = primaryKey,
                            ForeignKeys = referencesMapped.Where(x =>
                                x.CONSTRAINT_SCHEMA == schemaTable && x.TABLE_NAME == tableName).ToList()

                        };
                        database.Tables.Add(myDt);
                    }

                    database.Tables = database.Tables.OrderBy(x => x.Name).ToList();

                    // Create Serialize Object and save as XML file
                    SaveToFile(database, model.OutputFile);
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

        private static List<Index> FetchIndexes(List<ConstraintInformationModel> constraintInformation, int tableId)
        {
            var indexRows = constraintInformation
                .Where(x => x.Index.object_id == tableId && x.Index.is_primary_key == "False")
                .GroupBy(x => x.Index.name);
            var existsIndex = new List<Index>();
            foreach (var index in indexRows)
            {
                var resultIndex = index.FirstOrDefault()?.Index;
                if (resultIndex != null)
                    resultIndex.Columns = index.ToList().OrderBy(x => x.IndexColumn.key_ordinal)
                        .Select(x => x.SystemColumn.name)
                        .Aggregate((x, y) => x + "|" + y).Trim('|');
                existsIndex.Add(resultIndex);
            }

            return existsIndex;
        }

        private static PrimaryKey FetchPrimary(IEnumerable<ConstraintInformationModel> constraintInformation, int tableId)
        {
            var indexRows = constraintInformation
                .Where(x => x.Index.object_id == tableId && x.Index.is_primary_key == "True")
                .GroupBy(x => x.Index.name);
            var index = indexRows.FirstOrDefault();
            if (index != null)
            {
                var resultIndex = index.FirstOrDefault()?.Index;
                if (resultIndex != null)
                    resultIndex.Columns = index.ToList().OrderBy(x => x.IndexColumn.key_ordinal)
                        .Select(x => x.SystemColumn.name)
                        .Aggregate((x, y) => x + "|" + y).Trim('|');
                return resultIndex.MapTo<PrimaryKey>();
            }
            return null;
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

        private static void SaveToFile(Database database, string fileOutput)
        {
            var ser = new XmlSerializer(typeof(Database));
            var sw2 = new StringWriter();
            ser.Serialize(sw2, database);
            var xml = sw2.ToString();
            var path = AppDomain.CurrentDomain.BaseDirectory + "\\" + fileOutput;
            File.WriteAllText(path, xml);
            Console.WriteLine("Saving To xml");
        }
    }
}
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
                sql.Open();
                var dt = sql.GetSchema("Tables");
                SqlDataAdapter da = new SqlDataAdapter
                {
                    SelectCommand = new SqlCommand
                    {
                        Connection = sql,
                        CommandText = "select * from INFORMATION_SCHEMA.COLUMNS"
                    }
                };
                // Get All Columns Database
                DataTable allColumns = new DataTable("COLUMNS");
                da.Fill(allColumns);
                var columnsMaped = allColumns.DataTableToList<InformationSchemaColumns>();
                List<DbDarwin.Model.Table> tables = new List<DbDarwin.Model.Table>();
                // Create Table Model
                foreach (DataRow r in dt.Rows)
                {
                    Console.WriteLine(r["TABLE_SCHEMA"] + "." + r["TABLE_NAME"]);
                    DbDarwin.Model.Table myDt = new DbDarwin.Model.Table()
                    {
                        Name = r["TABLE_NAME"].ToString(),
                        Column = columnsMaped.Where(x =>
                                x.TABLE_NAME == r["TABLE_NAME"].ToString() &&
                                x.TABLE_SCHEMA == r["TABLE_SCHEMA"].ToString())
                            .ToList()
                    };
                    tables.Add(myDt);
                }

                // Create Serialize Object and save as XML file
                var ser = new XmlSerializer(typeof(List<DbDarwin.Model.Table>));
                StringWriter sw2 = new StringWriter();
                ser.Serialize(sw2, tables);
                var xml = sw2.ToString();
                var path = AppDomain.CurrentDomain.BaseDirectory + "\\" + fileOutput + ".xml";
                File.AppendAllText(path, xml);
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using DbDarwin.Schema;
using DbDarwin.SchemaXML;
using PowerMapper;

namespace DbDarwin
{

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var first = args.First();
                if (!string.IsNullOrEmpty(first) && first.ToLower() == "extract-schema")
                {
                    Console.WriteLine("Start Extract Schema...");
                    System.Data.SqlClient.SqlConnection sql = new System.Data.SqlClient.SqlConnection("Data Source=EPIPC;Initial Catalog=AdventureWorks2012;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
                    sql.Open();
                    var dt = sql.GetSchema("Tables");


                    SqlDataAdapter da = new SqlDataAdapter();
                    da.SelectCommand = new SqlCommand();
                    da.SelectCommand.Connection = sql;


                    da.SelectCommand.CommandText = "select * from INFORMATION_SCHEMA.COLUMNS";
                    DataTable dt3 = new DataTable("COLUMNS");
                    da.Fill(dt3);

                    var columns = dt3.DataTableToList<InformationSchemaColumns>();

                    List<SchemaXML.Table> tables = new List<Table>();
                    foreach (DataRow r in dt.Rows)
                    {
                        Console.WriteLine(r["TABLE_SCHEMA"] + "." + r["TABLE_NAME"]);
                        SchemaXML.Table myDt = new SchemaXML.Table()
                        {
                            Name = r["TABLE_NAME"].ToString(),
                            Column = columns.Where(x => x.TABLE_NAME == r["TABLE_NAME"].ToString() && x.TABLE_SCHEMA == r["TABLE_SCHEMA"].ToString()).ToList()
                        };
                        tables.Add(myDt);
                    }

                    var ser = new XmlSerializer(typeof(List<SchemaXML.Table>));
                    StringWriter sw2 = new StringWriter();
                    ser.Serialize(sw2, tables);
                    var xml = sw2.ToString();
                    var path = AppDomain.CurrentDomain.BaseDirectory + "\\schema" + DateTime.Now.Ticks + ".xml";
                    File.AppendAllText(path, xml);
                    Console.WriteLine("Saving To ");

                }
            }


            Console.Read();
        }
    }
}

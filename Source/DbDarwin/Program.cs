using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
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


                    foreach (DataRow r in dt.Rows)
                    {
                        Console.WriteLine(r["TABLE_NAME"]);
                        var tableName = r["TABLE_NAME"];

                        da.SelectCommand.CommandText = "select * FROM [Sales].[Store]";// + tableName;
                        DataTable dt1 = new DataTable("Store");
                        da.Fill(dt1);


                        List<SchemaXML.Column> c1Test = dt1.Columns.Cast<DataColumn>().ToList().MapTo<List<SchemaXML.Column>>();



                        DbDarwin.SchemaXML.Table myDt = new SchemaXML.Table()
                        {
                            Name = dt1.TableName,
                            Column = dt1.Columns.Cast<DataColumn>().ToList().MapTo<List<SchemaXML.Column>>()


                        };


                        var ser = new XmlSerializer(typeof(SchemaXML.Table));
                        StringWriter sw2 = new StringWriter();
                        ser.Serialize(sw2, myDt);
                        var xml = sw2.ToString();
                        File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\schema" + DateTime.Now.Ticks + ".xml", xml);
                        Console.WriteLine(xml);
                        //StringWriter sw = new StringWriter();
                        //dt1.WriteXml(sw, XmlWriteMode.DiffGram);
                        //var xml = sw.ToString();
                        //Console.WriteLine(xml);
                        //File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\schema" + DateTime.Now.Ticks + ".xml", xml);
                        break;
                    }

                    //MemoryStream ms = new MemoryStream();
                    //dt.WriteXml(ms, XmlWriteMode.IgnoreSchema);
                    //ms.Seek(0, SeekOrigin.Begin);
                    //StreamReader sr = new StreamReader(ms);
                    //string xml = sr.ReadToEnd();
                    //ms.Close();

                    //     File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\schema" + DateTime.Now.Ticks + ".xml", xml);

                    //Data Source=EPIPC;Initial Catalog=MSharp.Mvc2.Temp;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False
                }
            }


            Console.Read();
        }
    }
}

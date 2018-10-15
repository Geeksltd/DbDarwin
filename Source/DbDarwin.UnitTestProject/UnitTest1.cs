using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using DbDarwin.Model;
using DbDarwin.Model.Schema;
using DbDarwin.Service;
using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbDarwin.UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ExtractSchema()
        {
            ExtractSchemaService.ExtractSchema(@"Data Source=EPIPC;Initial Catalog=Test3;Integrated Security=True;Connect Timeout=30",
                "xml2.xml");

        }

        [TestMethod]
        public void GenerateDiff()
        {
            CompareSchemaService.StartCompare(
                AppDomain.CurrentDomain.BaseDirectory + "\\" + "xml1.xml",
                AppDomain.CurrentDomain.BaseDirectory + "\\" + "xml2.xml",
                AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml");

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GenerateScripts()
        {

            XmlSerializer serializer = new XmlSerializer(typeof(List<Table>));
            List<Table> diffFile = null;
            using (var reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml"))
                diffFile = (List<Table>)serializer.Deserialize(reader);


            StringBuilder sb = new StringBuilder();

            sb.AppendLine("BEGIN TRANSACTION");
            sb.AppendLine("SET QUOTED_IDENTIFIER ON");
            sb.AppendLine("SET ARITHABORT ON");
            sb.AppendLine("SET NUMERIC_ROUNDABORT OFF");
            sb.AppendLine("SET CONCAT_NULL_YIELDS_NULL ON");
            sb.AppendLine("SET ANSI_NULLS ON");
            sb.AppendLine("SET ANSI_PADDING ON");
            sb.AppendLine("SET ANSI_WARNINGS ON");
            sb.AppendLine("COMMIT");
            sb.AppendLine("BEGIN TRANSACTION ");

            //      GO
            //    --GO
            //    --ALTER TABLE dbo.Table_1 SET (LOCK_ESCALATION = TABLE)
            //GO
            //    COMMIT
            //"

            if (diffFile != null)
            {
                foreach (var table in diffFile)
                {
                    if (table.Add != null)
                    {

                        sb.AppendLine("GO");
                        sb.AppendFormat("ALTER TABLE {0} ADD ", table.Name);

                        var columns = table.Add.Column;
                        sb.AppendLine();
                        for (var index = 0; index < columns.Count; index++)
                        {
                            sb.Append("\t");
                            var column = columns[index];
                            sb.AppendFormat("{0} {1}{2} {3}", column.COLUMN_NAME, column.DATA_TYPE,
                                string.IsNullOrEmpty(column.CHARACTER_MAXIMUM_LENGTH)
                                    ? ""
                                    : "(" + column.CHARACTER_MAXIMUM_LENGTH + ")",
                                column.IS_NULLABLE == "NO" ? "NOT NULL" : "NULL");
                            if (columns.Count > 1 && index < columns.Count - 1)
                                sb.AppendLine(",");

                        }
                        sb.AppendLine();
                        sb.AppendLine("GO");
                        if (columns.Count > 0)
                        {
                            sb.Append($"ALTER TABLE {table.Name} SET (LOCK_ESCALATION = TABLE)");
                            sb.AppendLine();
                            sb.AppendLine("GO");
                        }
                    }
                }
            }
            sb.AppendLine("COMMIT");

            File.WriteAllText(AppContext.BaseDirectory + "\\output.sql", sb.ToString());

            Assert.IsTrue(true);
        }

    }
}

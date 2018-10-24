using DbDarwin.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DbDarwin.Model.Command;

namespace DbDarwin.UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ExtractSchema()
        {
            ExtractSchemaService.ExtractSchema(new ExtractSchema
            {
                ConnectionString = "Data Source=EPIPC;Initial Catalog=Pay247 2018 06 15;Integrated Security=True;Connect Timeout=30",
                OutputFile = "xml1.xml"
            }
            );
            ExtractSchemaService.ExtractSchema(new ExtractSchema
            {
                ConnectionString = "Data Source=EPIPC;Initial Catalog=Pay247 2018 06 15_2;Integrated Security=True;Connect Timeout=30",
                OutputFile = "xml2.xml"
            });
        }

        [TestMethod]
        public void GenerateDiff()
        {
            CompareSchemaService.StartCompare(new GenerateDiffFile
            {
                CurrentFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "xml1.xml",
                NewSchemaFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "xml2.xml",
                OutputFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml"
            });
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TransformationForRenameTableNameDiff()
        {
            CompareSchemaService.TransformationDiffFile(new Transformation
            {
                CurrentDiffFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml",
                TableName = "Table_1",
                FromName = "343",
                ToName = "3434",
                MigrateSqlFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff2.xml"
            });
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GenerateScripts()
        {
            GenerateScriptService.GenerateScript(
                new GenerateScript
                {
                    CurrentDiffFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml",
                    MigrateSqlFile = AppContext.BaseDirectory + "\\output.sql"
                });
            Assert.IsTrue(true);
        }
    }
}
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
                ConnectionString = "Data Source=.;Initial Catalog=Pay247_Source;Integrated Security=True;Connect Timeout=30",
                OutputFile = "Source.xml"
            });

            ExtractSchemaService.ExtractSchema(new ExtractSchema
            {
                ConnectionString = "Data Source=.;Initial Catalog=Pay247_Target;Integrated Security=True;Connect Timeout=30",
                OutputFile = "Target.xml"
            });

        }

        [TestMethod]
        public void GenerateDiff()
        {
            CompareSchemaService.StartCompare(new GenerateDiffFile
            {
                NewSchemaFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Source.xml",
                CurrentFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Target.xml",
                OutputFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml"
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

        [TestMethod]
        public void TestAll()
        {
            ExtractSchema();
            GenerateDiff();
            GenerateScripts();
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
    }
}
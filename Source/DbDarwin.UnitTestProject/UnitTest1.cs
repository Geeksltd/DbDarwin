using DbDarwin.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DbDarwin.UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ExtractSchema()
        {
            ExtractSchemaService.ExtractSchema(@"Data Source=EPIPC;Initial Catalog=Test3;Integrated Security=True;Connect Timeout=30", "xml1.xml");
            ExtractSchemaService.ExtractSchema(@"Data Source=EPIPC;Initial Catalog=Test4;Integrated Security=True;Connect Timeout=30", "xml2.xml");
            GenerateDiff();
            GenerateScripts();
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
        public void TransformationForRenameTableNameDiff()
        {
            CompareSchemaService.TransformationDiffFile(

                AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml",
                "Table_1",
                "343",
                "3434",
                AppDomain.CurrentDomain.BaseDirectory + "\\diff2.xml");
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GenerateScripts()
        {
            GenerateScriptService.GenerateScript(
                AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml",
                AppContext.BaseDirectory + "\\output.sql"
                );

            Assert.IsTrue(true);
        }
    }
}
using DbDarwin.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using DbDarwin.Model.Command;
using KellermanSoftware.CompareNetObjects;
using Olive;
using System.Dynamic;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DbDarwin.UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ExtractSchema()
        {
            var sourceModel = new ExtractSchema
            {
                ConnectionString =
                    "Data Source=.\\SQL2016;Initial Catalog=Pay247_Source;Integrated Security=True;Connect Timeout=30",
                OutputFile = "Source.xml"
            };
            using (var service = new ExtractSchemaService(sourceModel))
                service.ExtractSchema();


            var targetModel = new ExtractSchema
            {
                ConnectionString =
                    "Data Source=.\\SQL2016;Initial Catalog=Pay247_Target;Integrated Security=True;Connect Timeout=30",
                OutputFile = "Target.xml"
            };
            using (var service = new ExtractSchemaService(targetModel))
                service.ExtractSchema();



        }

        [TestMethod]
        public void GenerateDiff()
        {
            CompareSchemaService.StartCompare(new GenerateDiffFile
            {
                SourceSchemaFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Source.xml",
                TargetSchemaFile = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Target.xml",
                OutputFile = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml"
            });
            Assert.IsTrue(true);
        }



        [TestMethod]
        public void GenerateScripts()
        {
            new GenerateScriptService().GenerateScript(
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


        //https://www.oreilly.com/learning/building-c-objects-dynamically
        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
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
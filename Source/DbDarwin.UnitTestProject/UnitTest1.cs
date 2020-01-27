using DbDarwin.Common;
using DbDarwin.Model.Command;
using DbDarwin.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DbDarwin.UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        public UnitTest1()
        {
            Directory.CreateDirectory(ConstantData.WorkingDir);
            Directory.CreateDirectory(ConstantData.LogDir);
        }
        [TestMethod]
        public void ExtractSchema()
        {
            var sourceModel = new ExtractSchema {
                ConnectionString = "Data Source=.\\SQL2016;Initial Catalog=Source;Integrated Security=True;Connect Timeout=30",
                OutputFile = "Source.xml"
            };
            using(var service = new ExtractSchemaService(sourceModel))
                service.ExtractSchema(Model.CompareType.Schema);


            var targetModel = new ExtractSchema {
                ConnectionString = "Data Source=.\\SQL2016;Initial Catalog=Target;Integrated Security=True;Connect Timeout=30",
                OutputFile = "Target.xml"
            };
            using(var service = new ExtractSchemaService(targetModel))
                service.ExtractSchema(Model.CompareType.Schema);



        }

        [TestMethod]
        public void GenerateDiff()
        {
            using(var service = new CompareSchemaService())
                service.StartCompare(new GenerateDiffFile {
                    SourceSchemaFile = ConstantData.WorkingDir + "\\" + "Source.xml",
                    TargetSchemaFile = ConstantData.WorkingDir + "\\" + "Target.xml",
                    OutputFile = ConstantData.WorkingDir + "\\diff.xml"
                });


            Assert.IsTrue(true);
        }



        [TestMethod]
        public void GenerateScripts()
        {
            new GenerateScriptService().GenerateScript(
                new GenerateScript {
                    CurrentDiffFile = ConstantData.WorkingDir + "\\diff.xml",
                    MigrateSqlFile = ConstantData.WorkingDir + "\\output.sql"
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
            CompareSchemaService.TransformationDiffFile(new Transformation {
                CurrentDiffFile = ConstantData.WorkingDir + "\\diff.xml",
                TableName = "Table_1",
                FromName = "343",
                ToName = "3434",
                MigrateSqlFile = ConstantData.WorkingDir + "\\diff2.xml"
            });
            Assert.IsTrue(true);
        }
    }
}

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

        [TestMethod]
        public void TestLoadDataFile()
        {
            var data = CompareSchemaService.LoadXMLFile(AppDomain.CurrentDomain.BaseDirectory + "\\Source.xml");

            var doc = new XDocument
            {
                Declaration = new XDeclaration("1.0", "UTF-8", "true")
            };
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var rootDatabase = new XElement("Database");



            foreach (var table in data.Tables)
            {
                var tableElement = new XElement("Table");
                tableElement.SetAttributeValue(nameof(table.Name), table.Name);
                if (table.Schema.ToLower() != "dbo")
                    tableElement.SetAttributeValue(nameof(table.Schema), table.Schema);

                var addNodes = new XElement("Add");
                var updateNodes = new XElement("Remove");
                var removeNodes = new XElement("Update");



                var sourceList = new List<IDictionary<string, object>>();
                var targetList = new List<IDictionary<string, object>>();


                foreach (XmlNode[] o in table.Data.Rows)
                {
                    var expan = new ExpandoObject();
                    foreach (XmlNode node in o)
                        AddProperty(expan, node.Name, node.InnerText);
                    sourceList.Add(expan.ToDictionary());
                }

                var compareLogic = new CompareLogic
                {
                    Config =
                    {
                        MaxDifferences = int.MaxValue,
                    },
                };



                foreach (IDictionary<string, object> data1 in sourceList)
                {
                    data1.TryGetValue("Name", out var val);

                    var exists = false;
                    foreach (var data2 in targetList)
                    {
                        if (data2.Any(x => x.Key == "Name" && x.Value == val))
                        {
                            var result = compareLogic.Compare(sourceList, targetList);
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        var rowElement = new XElement("Row");
                        foreach (var column in data1)
                        {
                            rowElement.SetAttributeValue(
                                XmlConvert.EncodeName(column.Key) ?? column.Key,
                                column.Value.ToString());
                        }
                        addNodes.Add(rowElement);
                    }
                }

                foreach (IDictionary<string, object> data1 in targetList)
                {
                    data1.TryGetValue("Name", out var val);

                    var exists = false;
                    foreach (var data2 in targetList)
                    {
                        if (data2.Any(x => x.Key == "Name" && x.Value == val))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        var rowElement = new XElement("Row");
                        foreach (var column in data1)
                        {
                            rowElement.SetAttributeValue(
                                XmlConvert.EncodeName(column.Key) ?? column.Key,
                                column.Value.ToString());

                        }
                        removeNodes.Add(rowElement);
                    }
                }

                if (addNodes.HasElements)
                    tableElement.Add(addNodes);
                if (removeNodes.HasElements)
                    tableElement.Add(removeNodes);
                if (updateNodes.HasElements)
                    tableElement.Add(updateNodes);


                if (tableElement.HasElements)
                    rootDatabase.Add(tableElement);
            }






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
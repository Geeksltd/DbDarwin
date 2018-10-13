using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
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
        public void TestMethod1()
        {
            ExtractSchemaService.ExtractSchema(@"Data Source=EPIPC;Initial Catalog=Test2;Integrated Security=True;Connect Timeout=30",
                "xml3.xml");

        }

        [TestMethod]
        public void TestLoadXML()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<DbDarwin.Model.Table>));
            List<DbDarwin.Model.Table> result1 = null;
            List<DbDarwin.Model.Table> result2 = null;
            using (var reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\" + "xml1.xml"))
                result1 = (List<DbDarwin.Model.Table>)serializer.Deserialize(reader);
            using (var reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\" + "xml2.xml"))
                result2 = (List<DbDarwin.Model.Table>)serializer.Deserialize(reader);

            CompareLogic compareLogic = new CompareLogic();



            XmlAttributes samplePropertyAttributes = new XmlAttributes { XmlIgnore = true };



            XmlAttributeOverrides sampleClassAttributes = new XmlAttributeOverrides();
            //sampleClassAttributes.Add(typeof(SampleClass), "SampleProperty", samplePropertyAttributes);

            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            // XmlSerializerNamespaces ns1 = new XmlSerializerNamespaces();
            // ns1.Add("xmlns", "http://www.w3.org/2001/XMLSchema");

            XmlElement ArrayOfTable = doc.CreateElement("ArrayOfTable");


            //ArrayOfTable xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"


            foreach (var r1 in result1)
            {
                var findedTable = result2.FirstOrDefault(x => x.Name == r1.Name);
                if (findedTable == null)
                {
                    // Must Delete
                }
                else
                {
                    XmlElement root = doc.CreateElement("Table");
                    var name = doc.CreateAttribute("Name");
                    name.Value = r1.Name;
                    root.Attributes.Append(name);

                    foreach (var c1 in r1.Column)
                    {
                        var findedColumns = findedTable.Column.FirstOrDefault(x => x.COLUMN_NAME == c1.COLUMN_NAME);
                        if (findedColumns == null)
                        {
                            // Must Delete
                        }
                        else
                        {
                            XmlElement column = doc.CreateElement("Column");

                            var result = compareLogic.Compare(c1, findedColumns);
                            if (!result.AreEqual)
                            {
                                //var listedPropery = result.Differences.Select(x => x.PropertyName).ToList();
                                foreach (var r in result.Differences)
                                {
                                    var data = doc.CreateAttribute("Set-" + r.PropertyName);
                                    data.Value = r.Object2Value;
                                    column.Attributes.Append(data);
                                    Console.WriteLine(r.PropertyName);
                                    Console.WriteLine(r.Object1TypeName + ":" + r.Object1Value);
                                    Console.WriteLine(r.Object2TypeName + ":" + r.Object2Value);
                                }

                                //var pro = findedColumns.GetType().GetProperties();
                                //foreach (var propertyInfo in pro)
                                //{
                                //    if (!listedPropery.Contains(propertyInfo.Name))
                                //    {
                                //        sampleClassAttributes.Add(typeof(Column), propertyInfo.Name, samplePropertyAttributes);
                                //    }
                                //}
                            }
                            else
                            {

                            }

                            root.AppendChild(column);
                        }
                    }
                    ArrayOfTable.AppendChild(root);
                }
            }
            doc.AppendChild(ArrayOfTable);
            //doc.Save();

            //var ser = new XmlSerializer(typeof(List<DbDarwin.Model.Table>), sampleClassAttributes);
            //StringWriter sw2 = new StringWriter();
            //ser.Serialize(sw2, result2);
            //var xml = sw2.ToString();
            var path = AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml";

            doc.Save(path);
            //File.WriteAllText(path, xml);
            Console.WriteLine("Saving To xml");





            Assert.IsTrue(true);
        }

    }
}

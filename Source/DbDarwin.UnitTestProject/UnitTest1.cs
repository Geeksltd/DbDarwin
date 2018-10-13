using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
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
                "xml2.xml");

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

            var result = compareLogic.Compare(result1, result2);

            if (!result.AreEqual)
            {
                foreach (var r in result.Differences)
                {
                    Console.WriteLine(r.Object1TypeName + ":" + r.Object1Value);
                    Console.WriteLine(r.Object2TypeName + ":" + r.Object2Value);
                }
            }


            Assert.IsTrue(true);
        }

    }
}

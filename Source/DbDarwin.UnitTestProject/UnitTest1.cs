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

    }
}
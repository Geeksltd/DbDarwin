using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
        public void GenerateScripts()
        {

            GenerateScriptService.GenerateScript(AppDomain.CurrentDomain.BaseDirectory + "\\diff.xml",
                AppContext.BaseDirectory + "\\output.sql"
                );

            Assert.IsTrue(true);
        }

    }
}

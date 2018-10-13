using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using DbDarwin.Service;
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
            //XmlSerializer serializer = new XmlSerializer(typeof(List<DbDarwin.Model.Table>));

            //StringWriter sw2 = new StringWriter();
            //serializer.Deserialize(sw2, tables);
            //using (TextReader reader = new StringReader(xmlResult))
            //{
            //    GetOrdersResponse result = (GetOrdersResponse)serializer.Deserialize(reader);
            //}
        }

    }
}

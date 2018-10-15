using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using DbDarwin.Model;

namespace DbDarwin.Service
{
    public class GenerateScriptService
    {
        public static void GenerateScript(string diffrenceXMLFile, string outputFile)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Table>));
            List<Table> diffFile = null;
            using (var reader = new StreamReader(diffrenceXMLFile))
                diffFile = (List<Table>)serializer.Deserialize(reader);
        }
    }
}

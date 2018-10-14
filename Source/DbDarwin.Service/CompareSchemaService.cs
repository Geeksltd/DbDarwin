using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using KellermanSoftware.CompareNetObjects;

namespace DbDarwin.Service
{
    public class CompareSchemaService
    {

        public static void StartCompare(string currentFileName, string newSchema, string output)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<DbDarwin.Model.Table>));
            List<DbDarwin.Model.Table> result1 = null;
            List<DbDarwin.Model.Table> result2 = null;
            using (var reader = new StreamReader(currentFileName))
                result1 = (List<DbDarwin.Model.Table>)serializer.Deserialize(reader);
            using (var reader = new StreamReader(newSchema))
                result2 = (List<DbDarwin.Model.Table>)serializer.Deserialize(reader);

            CompareLogic compareLogic = new CompareLogic
            {
                Config = { MaxDifferences = Int32.MaxValue }
            };
            XmlAttributeOverrides sampleClassAttributes = new XmlAttributeOverrides();
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);
            // XmlSerializerNamespaces ns1 = new XmlSerializerNamespaces();
            // ns1.Add("xmlns", "http://www.w3.org/2001/XMLSchema");
            XmlElement ArrayOfTable = doc.CreateElement("ArrayOfTable");
            // ArrayOfTable xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"


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


                    XmlElement add = doc.CreateElement("add");
                    var naviagetorAdd = add.CreateNavigator();
                    var emptyNamepsaces = new XmlSerializerNamespaces(new[] {
                        XmlQualifiedName.Empty
                    });

                    var mustAdd = findedTable.Column
                        .Where(x => !r1.Column.Select(c => c.COLUMN_NAME).ToList().Contains(x.COLUMN_NAME)).ToList();
                    foreach (var column in mustAdd)
                    {
                        using (var writer = naviagetorAdd.AppendChild())
                        {
                            var serializer1 = new XmlSerializer(column.GetType());
                            writer.WriteWhitespace("");
                            serializer1.Serialize(writer, column, emptyNamepsaces);
                            writer.Close();
                        }

                        //add.AppendChild()
                    }

                    if (add.HasChildNodes)
                        root.AppendChild(add);


                    foreach (var c1 in r1.Column)
                    {
                        XmlElement column = doc.CreateElement("Column");
                        var findedColumns = findedTable.Column.FirstOrDefault(x => x.COLUMN_NAME == c1.COLUMN_NAME);
                        if (findedColumns == null)
                        {
                            // Must Delete
                        }
                        else
                        {


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
            doc.Save(output);
            //File.WriteAllText(path, xml);
            Console.WriteLine("Saving To xml");
        }

    }
}

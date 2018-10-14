using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using DbDarwin.Model.Schema;
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


            //XmlAttributeOverrides sampleClassAttributes = new XmlAttributeOverrides();
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);
            // XmlSerializerNamespaces ns1 = new XmlSerializerNamespaces();
            // ns1.Add("xmlns", "http://www.w3.org/2001/XMLSchema");
            XmlElement ArrayOfTable = doc.CreateElement("ArrayOfTable");
            var emptyNamepsaces = new XmlSerializerNamespaces(new[] {
                XmlQualifiedName.Empty
            });


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

                    XmlElement removeColumn = doc.CreateElement("remove");
                    XPathNavigator naviagetorRemove = removeColumn.CreateNavigator();



                    GenerateDifference<Column>(doc, root, r1.Column, findedTable.Column, naviagetorAdd, naviagetorRemove, "COLUMN_NAME");
                    GenerateDifference<Index>(doc, root, r1.Index, findedTable.Index, naviagetorAdd, naviagetorRemove, "name");
                    GenerateDifference<REFERENTIAL_CONSTRAINTS>(doc, root, r1.ForeignKey, findedTable.ForeignKey, naviagetorAdd, naviagetorRemove, "CONSTRAINT_NAME");

                    if (add.HasChildNodes)
                        root.AppendChild(add);

                    if (removeColumn.HasChildNodes)
                        root.AppendChild(removeColumn);
                    ArrayOfTable.AppendChild(root);
                }
            }
            doc.AppendChild(ArrayOfTable);
            doc.Save(output);
            //File.WriteAllText(path, xml);
            Console.WriteLine("Saving To xml");
        }

        public static void GenerateDifference<T>(XmlDocument doc, XmlElement root,
            List<T> list, List<T> findedTable,
            XPathNavigator naviagetorAdd, XPathNavigator naviagetorRemove, string properyCheck)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[] {
                XmlQualifiedName.Empty
            });

            List<T> mustAdd = new List<T>();
            if (typeof(T) == typeof(Column))
            {

                var tempAdd = findedTable.Cast<Column>()
                    .Where(x => !list.Cast<Column>().Select(c => c.COLUMN_NAME).ToList().Contains(x.COLUMN_NAME)).ToList();
                mustAdd = (List<T>)Convert.ChangeType(tempAdd, typeof(List<T>));
            }
            else if (typeof(T) == typeof(Index))
            {
                var tempAdd = findedTable.Cast<Index>()
                    .Where(x => !list.Cast<Index>().Select(c => c.name).ToList().Contains(x.name)).ToList();
                mustAdd = (List<T>)Convert.ChangeType(tempAdd, typeof(List<T>));
            }
            else if (typeof(T) == typeof(REFERENTIAL_CONSTRAINTS))
            {
                var tempAdd = findedTable.Cast<REFERENTIAL_CONSTRAINTS>()
                    .Where(x => !list.Cast<REFERENTIAL_CONSTRAINTS>().Select(c => c.CONSTRAINT_NAME).ToList().Contains(x.CONSTRAINT_NAME)).ToList();
                mustAdd = (List<T>)Convert.ChangeType(tempAdd, typeof(List<T>));
            }


            foreach (T column in mustAdd)
            {
                using (var writer = naviagetorAdd.AppendChild())
                {
                    var serializer1 = new XmlSerializer(column.GetType());
                    writer.WriteWhitespace("");
                    serializer1.Serialize(writer, column, emptyNamespaces);
                    writer.Close();
                }
            }




            CompareLogic compareLogic = new CompareLogic
            {
                Config = { MaxDifferences = Int32.MaxValue }
            };

            foreach (T c1 in list)
            {
                XmlElement column = doc.CreateElement(typeof(T).Name);

                T foundObject = default(T);

                if (typeof(T) == typeof(Column))
                {
                    var found = findedTable.Cast<Column>().FirstOrDefault(x => x.COLUMN_NAME == c1.GetType().GetProperty(properyCheck).GetValue(c1).ToString());
                    foundObject = (T)Convert.ChangeType(found, typeof(T));
                }
                else if (typeof(T) == typeof(Index))
                {
                    var found = findedTable.Cast<Index>().FirstOrDefault(x => x.name == c1.GetType().GetProperty(properyCheck).GetValue(c1).ToString());
                    foundObject = (T)Convert.ChangeType(found, typeof(T));
                }
                else if (typeof(T) == typeof(REFERENTIAL_CONSTRAINTS))
                {
                    var found = findedTable.Cast<REFERENTIAL_CONSTRAINTS>().FirstOrDefault(x => x.CONSTRAINT_NAME == c1.GetType().GetProperty(properyCheck).GetValue(c1).ToString());
                    foundObject = (T)Convert.ChangeType(found, typeof(T));
                }

                if (foundObject == null)
                {
                    using (var writer = naviagetorRemove.AppendChild())
                    {
                        var serializer1 = new XmlSerializer(c1.GetType());
                        writer.WriteWhitespace("");
                        serializer1.Serialize(writer, c1, emptyNamespaces);
                        writer.Close();
                    }

                }
                else
                {
                    var result = compareLogic.Compare(c1, foundObject);
                    if (!result.AreEqual)
                    {
                        var columnName = doc.CreateAttribute(properyCheck);
                        columnName.Value = c1.GetType().GetProperty(properyCheck).GetValue(c1).ToString();
                        column.Attributes.Append(columnName);
                        foreach (var r in result.Differences)
                        {
                            var data = doc.CreateAttribute("Set-" + r.PropertyName);
                            data.Value = r.Object2Value;
                            column.Attributes.Append(data);
                            Console.WriteLine(r.PropertyName);
                            Console.WriteLine(r.Object1TypeName + ":" + r.Object1Value);
                            Console.WriteLine(r.Object2TypeName + ":" + r.Object2Value);
                        }
                        root.AppendChild(column);
                    }
                }
            }
        }

    }
}

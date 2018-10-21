using DbDarwin.Model;
using DbDarwin.Model.Schema;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace DbDarwin.Service
{
    public class CompareSchemaService
    {
        /// <summary>
        /// compare two xml file and create diff xml file
        /// </summary>
        /// <param name="currentFileName"></param>
        /// <param name="newSchema"></param>
        /// <param name="output"></param>
        public static bool StartCompare(string currentFileName, string newSchema, string output)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<Table>));
                List<Table> result1 = null;
                List<Table> result2 = null;
                using (var reader = new StreamReader(currentFileName))
                    result1 = (List<Table>)serializer.Deserialize(reader);
                using (var reader = new StreamReader(newSchema))
                    result2 = (List<Table>)serializer.Deserialize(reader);

                var doc = new XmlDocument();
                XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.AppendChild(docNode);
                var arrayOfTable = doc.CreateElement("ArrayOfTable");
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
                        var root = doc.CreateElement("Table");
                        var name = doc.CreateAttribute("Name");
                        name.Value = r1.Name;
                        root.Attributes.Append(name);

                        var add = doc.CreateElement("add");
                        var naviagetorAdd = add.CreateNavigator();

                        var removeColumn = doc.CreateElement("remove");
                        var naviagetorRemove = removeColumn.CreateNavigator();

                        var updateElement = doc.CreateElement("update");
                        var naviagetorUpdate = updateElement.CreateNavigator();

                        GenerateDifference<Column>(doc, root, r1.Column, findedTable.Column, naviagetorAdd, naviagetorRemove, naviagetorUpdate);
                        GenerateDifference<Index>(doc, root, r1.Index, findedTable.Index, naviagetorAdd, naviagetorRemove, naviagetorUpdate);
                        GenerateDifference<ForeignKey>(doc, root, r1.ForeignKey, findedTable.ForeignKey, naviagetorAdd, naviagetorRemove, naviagetorUpdate);

                        if (add.HasChildNodes) root.AppendChild(add);

                        if (removeColumn.HasChildNodes)
                            root.AppendChild(removeColumn);

                        if (updateElement.HasChildNodes)
                            root.AppendChild(updateElement);

                        arrayOfTable.AppendChild(root);
                    }
                }

                doc.AppendChild(arrayOfTable);
                doc.Save(output);
                // File.WriteAllText(path, xml);
                Console.WriteLine("Saving To xml");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                throw ex;
            }
            finally
            {
                GC.Collect();
            }

            return true;
        }

        /// <summary>
        /// Operation Set Name to Diff File 
        /// </summary>
        /// <param name="diffFile"></param>
        /// <param name="tableName"></param>
        /// <param name="fromName"></param>
        /// <param name="toName"></param>
        /// <param name="diffFileOutput"></param>
        public static void TransformationDiffFile(string diffFile, string tableName, string fromName, string toName, string diffFileOutput)
        {
            var serializer = new XmlSerializer(typeof(List<Table>));
            List<Table> result1 = null;
            using (var reader = new StreamReader(diffFile))
                result1 = (List<Table>)serializer.Deserialize(reader);

            if (result1 != null)
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    var table = result1.FirstOrDefault(x => string.Equals(x.Name, fromName, StringComparison.CurrentCultureIgnoreCase));
                    if (table != null)
                        table.SetName = toName;
                    else
                    {
                        table = new Table { Name = fromName, SetName = toName };
                        result1.Add(table);
                    }
                }
                else
                {
                    var table = result1.FirstOrDefault(x => string.Equals(x.Name, tableName, StringComparison.CurrentCultureIgnoreCase));
                    if (table != null)
                    {
                        var column = table.Update?.Column.FirstOrDefault(x =>
                            string.Equals(x.COLUMN_NAME, fromName, StringComparison.CurrentCultureIgnoreCase));
                        if (column != null)
                            column.SetName = toName;
                        else
                        {
                            column = new Column { COLUMN_NAME = fromName, SetName = toName };
                            if (table.Update == null)
                                table.Update = new Table();
                            table.Update.Column.Add(column);
                        }
                    }
                }
            }

            var sw2 = new StringWriter();
            serializer.Serialize(sw2, result1);
            var xml = sw2.ToString();
            File.WriteAllText(diffFileOutput, xml);
        }

        /// <summary>
        /// compare objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="doc"></param>
        /// <param name="root"></param>
        /// <param name="currentList"></param>
        /// <param name="newList"></param>
        /// <param name="naviagetorAdd"></param>
        /// <param name="naviagetorRemove"></param>
        /// <param name="properyCheck"></param>
        public static void GenerateDifference<T>(XmlDocument doc, XmlElement root,
            List<T> currentList, List<T> newList,
            XPathNavigator naviagetorAdd, XPathNavigator naviagetorRemove, XPathNavigator naviagetorUpdate)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[] {
                XmlQualifiedName.Empty,
            });

            #region Detect new sql object like as INDEX , Column , REFERENTIAL_CONSTRAINTS 

            List<T> mustAdd = new List<T>();
            if (typeof(T) == typeof(Column))
            {
                var tempAdd = newList.Cast<Column>()
                    .Where(x => !currentList.Cast<Column>().Select(c => c.COLUMN_NAME).ToList().Contains(x.COLUMN_NAME)).ToList();
                mustAdd = (List<T>)Convert.ChangeType(tempAdd, typeof(List<T>));
            }
            else if (typeof(T) == typeof(Index))
            {
                var tempAdd = newList.Cast<Index>()
                    .Where(x => !currentList.Cast<Index>().Select(c => c.name).ToList().Contains(x.name)).ToList();
                mustAdd = (List<T>)Convert.ChangeType(tempAdd, typeof(List<T>));
            }
            else if (typeof(T) == typeof(ForeignKey))
            {
                var tempAdd = newList.Cast<ForeignKey>()
                    .Where(x => !currentList.Cast<ForeignKey>().Select(c => c.CONSTRAINT_NAME).ToList().Contains(x.CONSTRAINT_NAME)).ToList();
                mustAdd = (List<T>)Convert.ChangeType(tempAdd, typeof(List<T>));
            }

            #endregion

            // Add new objects to xml
            foreach (T sqlObject in mustAdd)
            {
                using (var writer = naviagetorAdd.AppendChild())
                {
                    var serializer1 = new XmlSerializer(sqlObject.GetType());
                    writer.WriteWhitespace("");
                    serializer1.Serialize(writer, sqlObject, emptyNamespaces);
                    writer.Close();
                }
            }

            var compareLogic = new CompareLogic
            {
                Config = { MaxDifferences = int.MaxValue }
            };

            // Detect Sql Objects Changes
            foreach (T c1 in currentList)
            {
                var sqlElement = doc.CreateElement(typeof(T).Name);

                var foundObject = default(T);

                if (typeof(T) == typeof(Column))
                {
                    var found = newList.Cast<Column>().FirstOrDefault(x => x.Name == c1.GetType().GetProperty("Name").GetValue(c1).ToString());
                    foundObject = (T)Convert.ChangeType(found, typeof(T));
                }
                else if (typeof(T) == typeof(Index))
                {
                    var found = newList.Cast<Index>().FirstOrDefault(x => x.Name == c1.GetType().GetProperty("Name").GetValue(c1).ToString());
                    foundObject = (T)Convert.ChangeType(found, typeof(T));
                }
                else if (typeof(T) == typeof(ForeignKey))
                {
                    var found = newList.Cast<ForeignKey>().FirstOrDefault(x => x.Name == c1.GetType().GetProperty("Name").GetValue(c1).ToString());
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
                        using (var writer = naviagetorUpdate.AppendChild())
                        {
                            var serializer1 = new XmlSerializer(foundObject.GetType());
                            writer.WriteWhitespace("");
                            serializer1.Serialize(writer, foundObject, emptyNamespaces);
                            writer.Close();
                        }

                        // var columnName = doc.CreateAttribute("Name");
                        // columnName.Value = c1.GetType().GetProperty("Name").GetValue(c1).ToString();
                        // sqlElement.Attributes.Append(columnName);
                        // foreach (var r in result.Differences)
                        // {
                        //    var data = doc.CreateAttribute(r.PropertyName);
                        //    data.Value = r.Object2Value;
                        //    sqlElement.Attributes.Append(data);
                        //    Console.WriteLine(r.PropertyName);
                        //    Console.WriteLine(r.Object1TypeName + ":" + r.Object1Value);
                        //    Console.WriteLine(r.Object2TypeName + ":" + r.Object2Value);
                        // }
                        // naviagetorUpdate.AppendChild(sqlElement);
                    }
                }
            }
        }
    }
}
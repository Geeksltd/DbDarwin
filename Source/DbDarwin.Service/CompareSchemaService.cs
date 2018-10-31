using DbDarwin.Model;
using DbDarwin.Model.Schema;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using DbDarwin.Model.Command;
using GCop.Core;

namespace DbDarwin.Service
{
    public class CompareSchemaService
    {
        /// <summary>
        /// compare two xml file and create diff xml file
        /// </summary>
        /// <param name="currentFileName">Current XML File</param>
        /// <param name="newSchemaFilePath">New XML File Want To Compare</param>
        /// <param name="output">Output File XML diff</param>
        public static bool StartCompare(GenerateDiffFile model)
        {
            try
            {
                var targetSchema = LoadXMLFile(model.TargetSchemaFile);
                var sourceSchema = LoadXMLFile(model.SourceSchemaFile);

                CompareAndSave(
                    sourceSchema,
                    targetSchema,
                    model.OutputFile);

                Console.WriteLine("Saving To xml");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                throw;
            }
            finally
            {
                GC.Collect();
            }

            return true;
        }

        private static void CompareAndSave(Database sourceSchema, Database targetSchema, string output)
        {
            var doc = new XDocument
            {
                Declaration = new XDeclaration("1.0", "UTF-8", "true")
            };
            var rootDatabase = new XElement("Database");

            var updateTables = new XElement("update");
            var addTables = new XElement("add");
            var removeTables = new XElement("remove");



            foreach (var sourceTable in sourceSchema.Tables)
            {

                var foundTable = targetSchema.Tables.FirstOrDefault(x => x.Name == sourceTable.Name);

                if (foundTable == null)
                {
                    using (var navigatorAdd = addTables.CreateWriter())
                    {
                        var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
                        var serializer1 = new XmlSerializer(sourceTable.GetType());
                        navigatorAdd.WriteWhitespace("");
                        serializer1.Serialize(navigatorAdd, sourceTable, emptyNamespaces);
                    }
                }
                else
                {


                    var root = new XElement("Table");
                    root.SetAttributeValue(nameof(sourceTable.Name), sourceTable.Name);

                    var add = new XElement("add");
                    var navigatorAdd = add.CreateWriter();

                    var removeColumn = new XElement("remove");
                    var navigatorRemove = removeColumn.CreateWriter();

                    var updateElement = new XElement("update");
                    var navigatorUpdate = updateElement.CreateWriter();

                    GenerateDifference<PrimaryKey>(
                        sourceTable.PrimaryKey == null ? new List<PrimaryKey>() : new List<PrimaryKey> { sourceTable.PrimaryKey },
                        foundTable.PrimaryKey == null ? new List<PrimaryKey>() : new List<PrimaryKey> { foundTable.PrimaryKey }, navigatorAdd,
                        navigatorRemove, navigatorUpdate);
                    GenerateDifference<Column>(sourceTable.Columns, foundTable.Columns, navigatorAdd, navigatorRemove,
                        navigatorUpdate);
                    GenerateDifference<Index>(sourceTable.Indexes, foundTable.Indexes, navigatorAdd, navigatorRemove,
                        navigatorUpdate);
                    GenerateDifference<ForeignKey>(sourceTable.ForeignKeys, foundTable.ForeignKeys, navigatorAdd,
                        navigatorRemove, navigatorUpdate);


                    navigatorAdd.Flush();
                    navigatorAdd.Close();

                    navigatorRemove.Flush();
                    navigatorRemove.Close();

                    navigatorUpdate.Flush();
                    navigatorUpdate.Close();

                    if (!add.IsEmpty)
                        root.Add(add);

                    if (!removeColumn.IsEmpty)
                        root.Add(removeColumn);

                    if (!updateElement.IsEmpty)
                        root.Add(updateElement);
                    updateTables.Add(root);
                }
            }
            var mustRemove = targetSchema.Tables.Except(c => sourceSchema.Tables.Select(x => x.Name).ToList().Contains(c.Name)).ToList();
            using (var navigatorAdd = removeTables.CreateWriter())
            {
                var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
                var serializer1 = new XmlSerializer(mustRemove.GetType());
                navigatorAdd.WriteWhitespace("");
                serializer1.Serialize(navigatorAdd, mustRemove, emptyNamespaces);
            }


            rootDatabase.Add(updateTables);
            rootDatabase.Add(addTables);
            rootDatabase.Add(removeTables);
            doc.Add(rootDatabase);

            doc.Save(output);
        }

        private static Database LoadXMLFile(string currentFileName)
        {
            var serializer = new XmlSerializer(typeof(Database));
            using (var reader = new StreamReader(currentFileName))
                return (Database)serializer.Deserialize(reader);
        }

        /// <summary>
        /// Operation Set Name to Diff File 
        /// </summary>
        /// <param name="diffFile">Current XML Diff File</param>
        /// <param name="tableName">table name if want change column name</param>
        /// <param name="fromName">First Name</param>
        /// <param name="toName">Replace Name</param>
        /// <param name="diffFileOutput">Output new XML file diff</param>
        public static void TransformationDiffFile(Transformation model)
        {

            var serializer = new XmlSerializer(typeof(List<Table>));
            List<Table> currentDiffSchema = null;
            using (var reader = new StreamReader(model.CurrentDiffFile))
                currentDiffSchema = (List<Table>)serializer.Deserialize(reader);

            if (currentDiffSchema != null)
            {
                if (model.TableName.HasValue())
                {
                    var table = currentDiffSchema.FirstOrDefault(x =>
                        string.Equals(x.Name, model.FromName, StringComparison.CurrentCultureIgnoreCase));
                    if (table != null)
                        table.SetName = model.ToName;
                    else
                    {
                        table = new Table { Name = model.FromName, SetName = model.ToName };
                        currentDiffSchema.Add(table);
                    }
                }
                else
                {
                    var table = currentDiffSchema.FirstOrDefault(x =>
                        string.Equals(x.Name, model.TableName, StringComparison.CurrentCultureIgnoreCase));
                    if (table != null)
                    {
                        if (table.Update == null)
                        {
                            var column = new Column { COLUMN_NAME = model.FromName, SetName = model.ToName };
                            table.Update = new Table();
                            table.Update.Columns.Add(column);
                        }
                        else
                        {
                            var column = table.Update.Columns.FirstOrDefault(x =>
                                string.Equals(x.COLUMN_NAME, model.FromName,
                                    StringComparison.CurrentCultureIgnoreCase));
                            SetColumnName(column, model);
                        }
                    }
                }
            }


            var sw2 = new StringWriter();
            serializer.Serialize(sw2, currentDiffSchema);
            var xml = sw2.ToString();
            File.WriteAllText(model.MigrateSqlFile, xml);
        }

        private static void SetColumnName(Column column, Transformation model)
        {
            if (column != null) column.SetName = model.ToName;
        }

        /// <summary>
        /// compare objects
        /// </summary>
        /// <typeparam name="T">Type can Column , Index , ForeignKey</typeparam>
        /// <param name="doc">must be current XDocument</param>
        /// <param name="root">root xml element</param>
        /// <param name="currentList">Current Diff List Data</param>
        /// <param name="newList">must be compare data</param>
        /// <param name="navigatorAdd">refers to add element XML</param>
        /// <param name="navigatorRemove">refers to remove element XML</param>
        /// <param name="navigatorUpdate">refers to update element XML</param>
        public static void GenerateDifference<T>(List<T> currentList, List<T> newList,
            XmlWriter navigatorAdd, XmlWriter navigatorRemove, XmlWriter navigatorUpdate)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[]
            {
                XmlQualifiedName.Empty,
            });

            // Detect new sql object like as INDEX , Column , REFERENTIAL_CONSTRAINTS 
            var mustAdd = FindNewComponent<T>(currentList, newList);

            // Add new objects to xml
            if (mustAdd != null)
                foreach (T sqlObject in mustAdd)
                {
                    var serializer1 = new XmlSerializer(sqlObject.GetType());
                    navigatorAdd.WriteWhitespace("");
                    serializer1.Serialize(navigatorAdd, sqlObject, emptyNamespaces);
                }

            var compareLogic = new CompareLogic
            {
                Config = { MaxDifferences = int.MaxValue }
            };

            // Detect Sql Objects Changes
            if (currentList != null)
                foreach (T currentObject in currentList)
                {
                    var foundObject = FindRemoveOrUpdate<T>(currentObject, newList);
                    if (foundObject == null)
                    {
                        var serializer1 = new XmlSerializer(currentObject.GetType());
                        navigatorRemove.WriteWhitespace("");
                        serializer1.Serialize(navigatorRemove, currentObject, emptyNamespaces);
                    }
                    else
                    {
                        var result = compareLogic.Compare(currentObject, foundObject);
                        if (!result.AreEqual)
                        {
                            var serializer1 = new XmlSerializer(foundObject.GetType());
                            navigatorUpdate.WriteWhitespace("");
                            serializer1.Serialize(navigatorUpdate, foundObject, emptyNamespaces);
                        }
                    }
                }
        }

        private static object FindRemoveOrUpdate<T>(T currentObject, IEnumerable<T> newList)
        {
            object found = null;
            if (typeof(T) == typeof(Column))
                found = newList.Cast<Column>().FirstOrDefault(x =>
                    x.Name == currentObject.GetType().GetProperty("Name").GetValue(currentObject).ToString());
            else if (typeof(T) == typeof(Index))
                found = newList.Cast<Index>().FirstOrDefault(x =>
                    x.Name == currentObject.GetType().GetProperty("Name").GetValue(currentObject).ToString());
            else if (typeof(T) == typeof(ForeignKey))
                found = newList.Cast<ForeignKey>().FirstOrDefault(x =>
                    x.Name == currentObject.GetType().GetProperty("Name").GetValue(currentObject).ToString());
            return (T)Convert.ChangeType(found, typeof(T));
        }

        private static List<T> FindNewComponent<T>(List<T> currentList, List<T> newList)
        {
            object tempAdd = null;
            if (newList == null)
                return new List<T>();
            if (currentList == null)
                return newList;
            if (typeof(T) == typeof(Column))
                tempAdd = newList.Cast<Column>()
                    .Except(x => currentList.Cast<Column>().Select(c => c.COLUMN_NAME).ToList().Contains(x.COLUMN_NAME))
                    .ToList();
            else if (typeof(T) == typeof(Index))
                tempAdd = newList.Cast<Index>()
                    .Except(x => currentList.Cast<Index>().Select(c => c.name).ToList().Contains(x.name)).ToList();
            else if (typeof(T) == typeof(ForeignKey))
                tempAdd = newList.Cast<ForeignKey>()
                    .Except(x =>
                        currentList.Cast<ForeignKey>().Select(c => c.CONSTRAINT_NAME).ToList()
                            .Contains(x.CONSTRAINT_NAME)).ToList();
            return (List<T>)Convert.ChangeType(tempAdd, typeof(List<T>));
        }
    }
}
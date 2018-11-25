using DbDarwin.Model;
using DbDarwin.Model.Schema;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
        public static ResultMessage StartCompare(GenerateDiffFile model)
        {
            var result = new ResultMessage();
            try
            {
                var targetSchema = LoadXMLFile(model.TargetSchemaFile);
                var sourceSchema = LoadXMLFile(model.SourceSchemaFile);

                CompareAndSave(
                    sourceSchema,
                    targetSchema,
                    model.OutputFile);

                result.IsSuccessfully = true;
                Console.WriteLine("Saving To xml");
            }
            catch (Exception ex)
            {
                result.IsSuccessfully = false;
                result.Messsage = ex.Message;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.White;
                throw;
            }
            finally
            {
                GC.Collect();
            }

            return result;
        }

        private static void CompareAndSave(Database sourceSchema, Database targetSchema, string output)
        {
            var doc = new XDocument
            {
                Declaration = new XDeclaration("1.0", "UTF-8", "true")
            };
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var rootDatabase = new XElement("Database");

            var updateTables = new XElement("update");
            var addTables = new XElement("add");
            var removeTables = new XElement("remove");



            foreach (var sourceTable in sourceSchema.Tables)
            {

                var foundTable = targetSchema.Tables.FirstOrDefault(x => x.FullName == sourceTable.FullName);

                if (foundTable == null)
                {
                    using (var navigatorAdd = addTables.CreateWriter())
                    {

                        var serializer1 = new XmlSerializer(sourceTable.GetType());
                        navigatorAdd.WriteWhitespace("");
                        serializer1.Serialize(navigatorAdd, sourceTable, emptyNamespaces);
                    }
                }
                else
                {


                    var root = new XElement("Table");
                    root.SetAttributeValue(nameof(sourceTable.Name), sourceTable.Name);
                    root.SetAttributeValue(nameof(sourceTable.Schema), sourceTable.Schema);

                    var add = new XElement("add");
                    var navigatorAdd = add.CreateWriter();

                    var removeColumn = new XElement("remove");
                    var navigatorRemove = removeColumn.CreateWriter();

                    var updateElement = new XElement("update");
                    var navigatorUpdate = updateElement.CreateWriter();



                    if (sourceTable.PrimaryKey == null && foundTable.PrimaryKey != null)
                    {
                        var serializer1 = new XmlSerializer(foundTable.PrimaryKey.GetType());
                        navigatorRemove.WriteWhitespace("");
                        serializer1.Serialize(navigatorRemove, foundTable.PrimaryKey, emptyNamespaces);
                    }
                    else
                    {
                        GenerateDifference<PrimaryKey>(
                            sourceTable.PrimaryKey == null
                                ? new List<PrimaryKey>()
                                : new List<PrimaryKey> { sourceTable.PrimaryKey },
                            foundTable.PrimaryKey == null
                                ? new List<PrimaryKey>()
                                : new List<PrimaryKey> { foundTable.PrimaryKey }, navigatorAdd,
                            navigatorRemove, navigatorUpdate);
                    }


                    GenerateDifference<Column>(sourceTable.Columns, foundTable.Columns, navigatorAdd, navigatorRemove,
                        navigatorUpdate);
                    GenerateDifference<Index>(sourceTable.Indexes, foundTable.Indexes, navigatorAdd, navigatorRemove,
                        navigatorUpdate);
                    GenerateDifference<ForeignKey>(sourceTable.ForeignKeys, foundTable.ForeignKeys, navigatorAdd,
                        navigatorRemove, navigatorUpdate);

                    GenerateDifferenceData(sourceTable.Data, foundTable.Data, navigatorAdd,
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

                    if (!add.IsEmpty || !removeColumn.IsEmpty || !updateElement.IsEmpty)
                        updateTables.Add(root);
                }
            }
            var mustRemove = targetSchema.Tables.Except(c => sourceSchema.Tables.Select(x => x.FullName).ToList().Contains(c.FullName)).ToList();
            using (var writer = removeTables.CreateWriter())
            {
                foreach (var table in mustRemove)
                {
                    var serializer1 = new XmlSerializer(table.GetType());
                    writer.WriteWhitespace("");
                    serializer1.Serialize(writer, table, emptyNamespaces);
                }
            }


            rootDatabase.Add(updateTables);
            rootDatabase.Add(addTables);
            rootDatabase.Add(removeTables);

            doc.Add(rootDatabase);

            doc.Save(output);
        }


        private static void GenerateDifferenceData(TableData sourceData, TableData targetData, XmlWriter addWriter, XmlWriter removeWriter, XmlWriter updateWriter)
        {
            var sourceList = sourceData.ToDictionaryList();
            var targetList = targetData.ToDictionaryList();


            var compareLogic = new CompareLogic { Config = { MaxDifferences = int.MaxValue } };
            var dataNodeAdd = new XElement("Data");
            foreach (IDictionary<string, object> row in sourceList)
            {
                row.TryGetValue("Name", out var val);
                var exists = false;
                foreach (var data2 in targetList)
                {
                    if (data2.Any(x => x.Key == "Name" && x.Value == val))
                    {
                        var result = compareLogic.Compare(sourceList, targetList);
                        //if (!result.AreEqual)
                        //{

                        //}
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    dataNodeAdd.Add(row.ToElement("Row"));
            }

            var dataNodeRemove = new XElement("Data");
            foreach (IDictionary<string, object> row in targetList)
            {
                row.TryGetValue("Name", out var val);

                var exists = false;
                foreach (var data2 in sourceList)
                {
                    if (data2.Any(x => x.Key == "Name" && x.Value == val))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    dataNodeRemove.Add(row.ToElement("Row"));
            }

            if (dataNodeAdd.HasElements)
                addWriter.Serialize(dataNodeAdd);
            if (dataNodeRemove.HasElements)
                removeWriter.Serialize(dataNodeRemove);

            //if (updateNodes.HasElements)
            //    tableElement.Add(updateNodes);


            //if (tableElement.HasElements)
            //    rootDatabase.Add(tableElement);
        }

        public static Database LoadXMLFile(string currentFileName)
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
        /// <param name="targetData">Current Diff List Data</param>
        /// <param name="sourceData">must be compare data</param>
        /// <param name="navigatorAdd">refers to add element XML</param>
        /// <param name="navigatorRemove">refers to remove element XML</param>
        /// <param name="navigatorUpdate">refers to update element XML</param>
        public static void GenerateDifference<T>(List<T> sourceData, List<T> targetData,
            XmlWriter navigatorAdd, XmlWriter navigatorRemove, XmlWriter navigatorUpdate)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[]
            {
                XmlQualifiedName.Empty,
            });

            // Detect new sql object like as INDEX , Column , REFERENTIAL_CONSTRAINTS 
            var mustAdd = FindNewComponent<T>(sourceData, targetData);

            // Add new objects to xml
            if (mustAdd != null)
                foreach (T sqlObject in mustAdd)
                    navigatorAdd.Serialize(sqlObject);

            var compareLogic = new CompareLogic { Config = { MaxDifferences = int.MaxValue } };
            if (typeof(T) == typeof(PrimaryKey))
            {
                compareLogic.Config.MembersToIgnore.Add("Name");
                compareLogic.Config.MembersToIgnore.Add("name");
            }

            // Detect Sql Objects Changes
            if (targetData == null) return;
            {
                if (mustAdd != null)
                    sourceData = sourceData.Except(x => mustAdd.Contains(x)).ToList();
                foreach (T currentObject in sourceData)
                {

                    var foundObject = FindRemoveOrUpdate<T>(currentObject, targetData);
                    if (foundObject == null)
                    {
                        if (typeof(T) == typeof(PrimaryKey))
                            navigatorUpdate.Serialize(currentObject);
                        else
                            navigatorRemove.Serialize(currentObject);
                    }
                    else
                    {
                        var result = compareLogic.Compare(currentObject, foundObject);
                        if (!result.AreEqual)
                            navigatorUpdate.Serialize(currentObject);
                    }
                }


                foreach (T currentObject in targetData)
                {
                    var foundObject = FindRemoveOrUpdate<T>(currentObject, sourceData);
                    if (foundObject == null)
                        navigatorRemove.Serialize(currentObject);
                }
            }
        }

        private static object FindRemoveOrUpdate<T>(T currentObject, IEnumerable<T> newList)
        {
            object found = null;
            if (typeof(T) == typeof(Column))
                found = newList.Cast<Column>().FirstOrDefault(x =>
                    x.Name == currentObject.GetType().GetProperty("Name")?.GetValue(currentObject).ToString());
            else if (typeof(T) == typeof(Index))
                found = newList.Cast<Index>().FirstOrDefault(x =>
                    x.Name == currentObject.GetType().GetProperty("Name")?.GetValue(currentObject).ToString());
            else if (typeof(T) == typeof(ForeignKey))
                found = newList.Cast<ForeignKey>().FirstOrDefault(x =>
                    x.Name == currentObject.GetType().GetProperty("Name")?.GetValue(currentObject).ToString());
            else if (typeof(T) == typeof(PrimaryKey))
                found = newList.Cast<PrimaryKey>().FirstOrDefault(x =>
                    x.Columns == currentObject.GetType().GetProperty("Columns")?.GetValue(currentObject).ToString());

            return (T)Convert.ChangeType(found, typeof(T));
        }

        private static List<T> FindNewComponent<T>(List<T> sourceList, List<T> targetList)
        {
            object tempAdd = null;
            if (sourceList == null)
                return new List<T>();
            if (targetList == null)
                return sourceList;
            if (typeof(T) == typeof(Column))
                tempAdd = sourceList.Cast<Column>()
                    .Except(x => targetList.Cast<Column>().Select(c => c.COLUMN_NAME).ToList().Contains(x.COLUMN_NAME))
                    .ToList();
            else if (typeof(T) == typeof(Index))
                tempAdd = sourceList.Cast<Index>()
                    .Except(x => targetList.Cast<Index>().Select(c => c.name).ToList().Contains(x.name)).ToList();
            else if (typeof(T) == typeof(ForeignKey))
                tempAdd = sourceList.Cast<ForeignKey>()
                    .Except(x =>
                        targetList.Cast<ForeignKey>().Select(c => c.CONSTRAINT_NAME).ToList()
                            .Contains(x.CONSTRAINT_NAME)).ToList();
            return (List<T>)Convert.ChangeType(tempAdd, typeof(List<T>));
        }
    }
}
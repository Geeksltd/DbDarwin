using DbDarwin.Model;
using DbDarwin.Model.Command;
using DbDarwin.Model.Schema;
using GCop.Core;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DbDarwin.Service
{
    public class CompareSchemaService : IDisposable
    {
        bool disposedValue; // To detect redundant calls
        readonly XElement UpdateTables;
        readonly XElement AddTables;
        readonly XElement RemoveTables;
        readonly XElement RootDatabase;
        readonly XDocument Doc;

        public CompareSchemaService()
        {
            Doc = new XDocument { Declaration = new XDeclaration("1.0", "UTF-8", "true") };
            RootDatabase = new XElement("Database");
            UpdateTables = new XElement("update");
            AddTables = new XElement("add");
            RemoveTables = new XElement("remove");
        }
        /// <summary>
        /// compare two xml file and create diff xml file
        /// </summary>
        /// <param name="currentFileName">Current XML File</param>
        /// <param name="newSchemaFilePath">New XML File Want To Compare</param>
        /// <param name="output">Output File XML diff</param>
        public ResultMessage StartCompare(GenerateDiffFile model)
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

        void CompareAndSave(Database sourceSchema, Database targetSchema, string output)
        {
            foreach (var sourceTable in sourceSchema.Tables)
            {
                var foundTable = targetSchema.Tables.FirstOrDefault(x => x.FullName == sourceTable.FullName);
                if (foundTable == null)
                {
                    using (var navigatorAdd = AddTables.CreateWriter())
                        navigatorAdd.Serialize(sourceTable);
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

                    GenerateDifferencePrimaryKey(sourceTable, foundTable, navigatorAdd, navigatorRemove, navigatorUpdate);
                    GenerateDifference<Column>(sourceTable.Columns, foundTable.Columns, navigatorAdd, navigatorRemove, navigatorUpdate);
                    GenerateDifference<Index>(sourceTable.Indexes, foundTable.Indexes, navigatorAdd, navigatorRemove, navigatorUpdate);
                    GenerateDifference<ForeignKey>(sourceTable.ForeignKeys, foundTable.ForeignKeys, navigatorAdd, navigatorRemove, navigatorUpdate);
                    GenerateDifferenceData(sourceTable, foundTable.Data, navigatorAdd, navigatorRemove, navigatorUpdate);

                    navigatorAdd.Flush();
                    navigatorAdd.Close();

                    navigatorRemove.Flush();
                    navigatorRemove.Close();

                    navigatorUpdate.Flush();
                    navigatorUpdate.Close();

                    if (!add.IsEmpty) root.Add(add);
                    if (!removeColumn.IsEmpty) root.Add(removeColumn);
                    if (!updateElement.IsEmpty) root.Add(updateElement);
                    if (!add.IsEmpty || !removeColumn.IsEmpty || !updateElement.IsEmpty) UpdateTables.Add(root);
                }
            }

            var mustRemove = targetSchema.Tables.Except(c => sourceSchema.Tables.Select(x => x.FullName).ToList().Contains(c.FullName)).ToList();
            using (var writer = RemoveTables.CreateWriter())
                mustRemove.ForEach(c => writer.Serialize(c));

            SaveChanges(output);
        }

        void GenerateDifferencePrimaryKey(Table sourceTable, Table foundTable, XmlWriter navigatorAdd, XmlWriter navigatorRemove, XmlWriter navigatorUpdate)
        {
            if (sourceTable.PrimaryKey == null && foundTable.PrimaryKey != null)
                navigatorRemove.Serialize(foundTable.PrimaryKey);
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
        }

        void SaveChanges(string output)
        {
            if (UpdateTables.HasElements) RootDatabase.Add(UpdateTables);
            if (AddTables.HasElements) RootDatabase.Add(AddTables);
            if (RemoveTables.HasElements) RootDatabase.Add(RemoveTables);

            Doc.Add(RootDatabase);
            Doc.Save(output);
        }

        /// <summary>
        /// Generate Difference Data
        /// </summary>
        /// <param name="source">Source Table</param>
        /// <param name="targetData">Target Table Data</param>
        /// <param name="addWriter">XML Writer for add data</param>
        /// <param name="removeWriter">XML Writer for remove data</param>
        /// <param name="updateWriter">XML Writer for update data</param>
        void GenerateDifferenceData(Table source, TableData targetData, XmlWriter addWriter, XmlWriter removeWriter, XmlWriter updateWriter)
        {
            if (source.Data == null && targetData == null) return;
            var sourceTable = source.Data.ToDictionaryList();
            var targetTable = targetData.ToDictionaryList();
            var columnType = GetColumnTypes(source.Columns);
            // Detect Add Or Update
            var result = DetectAddOrUpdate(sourceTable, targetTable);
            result.TryGetValue("Add", out var dataNodeAdd);
            result.TryGetValue("Update", out var dataNodeUpdate);
            // Detect Remove Data
            var dataNodeRemove = DetectRemoveData(sourceTable, targetTable);

            AddDataElementToWriter(dataNodeAdd, columnType, addWriter);
            AddDataElementToWriter(dataNodeRemove, columnType, removeWriter);
            AddDataElementToWriter(dataNodeUpdate, columnType, updateWriter);
        }

        /// <summary>
        /// Add XElement Data Row to writer
        /// </summary>
        /// <param name="dataNode">Data Node</param>
        /// <param name="columnType">Column Type</param>
        /// <param name="writer">XML Writer</param>
        void AddDataElementToWriter(XElement dataNodeAdd, XElement columnType, XmlWriter writer)
        {
            if (dataNodeAdd != null && dataNodeAdd.HasElements)
            {
                dataNodeAdd.AddFirst(columnType);
                writer.Serialize(dataNodeAdd);
            }
        }

        XElement GetColumnTypes(IEnumerable<Column> columns)
        {
            var columnTypes = new XElement("ColumnTypes");
            columns.ForEach(column => columnTypes.SetAttributeValue(XmlConvert.EncodeName(column.Name) ?? column.Name, column.DATA_TYPE));
            return columnTypes;
        }

        /// <summary>
        /// Detect Add Or Update Data
        /// </summary>
        /// <param name="sourceTable">soucre table</param>
        /// <param name="targetTable">target table</param>
        /// <returns>return ditionary</returns>
        Dictionary<string, XElement> DetectAddOrUpdate(List<IDictionary<string, object>> sourceTable, List<IDictionary<string, object>> targetTable)
        {
            var compareLogic = new CompareLogic { Config = { MaxDifferences = int.MaxValue } };
            var dataNodeAdd = new XElement("Data");
            var dataNodeUpdate = new XElement("Data");
            // Detect new data or updates
            foreach (IDictionary<string, object> sourceRow in sourceTable)
            {
                sourceRow.TryGetValue("Name", out var val);
                var exists = false;
                foreach (var targetRow in targetTable)
                {
                    if (targetRow.Any(x => x.Key == "Name" && x.Value?.ToString() == val?.ToString()))
                    {
                        var result = compareLogic.Compare(sourceRow, targetRow);
                        if (!result.AreEqual)
                            dataNodeUpdate.Add(sourceRow.ToElement("Row"));
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    dataNodeAdd.Add(sourceRow.ToElement("Row"));
            }

            return new Dictionary<string, XElement> { { "Add", dataNodeAdd }, { "Update", dataNodeUpdate } };
        }

        /// <summary>
        /// Detect Remove Data
        /// </summary>
        /// <param name="sourceTable">Souce Data</param>
        /// <param name="targetTable">Target Data</param>
        /// <returns>Data XML Node</returns>
        XElement DetectRemoveData(List<IDictionary<string, object>> sourceTable, List<IDictionary<string, object>> targetTable)
        {
            var dataNodeRemove = new XElement("Data");
            foreach (IDictionary<string, object> row in targetTable)
            {
                row.TryGetValue("Name", out var val);
                var exists = false;
                foreach (var data2 in sourceTable)
                {
                    exists = data2.Any(x => x.Key == "Name" && x.Value?.ToString() == val?.ToString());
                    if (exists) break;
                }

                if (!exists)
                    dataNodeRemove.Add(row.ToElement("Row"));
            }

            return dataNodeRemove;
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
                        string.Equals(x.Name, model.TableName, StringComparison.OrdinalIgnoreCase));
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

        static void SetColumnName(Column column, Transformation model)
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
        public void GenerateDifference<T>(List<T> sourceData, List<T> targetData,
            XmlWriter navigatorAdd, XmlWriter navigatorRemove, XmlWriter navigatorUpdate)
        {
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

        object FindRemoveOrUpdate<T>(T currentObject, IEnumerable<T> newList)
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

        List<T> FindNewComponent<T>(List<T> sourceList, List<T> targetList)
        {
            object tempAdd = null;
            if (sourceList == null) return new List<T>();
            if (targetList == null) return sourceList;
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

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing)
            {
            }

            GC.Collect();
            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CompareSchemaService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
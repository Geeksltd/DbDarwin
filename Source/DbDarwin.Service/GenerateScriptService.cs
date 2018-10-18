using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using DbDarwin.Model;
using DbDarwin.Model.Schema;
using KellermanSoftware.CompareNetObjects;

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


            StringBuilder sb = new StringBuilder();

            sb.AppendLine("BEGIN TRANSACTION");
            sb.AppendLine("SET QUOTED_IDENTIFIER ON");
            sb.AppendLine("SET ARITHABORT ON");
            sb.AppendLine("SET NUMERIC_ROUNDABORT OFF");
            sb.AppendLine("SET CONCAT_NULL_YIELDS_NULL ON");
            sb.AppendLine("SET ANSI_NULLS ON");
            sb.AppendLine("SET ANSI_PADDING ON");
            sb.AppendLine("SET ANSI_WARNINGS ON");
            sb.AppendLine("COMMIT");
            sb.AppendLine("BEGIN TRANSACTION ");


            if (diffFile != null)
            {
                foreach (var table in diffFile)
                {
                    if (table.Remove != null)
                    {
                        sb.AppendLine("GO");
                        if (table.Remove.Index.Count > 0)
                            sb.Append(GenerateRemoveIndexes(table.Remove.Index, table.Name));
                        if (table.Remove.ForeignKey.Count > 0)
                            sb.Append(GenerateRemoveForeignKey(table.Remove.ForeignKey, table.Name));
                        if (table.Remove.Column.Count > 0)
                            sb.Append(GenerateRemoveColumns(table.Remove.Column, table.Name));
                    }
                    if (table.Add != null)
                    {

                        sb.AppendLine("GO");
                        if (table.Add.Column.Count > 0)
                            sb.Append(GenerateNewColumns(table.Add.Column, table.Name));
                        if (table.Add.Index.Count > 0)
                            sb.Append(GenerateNewIndexes(table.Add.Index, table.Name));
                        if (table.Add.ForeignKey.Count > 0)
                            sb.Append(GenerateNewForeignKey(table.Add.ForeignKey, table.Name));
                    }

                    if (table.Update != null)
                    {

                        sb.AppendLine("GO");
                        if (table.Update.Column.Count > 0)
                            sb.Append(GenerateUpdateColumns(table.Update.Column, table.Name));
                        if (table.Update.Index.Count > 0)
                            sb.Append(GenerateUpdateIndexes(table.Update.Index, table.Name));
                        if (table.Update.ForeignKey.Count > 0)
                            sb.Append(GenerateUpdateForeignKey(table.Update.ForeignKey, table.Name));
                    }


                }
            }
            sb.AppendLine("COMMIT");

            File.WriteAllText(outputFile, sb.ToString());
        }

        private static string GenerateUpdateForeignKey(List<ForeignKey> foreignKeys, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Update ForeignKey --------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var key in foreignKeys)
            {
                CompareLogic compareLogic = new CompareLogic
                {
                    Config = { MaxDifferences = Int32.MaxValue }
                };

                if (!string.IsNullOrEmpty(key.SetName))
                {
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("GO");
                    sb.AppendLine("PRINT 'Updating ForeignKeys Name...'");
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("GO");
                    sb.AppendFormat("EXECUTE sp_rename N'{0}.{1}', N'{2}', 'OBJECT' ", tableName,
                        key.Name,
                        key.SetName);
                    key.CONSTRAINT_NAME = key.SetName;
                }

                var resultCompare = compareLogic.Compare(new Index(), key);
                if (!resultCompare.AreEqual)
                {
                    sb.AppendLine(string.Format("ALTER TABLE {0} DROP CONSTRAINT {1}", tableName, key.Name));
                    sb.AppendLine(GenerateNewForeignKey(new List<ForeignKey>() { key }, tableName));
                }

                sb.AppendLine();
                sb.AppendLine("GO");
            }
            return sb.ToString();
        }

        private static string GenerateUpdateIndexes(List<Index> indexes, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Update Index ------------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var index in indexes)
            {
                CompareLogic compareLogic = new CompareLogic
                {
                    Config = { MaxDifferences = Int32.MaxValue }
                };

                if (!string.IsNullOrEmpty(index.SetName))
                {
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("GO");
                    sb.AppendLine("PRINT 'Updating Index Name...'");
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("GO");
                    sb.AppendFormat("EXECUTE sp_rename N'{0}.{1}', N'{2}', 'Index' ", tableName,
                        index.Name,
                        index.SetName);
                    index.name = index.SetName;
                }

                var resultCompare = compareLogic.Compare(new Index(), index);
                if (!resultCompare.AreEqual)
                    sb.AppendLine(GenerateNewIndexes(new List<Index>() { index }, tableName));
                sb.AppendLine();
                sb.AppendLine("GO");
            }
            return sb.ToString();
        }

        private static string GenerateUpdateColumns(List<Column> columns, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Update Column ------------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var column in columns)
            {
                CompareLogic compareLogic = new CompareLogic
                {
                    Config = { MaxDifferences = Int32.MaxValue }
                };


                if (!string.IsNullOrEmpty(column.SetName))
                {
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("GO");
                    sb.AppendLine("PRINT 'Updating Column Name...'");
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("GO");
                    sb.AppendFormat("EXECUTE sp_rename N'{0}.{1}', N'{2}', 'COLUMN' ", tableName,
                        column.Name,
                        column.SetName);
                    column.COLUMN_NAME = column.SetName;
                }

                var resultCompare = compareLogic.Compare(new Column(), column);
                if (!resultCompare.AreEqual)
                {
                    foreach (var dif in resultCompare.Differences)
                    {
                        switch (dif.PropertyName)
                        {
                            case nameof(column.CHARACTER_MAXIMUM_LENGTH):
                                sb.AppendLine();
                                sb.AppendLine();
                                sb.AppendLine();
                                sb.AppendLine("GO");
                                sb.AppendLine("PRINT 'Updating Column Type and Length...'");
                                sb.AppendLine();
                                sb.AppendLine();
                                sb.AppendLine("GO");
                                var typeLen = string.Empty;
                                if (IsTypeHaveLength(column.DATA_TYPE))
                                {
                                    if (!string.IsNullOrEmpty(column.CHARACTER_MAXIMUM_LENGTH))
                                        typeLen = "(" + column.CHARACTER_MAXIMUM_LENGTH + ")";
                                }
                                sb.AppendFormat("ALTER TABLE [{0}] ALTER COLUMN [{1}] {2} {3} {4};", tableName,
                                    column.Name,
                                    column.DATA_TYPE,
                                    typeLen,
                                    column.IS_NULLABLE == "NO" ? "NOT NULL" : "NULL");
                                break;

                        }

                    }
                }



                sb.AppendLine();
                sb.AppendLine("GO");
            }
            return sb.ToString();
        }

        public static bool IsTypeHaveLength(string type)
        {
            List<string> types = new List<string>()
            {
                 "bigint", "bit", "date", "datetime", "float", "geography", "geometry", "hierarchyid", "image", "int",
                "money", "ntext", "real", "smalldatetime", "smallint", "smallmoney", "sql_variant", "text", "timestamp",
                "tinyint", "uniqueidentifier", "xml"
            };

            return !types.Contains(type.ToLower());
        }



        /// <summary>
        /// Generate Drop ForeignKeys
        /// </summary>
        /// <param name="foreignKeys"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private static string GenerateRemoveForeignKey(List<ForeignKey> foreignKeys, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Drop Foreign Key ---------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var key in foreignKeys)
            {
                sb.AppendFormat("ALTER TABLE [{0}] DROP CONSTRAINT [{1}]", tableName, tableName, key.Name);
                sb.AppendLine();
                sb.AppendLine("GO");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generate Drop Indexes
        /// </summary>
        /// <param name="tableName">Table Name</param>
        /// <param name="indexes">Index Collection for this table</param>
        /// <returns></returns>
        private static string GenerateRemoveIndexes(List<Index> indexes, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Drop Indexes -------------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var index in indexes)
            {
                sb.AppendFormat("DROP INDEX [{0}] ON [{1}]", index.Name, tableName);
                sb.AppendLine();
                sb.AppendLine("GO");
            }
            return sb.ToString();
        }

        private static string GenerateRemoveColumns(List<Column> columns, string tableName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Drop Columns -------------------------");
            sb.AppendLine("-----------------------------------------------------------");

            sb.AppendFormat("ALTER TABLE [{0}]", tableName);
            sb.AppendLine();
            sb.Append("\t");
            sb.AppendFormat("DROP COLUMN {0}",
                columns.Select(x => x.Name).Aggregate((x, y) => x + ", " + y).Trim(new[] { ',' }));

            sb.AppendLine();
            sb.AppendLine("GO");
            sb.Append($"ALTER TABLE {tableName} SET (LOCK_ESCALATION = TABLE)");
            sb.AppendLine();
            sb.AppendLine("GO");

            return sb.ToString();
        }

        private static string GenerateNewForeignKey(List<ForeignKey> foreignKey, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New ForeignKeys ---------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var key in foreignKey)
            {
                sb.AppendLine("GO");
                sb.AppendFormat("ALTER TABLE [{0}]  WITH CHECK ADD  CONSTRAINT [{1}] FOREIGN KEY([{2}]) \r\n", tableName, key.Name, key.COLUMN_NAME);
                sb.AppendFormat("REFERENCES [{0}] ", key.Ref_TABLE_NAME, key.Ref_COLUMN_NAME);

                sb.AppendLine(string.Format("([{0}]) ON UPDATE {1} ON DELETE {2} ", key.COLUMN_NAME, key.UPDATE_RULE,
                    key.DELETE_RULE));

                sb.AppendLine("\r\nGO");
                sb.AppendFormat("ALTER TABLE [{0}] CHECK CONSTRAINT [{1}]", tableName, key.Name);
                sb.AppendLine("\r\nGO");
            }
            return sb.ToString();
        }

        private static string GenerateNewColumns(List<Column> columns, string name)
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New Columns -------------------");
            sb.AppendLine("-----------------------------------------------------------");

            sb.AppendFormat("ALTER TABLE {0} ADD ", name);

            sb.AppendLine();
            for (var index = 0; index < columns.Count; index++)
            {
                sb.Append("\t");
                var column = columns[index];
                sb.AppendFormat("{0} {1}{2} {3}", column.COLUMN_NAME, column.DATA_TYPE,
                    string.IsNullOrEmpty(column.CHARACTER_MAXIMUM_LENGTH)
                        ? ""
                        : "(" + column.CHARACTER_MAXIMUM_LENGTH + ")",
                    column.IS_NULLABLE == "NO" ? "NOT NULL" : "NULL");
                if (columns.Count > 1 && index < columns.Count - 1)
                    sb.AppendLine(",");

            }
            sb.AppendLine();
            sb.AppendLine("GO");
            if (columns.Count > 0)
            {
                sb.Append($"ALTER TABLE {name} SET (LOCK_ESCALATION = TABLE)");
                sb.AppendLine();
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        private static string GenerateNewIndexes(List<Index> indexes, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New Indexes -------------------");
            sb.AppendLine("-----------------------------------------------------------");
            for (var i = 0; i < indexes.Count; i++)
            {
                sb.AppendLine("GO");
                var index = indexes[i];
                sb.AppendFormat("CREATE {0} NONCLUSTERED INDEX [{1}] ON {2}", index.is_unique.ToBoolean() ? "UNIQUE" : string.Empty,
                    index.Name, tableName);
                sb.AppendLine("(");

                var spited = index.Columns.Split(new char[] { '|' });
                for (var index1 = 0; index1 < spited.Length; index1++)
                {
                    var c = spited[index1];
                    sb.Append($"[{c}] ASC");
                    if (spited.Length > 1 && index1 < spited.Length - 1)
                        sb.AppendLine(",");
                }
                sb.AppendLine(")");
                if (index.has_filter.ToBoolean())
                    sb.AppendFormat("WHERE {0}", index.filter_definition);
                sb.AppendLine();
                sb.Append(" WITH (");


                sb.AppendFormat("PAD_INDEX = {0}", index.is_padded.To_ON_OFF());

                if (!string.IsNullOrEmpty(index.ignore_dup_key) && index.is_padded.To_ON_OFF() == "ON")
                {
                    if (index.is_unique.ToBoolean())
                        sb.AppendFormat(", IGNORE_DUP_KEY = {0}", index.is_padded.To_ON_OFF());
                    else
                        Console.WriteLine("Ignore duplicate values is valid only for unique indexes");
                }
                if (!string.IsNullOrEmpty(index.allow_row_locks))
                    sb.AppendFormat(", ALLOW_ROW_LOCKS = {0}", index.allow_row_locks.To_ON_OFF());
                if (!string.IsNullOrEmpty(index.allow_page_locks))
                    sb.AppendFormat(", ALLOW_PAGE_LOCKS = {0}", index.allow_page_locks.To_ON_OFF());
                if (index.fill_factor > 0)
                    sb.AppendFormat(", FILLFACTOR = {0}", index.fill_factor);

                sb.AppendFormat(", DROP_EXISTING = ON");



                sb.AppendLine(") ON [PRIMARY]");

            }

            return sb.ToString();
        }
    }
}

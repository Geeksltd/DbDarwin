using DbDarwin.Model;
using DbDarwin.Model.Schema;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using DbDarwin.Model.Command;
using Olive;

namespace DbDarwin.Service
{
    public class GenerateScriptService
    {
        public static void GenerateScript(GenerateScript model)
        {
            var serializer = new XmlSerializer(typeof(List<Table>));
            List<Table> diffFile = null;
            using (var reader = new StreamReader(model.CurrentDiffFile))
                diffFile = (List<Table>)serializer.Deserialize(reader);

            var sb = new StringBuilder();
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
                    if (table.SetName.HasValue())
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine("GO");
                        sb.AppendLine("PRINT 'Updating Table Name...'");
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine("GO");
                        sb.AppendLine(string.Format("EXECUTE sp_rename N'{0}', N'{1}', 'OBJECT' ", table.Name, table.SetName));
                        table.Name = table.SetName;
                    }

                    if (table.Remove != null)
                    {
                        sb.AppendLine("GO");
                        if (table.Remove.Index.Any())
                            sb.Append(GenerateRemoveIndexes(table.Remove.Index, table.Name));
                        if (table.Remove.ForeignKey.Any())
                            sb.Append(GenerateRemoveForeignKey(table.Remove.ForeignKey, table.Name));
                        if (table.Remove.Column.Any())
                            sb.Append(GenerateRemoveColumns(table.Remove.Column, table.Name));
                    }

                    if (table.Add != null)
                    {
                        sb.AppendLine("GO");
                        if (table.Add.Column.Any())
                            sb.Append(GenerateNewColumns(table.Add.Column, table.Name));
                        if (table.Add.PrimaryKey != null)
                            sb.Append(GenerateNewPrimaryKey(table.Add.PrimaryKey, table.Name));
                        if (table.Add.Index.Any())
                            sb.Append(GenerateNewIndexes(table.Add.Index, table.Name));
                        if (table.Add.ForeignKey.Any())
                            sb.Append(GenerateNewForeignKey(table.Add.ForeignKey, table.Name));
                    }

                    if (table.Update != null)
                    {
                        sb.AppendLine("GO");
                        if (table.Update.Column.Any())
                            sb.Append(GenerateUpdateColumns(table.Update.Column, table.Name));
                        if (table.Update.Index.Any())
                            sb.Append(GenerateUpdateIndexes(table.Update.Index, table.Name));
                        if (table.Update.ForeignKey.Any())
                            sb.Append(GenerateUpdateForeignKey(table.Update.ForeignKey, table.Name));
                    }
                }
            }

            sb.AppendLine("COMMIT");

            File.WriteAllText(model.MigrateSqlFile, sb.ToString());
        }


        /// <summary>
        /// Find PK name by table name and delete PK
        /// </summary>
        /// <param name="tableName">Table Name</param>
        /// <returns>SQL Script</returns>
        public static string GenerateDeletePkBeforeAddOrUpdate(string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendLine("------------- Remove PrimaryKey Before Add Or Update -------------");
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendFormat(@"
IF EXISTS (SELECT name FROM sys.key_constraints WHERE type = 'PK' AND OBJECT_NAME(parent_object_id) = N'{0}')
BEGIN
	PRINT 'Found one PK on table {0} and must delete it before add new or update'
	DECLARE @SQLString nvarchar(MAX)
	DECLARE @ContraintName nvarchar(1000) = (SELECT name FROM sys.key_constraints WHERE type = 'PK' AND OBJECT_NAME(parent_object_id) = N'{0}')
	SET @SQLString = N'ALTER TABLE [{0}] DROP CONSTRAINT ['+ @ContraintName+']'
	EXECUTE sp_executesql @SQLString
END
", tableName);
            return sb.ToString();
        }

        private static string GenerateNewPrimaryKey(PrimaryKey key, string tableName)
        {
            var sb = new StringBuilder();


            sb.AppendLine(GenerateDeletePkBeforeAddOrUpdate(tableName));

            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New PrimaryKey ---------------");
            sb.AppendLine("-----------------------------------------------------------");

            sb.AppendLine("GO");
            sb.AppendLine(string.Format("ALTER TABLE [{0}]  WITH CHECK ADD CONSTRAINT", tableName));
            sb.AppendLine(string.Format("\t[{0}] PRIMARY KEY {1}", key.Name, key.type_desc));
            sb.AppendLine("\t(");
            sb.AppendLine("\t" + key.Columns.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Aggregate((x, y) => $"[{x}] {(y.HasValue() ? $", [{y}]" : "")}")
                              .Trim(new[] { ',' }));
            sb.Append("\t)");
            sb.Append(" WITH ( ");

            sb.AppendFormat("PAD_INDEX = {0}", key.is_padded.To_ON_OFF());


            if (key.ignore_dup_key.HasAny() && key.ignore_dup_key.To_ON_OFF() == "OFF")
            {
                if (!key.is_unique.ToBoolean())
                    sb.AppendFormat(", IGNORE_DUP_KEY = {0}", key.ignore_dup_key.To_ON_OFF());
                else
                    Console.WriteLine("Ignore duplicate values ON not valid primary key");
            }

            if (key.allow_row_locks.HasAny())
                sb.AppendFormat(", ALLOW_ROW_LOCKS = {0}", key.allow_row_locks.To_ON_OFF());
            if (key.allow_page_locks.HasAny())
                sb.AppendFormat(", ALLOW_PAGE_LOCKS = {0}", key.allow_page_locks.To_ON_OFF());
            if (key.fill_factor > 0)
                sb.AppendFormat(", FILLFACTOR = {0}", key.fill_factor);


            sb.AppendLine(") ON [PRIMARY]");
            sb.AppendLine("GO");
            sb.AppendLine($"ALTER TABLE {tableName} SET (LOCK_ESCALATION = TABLE)");


            return sb.ToString();
        }

        static string GenerateUpdateForeignKey(IEnumerable<ForeignKey> foreignKeys, string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Update ForeignKey --------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var key in foreignKeys)
            {
                var compareLogic = new CompareLogic
                {
                    Config = { MaxDifferences = int.MaxValue }
                };

                if (key.SetName.HasValue())
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
                    sb.AppendLine(string.Format("ALTER TABLE {0} DROP CONSTRAINT [{1}]", tableName, key.Name));
                    sb.AppendLine(GenerateNewForeignKey(new List<ForeignKey> { key }, tableName));
                }

                sb.AppendLine();
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        static string GenerateUpdateIndexes(IEnumerable<Index> indexes, string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Update Index ------------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var index in indexes)
            {
                var compareLogic = new CompareLogic
                {
                    Config = { MaxDifferences = int.MaxValue }
                };

                if (index.SetName.HasValue())
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
                    sb.AppendLine(GenerateNewIndexes(new List<Index> { index }, tableName));
                sb.AppendLine();
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        static string GenerateUpdateColumns(IEnumerable<Column> columns, string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Update Column ------------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var column in columns)
            {
                var compareLogic = new CompareLogic
                {
                    Config = { MaxDifferences = int.MaxValue }
                };

                if (column.SetName.HasValue())
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
                    var listPropetyChanged = resultCompare.Differences.Select(x => x.PropertyName);
                    var detectChanges = new List<string>()
                    {
                        nameof(column.CHARACTER_MAXIMUM_LENGTH), nameof(column.NUMERIC_PRECISION), nameof(column
                            .NUMERIC_SCALE),
                        nameof(column.DATA_TYPE)
                    };
                    if (listPropetyChanged.Intersect(detectChanges).Any())
                    {

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
                            switch (column.DATA_TYPE.ToLower())
                            {
                                case "decimal":
                                case "numeric":
                                    typeLen = $"({column.NUMERIC_PRECISION},{column.NUMERIC_SCALE})";
                                    break;
                                case "datetimeoffset":
                                case "datetime2":
                                    typeLen = $"({column.DATETIME_PRECISION})";
                                    break;
                                default:
                                    if (column.CHARACTER_MAXIMUM_LENGTH.HasValue())
                                        typeLen = "(" + column.CHARACTER_MAXIMUM_LENGTH + ")";
                                    break;
                            }
                        }

                        sb.AppendFormat("ALTER TABLE [{0}] ALTER COLUMN [{1}] {2}", tableName, column.Name,
                            column.DATA_TYPE);
                        sb.AppendFormat("{0} {1};", typeLen, column.IS_NULLABLE == "NO" ? "NOT NULL" : "NULL");
                    }
                }
                sb.AppendLine();
                sb.AppendLine("GO");
            }
            return sb.ToString();
        }

        public static bool IsTypeHaveLength(string type)
        {
            var types = new List<string>
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
        /// <param name="foreignKeys">foreignKeys for remove</param>
        /// <param name="tableName">Table Name</param>
        /// <returns>sql script</returns>
        static string GenerateRemoveForeignKey(IEnumerable<ForeignKey> foreignKeys, string tableName)
        {
            var sb = new StringBuilder();
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
        /// <returns>sql script to remove object</returns>
        static string GenerateRemoveIndexes(IEnumerable<Index> indexes, string tableName)
        {
            var sb = new StringBuilder();
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

        static string GenerateRemoveColumns(IEnumerable<Column> columns, string tableName)
        {
            var sb = new StringBuilder();

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

        static string GenerateNewForeignKey(IEnumerable<ForeignKey> foreignKey, string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New ForeignKeys ---------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var key in foreignKey)
            {
                sb.AppendLine("GO");
                sb.AppendFormat("ALTER TABLE [{0}]  WITH CHECK ADD CONSTRAINT [{1}] FOREIGN KEY([{2}]) \r\n", tableName, key.Name, key.COLUMN_NAME);
                sb.AppendFormat("REFERENCES [{0}] ", key.Ref_TABLE_NAME, key.Ref_COLUMN_NAME);

                sb.AppendLine(string.Format("([{0}]) ON UPDATE {1} ON DELETE {2} ", key.COLUMN_NAME, key.UPDATE_RULE,
                    key.DELETE_RULE));

                sb.AppendLine("\r\nGO");
                sb.AppendFormat("ALTER TABLE [{0}] CHECK CONSTRAINT [{1}]", tableName, key.Name);
                sb.AppendLine("\r\nGO");
            }

            return sb.ToString();
        }

        static string GenerateNewColumns(IEnumerable<Column> columns, string name)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New Columns -------------------");
            sb.AppendLine("-----------------------------------------------------------");

            sb.AppendFormat("ALTER TABLE {0} ADD ", name);

            sb.AppendLine();

            var columnsBuilder = new StringBuilder();
            foreach (var column in columns)
            {
                columnsBuilder.Append("\t");
                columnsBuilder.AppendFormat("{0} {1}", column.COLUMN_NAME, column.DATA_TYPE);

                var typeLen = string.Empty;
                if (IsTypeHaveLength(column.DATA_TYPE))
                {
                    switch (column.DATA_TYPE.ToLower())
                    {
                        case "decimal":
                        case "numeric":
                            typeLen = $"({column.NUMERIC_PRECISION},{column.NUMERIC_SCALE})";
                            break;
                        case "datetimeoffset":
                        case "datetime2":
                            typeLen = $"({column.DATETIME_PRECISION})";
                            break;
                        default:
                            if (column.CHARACTER_MAXIMUM_LENGTH.HasValue())
                                typeLen = "(" + column.CHARACTER_MAXIMUM_LENGTH + ")";
                            break;
                    }


                }
                columnsBuilder.AppendFormat("{0} {1}", typeLen, column.IS_NULLABLE == "NO" ? "NOT NULL" : "NULL");

                columnsBuilder.AppendLine(",");
            }
            sb.Append(columnsBuilder.ToString().Trim(new[] { ',', '\r', '\n' }));
            sb.AppendLine();
            sb.AppendLine("GO");
            if (columns.Any())
            {
                sb.Append($"ALTER TABLE {name} SET (LOCK_ESCALATION = TABLE)");
                sb.AppendLine();
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        static string GenerateNewIndexes(IEnumerable<Index> indexes, string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New Indexes -------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var index in indexes)
            {
                sb.AppendLine("GO");
                sb.AppendFormat("CREATE {0} {1} INDEX [{2}] ON {3}", index.is_unique.ToBoolean() ? "UNIQUE" : string.Empty,
                    index.type_desc,
                    index.Name,
                    tableName);

                sb.AppendLine("(");

                var columnsSpited = index.Columns.Split(new char[] { '|' });
                for (var index1 = 0; index1 < columnsSpited.Length; index1++)
                {
                    var columnName = columnsSpited[index1];
                    sb.Append($"[{columnName}] ASC");
                    if (columnsSpited.Length > 1 && index1 < columnsSpited.Length - 1)
                        sb.AppendLine(",");
                }

                sb.AppendLine(")");
                if (index.has_filter.ToBoolean())
                    sb.AppendFormat("WHERE {0}", index.filter_definition);
                sb.AppendLine();
                sb.Append(" WITH (");

                sb.AppendFormat("PAD_INDEX = {0}", index.is_padded.To_ON_OFF());

                if (index.ignore_dup_key.HasAny() && index.ignore_dup_key.To_ON_OFF() == "ON")
                {
                    if (index.is_unique.ToBoolean())
                        sb.AppendFormat(", IGNORE_DUP_KEY = {0}", index.ignore_dup_key.To_ON_OFF());
                    else
                        Console.WriteLine("Ignore duplicate values is valid only for unique indexes");
                }

                if (index.allow_row_locks.HasAny())
                    sb.AppendFormat(", ALLOW_ROW_LOCKS = {0}", index.allow_row_locks.To_ON_OFF());
                if (index.allow_page_locks.HasAny())
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
using DbDarwin.Model;
using DbDarwin.Model.Command;
using DbDarwin.Model.Schema;
using KellermanSoftware.CompareNetObjects;
using Olive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DbDarwin.Service
{
    public static class ExtentionGenerateScriptService
    {
        public static string NormalData(this object data, string type)
        {
            switch (type)
            {
                case "datetimeoffset":
                case "datetime2":
                case "uniqueidentifier":
                case "char":
                case "date":
                case "datetime":
                case "ntext":
                case "nvarchar":
                case "text":
                case "varchar":
                    return $"'{data}'";
                case "bit":
                    return data.ToString().ToLower() == "true" ? "1" : "0";
                default:
                    return data.ToString();
            }
        }
    }
    public class GenerateScriptService
    {
        readonly List<GeneratedScriptResult> results;
        public GenerateScriptService()
        {
            results = new List<GeneratedScriptResult>();
        }

        public List<GeneratedScriptResult> SqlOperation(string title, string sqlScript, ViewMode mode, string fullTableName, string objectName, SQLObject objectType)
        {
            results.Add(new GeneratedScriptResult
            {
                ID = Guid.NewGuid().ToString(),
                TableName = fullTableName,
                ObjectName = objectName,
                ObjectType = objectType,
                Mode = mode,
                Title = title,
                SQLScript = sqlScript,
                Order = results.Count + 1
            });
            return results;
        }

        public List<GeneratedScriptResult> GenerateScript(GenerateScript model)
        {
            var serializer = new XmlSerializer(typeof(Database));
            Database diffFile = null;
            using (var reader = new StreamReader(model.CurrentDiffFile))
                diffFile = (Database)serializer.Deserialize(reader);

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
            var foreignKeys = new List<SqlCommandGenerated>();
            if (diffFile != null)
            {
                if (diffFile.Update != null)
                    foreach (var foreignKey in diffFile.Update.Tables.Select(x => x.Update)
                        .ExceptNull()
                        .SelectMany(c => c.ForeignKeys).ToList().GroupBy(x => new { x.TABLE_SCHEMA, x.TABLE_NAME }))
                        sb.Append(GenerateRemoveForeignKey(foreignKey.ToList(), foreignKey.Key.TABLE_NAME,
                            foreignKey.Key.TABLE_SCHEMA));

                if (diffFile.Update?.Tables != null)
                    sb.AppendLine(GenerateUpdateTables(diffFile.Update.Tables));
                if (diffFile.Add?.Tables != null)
                    sb.AppendLine(GenerateAddTables(diffFile.Add.Tables));
                if (diffFile.Remove?.Tables != null)
                    sb.AppendLine(GenerateRemoveTables(diffFile.Remove.Tables));

                if (diffFile.Add?.Tables != null)
                    foreach (var table in diffFile.Add.Tables.Where(x => x.ForeignKeys.Any()))
                    {
                        var foreignKeysTemp = GenerateNewForeignKey(table.ForeignKeys, table.Name, table.Schema);
                        if (foreignKeysTemp.Any())
                        {
                            foreignKeys.AddRange(foreignKeysTemp);
                            sb.Append(foreignKeysTemp.Select(x => x.Body).Aggregate((x, y) => x + y));
                        }
                    }

                if (diffFile.Update?.Tables != null)
                    foreach (var table in diffFile.Update.Tables)
                    {
                        var foreignKeysTemp = new List<SqlCommandGenerated>();
                        if (table.ForeignKeys.Any())
                            foreignKeysTemp.AddRange(GenerateNewForeignKey(table.ForeignKeys, table.Name, table.Schema));
                        if (table.Update?.ForeignKeys != null)
                            foreignKeysTemp.AddRange(GenerateNewForeignKey(table.Update.ForeignKeys, table.Name, table.Schema));
                        if (table.Add?.ForeignKeys != null)
                            foreignKeysTemp.AddRange(GenerateNewForeignKey(table.Add.ForeignKeys, table.Name, table.Schema));

                        if (foreignKeysTemp.Any())
                        {
                            sb.AppendLine(foreignKeysTemp.Select(x => x.Body).Aggregate((x, y) => x + y));
                            foreignKeys.AddRange(foreignKeysTemp);
                        }
                    }
            }

            sb.AppendLine("PRINT 'Schema update completed successfully'");
            sb.AppendLine("COMMIT");

            if (foreignKeys.Any())
                sb.AppendLine(foreignKeys.Select(x => x.AfterCommit).Aggregate((x, y) => x + y));

            if (diffFile?.Update != null)
                sb.AppendLine(GenerateData(diffFile.Update));

            File.WriteAllText(model.MigrateSqlFile, sb.ToString());

            return results;
        }

        /// <summary>
        /// Generate SQL Add or Update or Delete
        /// </summary>
        /// <param name="diffFile">Diff file</param>
        /// <returns>SQL Scripts</returns>
        string GenerateData(Database diffFile)
        {
            var builder = new StringBuilder();

            foreach (var table in diffFile.Tables)
            {
                if (table.Add?.Data != null)
                {
                    SqlOperation($"Add {table.Remove?.Data.Rows.Count} row data on table", GenerateInsertRows(table),
                        ViewMode.Add, $"{table.Schema}.{table.Name}", $"{table.Schema}.{table.Name}",
                        SQLObject.RowData);
                }

                if (table.Remove?.Data != null)
                {
                    SqlOperation($"Remove {table.Remove?.Data.Rows.Count} row data on table", GenerateRemoveRows(table),
                        ViewMode.Delete, $"{table.Schema}.{table.Name}", $"{table.Schema}.{table.Name}",
                        SQLObject.RowData);
                }

                if (table.Update?.Data != null)
                {
                    SqlOperation($"Update {table.Update?.Data.Rows.Count} row data on table", GenerateUpdateRows(table),
                        ViewMode.Update, $"{table.Schema}.{table.Name}", $"{table.Schema}.{table.Name}",
                        SQLObject.RowData);
                }
            }

            return results
                .Where(c => c.ObjectType == SQLObject.RowData)
                .OrderBy(c => c.Order)
                .Select(c => c.SQLScript).Aggregate((x, y) => x + "\r\n" + y);
        }

        string GenerateUpdateRows(Table table)
        {
            var sb = new StringBuilder();
            var sourceDataTable = table.Update?.Data?.Rows.ToDictionaryList();
            if (sourceDataTable == null) return sb.ToString();
            var columns = sourceDataTable.FirstOrDefault();
            var columnsSql = string.Empty;
            if (columns != null)
                columnsSql = $"UPDATE [{table.Schema}].[{table.Name}] SET ";

            foreach (var rows in sourceDataTable)
            {
                sb.AppendLine(columnsSql + GenerateUpdateData(rows, XmlExtention.ToDictionary(table.Update?.Data?.ColumnTypes)));
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        string GenerateRemoveRows(Table table)
        {
            var sb = new StringBuilder();
            var sourceDataTable = table.Remove?.Data?.Rows.ToDictionaryList();
            if (sourceDataTable == null) return sb.ToString();
            var data = "";
            sourceDataTable.ForEach(row => data += $"'{row["Name"]}',");
            sb.AppendLine($"DELETE FROM [{table.Schema}].[{table.Name}] WHERE Name IN ({data.Trim(',', ' ')})");
            sb.AppendLine("GO");
            return sb.ToString();
        }

        string GenerateInsertRows(Table table)
        {
            var sb = new StringBuilder();
            var sourceDataTable = table.Add?.Data?.Rows.ToDictionaryList();
            if (sourceDataTable == null) return sb.ToString();
            var columns = sourceDataTable.FirstOrDefault();
            var columnsSql = string.Empty;
            if (columns != null)
                columnsSql = $"INSERT [{table.Schema}].[{table.Name}] (" +
                             columns
                                 .Aggregate(columnsSql, (current, column) => current + ($"[{column.Key}]" + ", "))
                                 .Trim(',', ' ') + ") VALUES ";

            foreach (var rows in sourceDataTable)
            {
                sb.AppendLine(columnsSql + GenerateInsertData(rows, XmlExtention.ToDictionary(table.Add?.Data?.ColumnTypes)));
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        string GenerateInsertData(IDictionary<string, object> rowData, IDictionary<string, object> columnTypes)
        {
            return $"({rowData.Aggregate("", (current, data) => current + (data.Value.NormalData(columnTypes[data.Key].ToString()) + ", ")).Trim(',', ' ')})";
        }

        string GenerateUpdateData(IDictionary<string, object> rowData, IDictionary<string, object> columnTypes)
        {
            var builder = new StringBuilder();
            var condition = string.Empty;
            foreach (var row in rowData)
            {
                if (row.Key == "Name")
                    condition = $" WHERE Name = {row.Value.NormalData(columnTypes[row.Key].ToString())}";
                else
                    builder.Append($", [{row.Key}] = {row.Value.NormalData(columnTypes[row.Key].ToString())}");
            }

            builder = new StringBuilder(builder.ToString().Trim(',', ' '));
            builder.Append(condition);
            return builder.ToString();
        }

        string GenerateRemoveTables(IEnumerable<Table> tables)
        {
            var sb = new StringBuilder();
            foreach (var table in tables)
            {
                var builder = new StringBuilder();
                builder.AppendLine("GO");
                builder.AppendLine($"DROP TABLE [{table.Schema}].[{table.Name}]");

                sb.Append(builder);
                SqlOperation($"Remove Table [{table.Schema}].[{table.Name}]", builder.ToString(), ViewMode.Delete, table.FullName, table.Name, SQLObject.Table);
            }

            return sb.ToString();
        }

        string GenerateAddTables(IEnumerable<Table> tables)
        {
            var sb = new StringBuilder();
            foreach (var table in tables)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"CREATE TABLE [{table.Schema}].[{table.Name}] (");

                if (table.Columns.Any())
                    builder.AppendLine(GenerateColumns(table.Columns, table.Name));
                if (table.PrimaryKey != null)
                    builder.AppendLine(GeneratePrimaryKeyCore(table.PrimaryKey));

                builder.AppendLine(")");
                builder.AppendLine(" ON [PRIMARY]");
                if (table.Indexes.Any())
                    builder.AppendLine(GenerateNewIndexes(table.Indexes, table.Name, table.Schema, indexExists: false));
                SqlOperation($"Add New Table [{table.Schema}].[{table.Name}]", builder.ToString(), ViewMode.Add, table.FullName, table.Name, SQLObject.Table);

                sb.Append(builder);
            }

            return sb.ToString();
        }

        string GenerateUpdateTables(IEnumerable<Table> tables)
        {
            var sb = new StringBuilder();
            foreach (var table in tables)
            {
                if (table.SetName.HasValue())
                {
                    var builder = new StringBuilder();
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("GO");
                    builder.AppendLine("PRINT 'Updating Table Name...'");
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("GO");
                    builder.AppendLine($"EXECUTE sp_rename N'[{table.Schema}].[{table.Name}]', N'{table.SetName}', 'OBJECT' ");

                    SqlOperation($"Rename table name {table.Name}", builder.ToString(), ViewMode.Rename, table.FullName, table.Name, SQLObject.Table);

                    sb.Append(builder);

                    table.Name = table.SetName;
                }

                if (table.Remove != null)
                {
                    sb.AppendLine("GO");
                    if (table.Remove.PrimaryKey != null)
                        sb.Append(GenerateDeletePkBeforeAddOrUpdate(table.Name, table.Schema));
                    if (table.Remove.Indexes.Any())
                        sb.Append(GenerateRemoveIndexes(table.Remove.Indexes, table.Name, table.Schema));
                    if (table.Remove.ForeignKeys.Any())
                        sb.Append(GenerateRemoveForeignKey(table.Remove.ForeignKeys, table.Name, table.Schema));
                    if (table.Remove.Columns.Any())
                        sb.Append(GenerateRemoveColumns(table.Remove.Columns, table.Name, table.Schema));
                }

                if (table.Add != null)
                {
                    sb.AppendLine("GO");
                    if (table.Add.Columns.Any())
                        sb.Append(GenerateNewColumns(table.Add.Columns, table.Name, table.Schema));
                    if (table.Add.PrimaryKey != null)
                        sb.Append(GenerateNewPrimaryKey(table.Add.PrimaryKey, table.Name, table.Schema));
                    if (table.Add.Indexes.Any())
                        sb.Append(GenerateNewIndexes(table.Add.Indexes, table.Name, table.Schema, indexExists: false));
                }

                if (table.Update != null)
                {
                    sb.AppendLine("GO");
                    if (table.Update.Columns.Any())
                        sb.Append(GenerateUpdateColumns(table.Update.Columns, table.Name, table.Schema));
                    if (table.Update.PrimaryKey != null)
                        sb.Append(GenerateNewPrimaryKey(table.Update.PrimaryKey, table.Name, table.Schema));
                    if (table.Update.Indexes.Any())
                        sb.Append(GenerateUpdateIndexes(table.Update.Indexes, table.Name, table.Schema));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Find PK name by table name and delete PK
        /// </summary>
        /// <param name="tableName">Table Name</param>
        /// <returns>SQL Script</returns>
        public static string GenerateDeletePkBeforeAddOrUpdate(string tableName, string schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendLine("------------- Remove PrimaryKey Before Add Or Update -------------");
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendFormat(@"
IF EXISTS (SELECT name FROM sys.key_constraints WHERE type = 'PK' AND OBJECT_NAME(parent_object_id) = N'{1}' AND OBJECT_SCHEMA_NAME (parent_object_id) = N'{0}')
BEGIN
	PRINT 'Found one PK on table [{0}].[{1}] and must delete it before add new or update'
	DECLARE @SQLString nvarchar(MAX)
	DECLARE @ContraintName nvarchar(1000) = (SELECT name FROM sys.key_constraints WHERE type = 'PK'  AND OBJECT_SCHEMA_NAME (parent_object_id) = N'{0}' AND OBJECT_NAME(parent_object_id) = N'{1}')
	SET @SQLString = N'ALTER TABLE [{0}].[{1}] DROP CONSTRAINT ['+ @ContraintName+']'
	EXECUTE sp_executesql @SQLString
END
", schema, tableName);
            return sb.ToString();
        }

        /// <summary>
        /// Find Constraint by table name and delete Constraint
        /// </summary>
        /// <param name="tableName">Table Name</param>
        /// <param name="constraintName">Constraint name</param>
        /// <returns>SQL Script</returns>
        public static string GenerateDeleteConstraintBeforeAddOrUpdate(string tableName, string schema, string constraintName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendLine("------------- Remove Constraint Before Add Or Update -------------");
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendFormat(@"
IF EXISTS (SELECT name FROM sys.default_constraints WHERE type = 'D' AND OBJECT_SCHEMA_NAME (parent_object_id) = N'{0}' AND OBJECT_NAME(parent_object_id) = N'{1}' and name = N'{2}')
BEGIN
	ALTER TABLE [{0}].[{1}] DROP CONSTRAINT [{2}];
END
", schema, tableName, constraintName);
            return sb.ToString();
        }

        string GenerateNewPrimaryKey(PrimaryKey key, string tableName, string schema)
        {
            var sb = new StringBuilder();

            sb.AppendLine(GenerateDeletePkBeforeAddOrUpdate(tableName, schema));

            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New PrimaryKey ---------------");
            sb.AppendLine("-----------------------------------------------------------");

            sb.AppendLine("GO");
            sb.AppendLine($"ALTER TABLE [{schema}].[{tableName}]  WITH CHECK ADD ");
            sb.AppendLine(GeneratePrimaryKeyCore(key));
            sb.AppendLine("GO");
            sb.AppendLine($"ALTER TABLE [{schema}].[{tableName}] SET (LOCK_ESCALATION = TABLE)");
            SqlOperation($"Add New PrimaryKey {key.Name} on table [{schema}].[{tableName}]", sb.ToString(),
                ViewMode.Add, $"{schema}.{tableName}", key.Name, SQLObject.PrimaryKey);
            return sb.ToString();
        }

        static string GeneratePrimaryKeyCore(PrimaryKey key)
        {
            var sb = new StringBuilder();
            if (!key.is_system_named)
                sb.AppendFormat("CONSTRAINT [{0}]", key.name);
            sb.AppendLine($" PRIMARY KEY {key.type_desc}");
            sb.AppendLine("\t(");
            sb.AppendLine("\t" + key.Columns.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Aggregate((x, y) => $"[{x}] {(y.HasValue() ? $", [{y}]" : "")}")
                              .Trim(','));
            sb.Append("\t)");

            sb.AppendLine(PrimaryKeyOptions(key));

            sb.AppendLine("ON [PRIMARY]");

            return sb.ToString();
        }

        static string PrimaryKeyOptions(PrimaryKey key)
        {
            var sb = new StringBuilder();

            if (key.is_padded.HasValue())
                sb.AppendFormat("PAD_INDEX = {0}", key.is_padded.To_ON_OFF());

            if (key.ignore_dup_key.HasValue() && key.ignore_dup_key.ToLower() == "false")
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

            return sb.Length > 0 ? $" WITH ({sb})".Trim(',', ' ') : string.Empty;
        }

        string GenerateUpdateForeignKey(IEnumerable<ForeignKey> foreignKeys, string tableName, string schema)
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
                    sb.AppendLine($"ALTER TABLE [{schema}].[{tableName}] DROP CONSTRAINT [{key.Name}]");
                    sb.AppendLine(GenerateNewForeignKey(new List<ForeignKey> { key }, tableName, schema)
                        .Select(x => x.Full)
                        .Aggregate((x, y) => x + y));
                }

                sb.AppendLine();
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        string GenerateUpdateIndexes(IEnumerable<Index> indexes, string tableName, string schema)
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
                    var builder = new StringBuilder();

                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("GO");
                    builder.AppendLine("PRINT 'Updating Index Name...'");
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("GO");
                    builder.AppendFormat("EXECUTE sp_rename N'{0}.{1}', N'{2}', 'Index' ", tableName,
                        index.Name,
                        index.SetName);
                    index.name = index.SetName;

                    sb.Append(builder);
                    SqlOperation($"Rename index from {index.Name} to {index.SetName} on table [{schema}].[{tableName}]",
                        builder.ToString(), ViewMode.Rename, $"{schema}.{tableName}", index.Name, SQLObject.Index);
                }

                var resultCompare = compareLogic.Compare(new Index(), index);
                if (!resultCompare.AreEqual)
                {
                    var builder = new StringBuilder();

                    builder.AppendLine(GenerateNewIndexes(new List<Index> { index }, tableName, schema, indexExists: true));
                    if (index.is_disabled.ToBoolean())
                        builder.AppendLine($"ALTER INDEX [{index.Name}] ON [{schema}].[{tableName}] DISABLE");

                    SqlOperation($"Update index {index.Name} on table [{schema}].[{tableName}]", builder.ToString(), ViewMode.Update, $"{schema}.{tableName}", index.Name, SQLObject.Index);

                    sb.Append(builder);
                }

                sb.AppendLine();
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        string GenerateUpdateColumns(IEnumerable<Column> columns, string tableName, string schema)
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
                    var builder = new StringBuilder();
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("GO");
                    builder.AppendLine("PRINT 'Updating Column Name...'");
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.AppendLine("GO");
                    builder.AppendFormat("EXECUTE sp_rename N'{0}.{1}', N'{2}', 'COLUMN' ", tableName,
                        column.Name,
                        column.SetName);

                    SqlOperation(
                        $"Rename column name from {column.Name} to {column.SetName} from table [{schema}].[{tableName}]",
                        builder.ToString(), ViewMode.Rename, $"{schema}.{tableName}", column.Name, SQLObject.Column);

                    sb.Append(builder);
                    column.COLUMN_NAME = column.SetName;
                }

                var resultCompare = compareLogic.Compare(new Column(), column);
                if (!resultCompare.AreEqual)
                {
                    var listPropertyChanged = resultCompare.Differences.Select(x => x.PropertyName).ToList();
                    var detectChanges = new List<string>
                    {
                        nameof(column.CHARACTER_MAXIMUM_LENGTH),
                        nameof(column.NUMERIC_PRECISION),
                        nameof(column.NUMERIC_SCALE),
                        nameof(column.DATA_TYPE),
                        nameof(column.COLUMN_DEFAULT),
                    };
                    var intersected = listPropertyChanged.Intersect(detectChanges).ToList();
                    if (intersected.Any())
                    {
                        var builder = new StringBuilder();
                        builder.AppendLine();
                        builder.AppendLine();
                        builder.AppendLine();
                        builder.AppendLine("GO");
                        builder.AppendLine("PRINT 'Updating Column Type and Length...'");
                        builder.AppendLine();
                        builder.AppendLine();
                        builder.AppendLine("GO");

                        builder.AppendLine(GenerateDeleteConstraintBeforeAddOrUpdate(tableName, schema,
                            $"DF_{tableName}_{column.Name}"));

                        var typeLen = GenerateLength(column);
                        builder.AppendFormat("ALTER TABLE [{0}].[{1}] ALTER COLUMN [{2}] {3}", schema, tableName, column.Name,
                            column.DATA_TYPE);
                        builder.AppendFormat("{0} {1};", typeLen, column.IS_NULLABLE == "NO" ? "NOT NULL" : "NULL");
                        builder.AppendLine();
                        builder.AppendLine("GO");

                        if (listPropertyChanged.Contains(nameof(column.COLUMN_DEFAULT)))
                        {
                            builder.AppendLine();
                            builder.AppendLine(string.Format(
                                "ALTER TABLE [{0}].[{1}] ADD  CONSTRAINT [DF_{1}_{2}]  DEFAULT {3} FOR [{2}]", schema, tableName,
                                column.Name, column.COLUMN_DEFAULT));
                            builder.AppendLine("GO");
                        }

                        sb.Append(builder);
                        SqlOperation(
                            $"Update column {column.Name} from table [{schema}].[{tableName}]",
                            builder.ToString(), ViewMode.Update, $"{schema}.{tableName}", column.Name, SQLObject.Column);
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
        /// <param name="schema">Table Schema</param>
        /// <returns>sql script</returns>
        string GenerateRemoveForeignKey(IEnumerable<ForeignKey> foreignKeys, string tableName, string schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Drop Foreign Key ---------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var key in foreignKeys)
            {
                var builder = new StringBuilder();

                builder.AppendLine(
                    $"IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[{key.Name}]') AND parent_object_id = OBJECT_ID(N'[{schema}].[{tableName}]'))");
                builder.AppendLine($"ALTER TABLE [{schema}].[{tableName}] DROP CONSTRAINT [{key.Name}]");
                builder.AppendLine("GO");

                sb.Append(builder);
                SqlOperation($"Drop Foreign Key {key.Name}", builder.ToString(), ViewMode.Delete, $"{schema}.{tableName}", key.Name, SQLObject.ForeignKey);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate Drop Indexes
        /// </summary>
        /// <param name="tableName">Table Name</param>
        /// <param name="indexes">Index Collection for this table</param>
        /// <returns>sql script to remove object</returns>
        string GenerateRemoveIndexes(IEnumerable<Index> indexes, string tableName, string schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Drop Indexes -------------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var index in indexes)
            {
                var builder = new StringBuilder();

                builder.AppendFormat("DROP INDEX [{0}] ON [{1}].[{2}]", index.Name, schema, tableName);
                builder.AppendLine();
                builder.AppendLine("GO");

                sb.Append(builder);

                SqlOperation($"Drop Index Name {index.Name}", builder.ToString(), ViewMode.Delete, $"{schema}.{tableName}", $"{index.Name}", SQLObject.Index);
            }

            return sb.ToString();
        }

        string GenerateRemoveColumns(IEnumerable<Column> columns, string tableName, string schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Drop Columns -------------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var column in columns)
            {
                var builder = new StringBuilder();

                builder.Append($"ALTER TABLE [{schema}].[{tableName}]");
                //   var columnNames = columns.Select(x => x.Name).Aggregate("", (current, c) => current + $"[{c}] ,").Trim(',');
                builder.AppendLine($" DROP COLUMN [{column.Name}]");
                builder.AppendLine("GO");
                builder.AppendLine($"ALTER TABLE [{schema}].[{tableName}] SET (LOCK_ESCALATION = TABLE)");
                builder.AppendLine("GO");
                SqlOperation($"Drop column [{column.Name}] from table [{schema}].[{tableName}]", builder.ToString(),
                    ViewMode.Delete, $"{schema}.{tableName}", column.Name, SQLObject.Column);
                sb.Append(builder);
            }

            return sb.ToString();
        }

        List<SqlCommandGenerated> GenerateNewForeignKey(IEnumerable<ForeignKey> foreignKey, string tableName, string schema)
        {
            var sb = new StringBuilder();
            if (foreignKey.Any())
            {
                sb.AppendLine("-----------------------------------------------------------");
                sb.AppendLine("-------------------- Create New ForeignKeys ---------------");
                sb.AppendLine("-----------------------------------------------------------");
            }

            var commandBuilder = new List<SqlCommandGenerated>();

            foreach (var key in foreignKey)
            {
                var operationDescription =
                    $"Add new foreign key {key.COLUMN_NAME} from [{key.TABLE_SCHEMA}].[{key.TABLE_NAME}] to {key.Ref_COLUMN_NAME} on table [{key.Ref_TABLE_SCHEMA}].[{key.Ref_TABLE_NAME}] ";

                var sqlBuilder = new SqlCommandGenerated();

                sqlBuilder.AppendBody("GO", LineEnum.FirstLine);
                sqlBuilder.AppendBody($"PRINT '{operationDescription}'", LineEnum.FirstLine);
                sqlBuilder.AppendBody(string.Empty, LineEnum.FullLine);

                sqlBuilder.AppendBody("GO", LineEnum.FirstLine);
                sqlBuilder.AppendBody($"ALTER TABLE [{schema}].[{tableName}]  WITH NOCHECK", LineEnum.FirstLine);
                sqlBuilder.AppendBody($"ADD CONSTRAINT [{key.Name}] FOREIGN KEY([{key.COLUMN_NAME}])", LineEnum.FirstLineWithTab);
                sqlBuilder.AppendBody($"REFERENCES [{key.Ref_TABLE_SCHEMA}].[{key.Ref_TABLE_NAME}] ([{key.Ref_COLUMN_NAME}]) ", LineEnum.FirstLineWithTab);
                sqlBuilder.AppendBody($"ON UPDATE {key.UPDATE_RULE} ON DELETE {key.DELETE_RULE}");
                sqlBuilder.AppendBody(string.Empty, LineEnum.FullLine);

                sqlBuilder.AppendAfterCommit("GO", LineEnum.FirstLine);
                sqlBuilder.AppendAfterCommit("BEGIN TRY", LineEnum.FirstLine);
                sqlBuilder.AppendAfterCommit($"ALTER TABLE [{schema}].[{tableName}] WITH CHECK CHECK CONSTRAINT [{key.Name}]", LineEnum.FirstLineWithTab);
                sqlBuilder.AppendAfterCommit("END TRY", LineEnum.FirstLine);
                sqlBuilder.AppendAfterCommit("BEGIN CATCH", LineEnum.FirstLine);
                sqlBuilder.AppendAfterCommit("DECLARE @Msg NVARCHAR(4000) = ERROR_MESSAGE();", LineEnum.FirstLineWithTab);
                sqlBuilder.AppendAfterCommit($"Print 'You must remove or update some records in table [{schema}].[{tableName}] because:';", LineEnum.FirstLineWithTab);
                sqlBuilder.AppendAfterCommit("THROW 60000, @Msg, 1;", LineEnum.FirstLineWithTab);
                sqlBuilder.AppendAfterCommit("END CATCH", LineEnum.FirstLine);
                sqlBuilder.AppendAfterCommit(string.Empty, LineEnum.FullLine);

                commandBuilder.Add(sqlBuilder);

                // sb.Append(builder);
                SqlOperation(operationDescription, sqlBuilder.Full, ViewMode.Add, $"{schema}.{tableName}", key.Name,
                    SQLObject.ForeignKey);
            }

            return commandBuilder;
        }

        static string GenerateLength(Column column)
        {
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
                            typeLen = $"({(column.CHARACTER_MAXIMUM_LENGTH == "-1" ? "MAX" : column.CHARACTER_MAXIMUM_LENGTH)})";
                        break;
                }
            }

            return typeLen;
        }

        static string GenerateColumns(IEnumerable<Column> columns, string tableName)
        {
            var columnsBuilder = new StringBuilder();
            foreach (var column in columns)
            {
                columnsBuilder.Append("\t");
                columnsBuilder.AppendFormat("[{0}] [{1}]", column.COLUMN_NAME, column.DATA_TYPE);
                var typeLen = GenerateLength(column);
                columnsBuilder.AppendFormat("{0} {1}", typeLen, column.IS_NULLABLE == "NO" ? "NOT NULL" : "NULL");
                if (column.COLUMN_DEFAULT.HasValue())
                    columnsBuilder.AppendFormat(" CONSTRAINT [DF_{0}_{1}] DEFAULT ({2})", tableName, column.Name, column.COLUMN_DEFAULT);
                columnsBuilder.AppendLine(",");
            }

            return columnsBuilder.ToString().Trim(',', '\r', '\n');
        }

        string GenerateNewColumns(IEnumerable<Column> columns, string tableName, string schema)
        {
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                var builder = new StringBuilder();
                builder.AppendLine("-----------------------------------------------------------");
                builder.AppendLine("-------------------- Create New Columns -------------------");
                builder.AppendLine("-----------------------------------------------------------");
                builder.AppendFormat("ALTER TABLE [{0}].[{1}] ADD ", schema, tableName);
                builder.AppendLine();
                builder.AppendLine(GenerateColumns(new[] { column }, tableName));
                builder.AppendLine();
                builder.AppendLine("GO");
                builder.Append($"ALTER TABLE [{schema}].[{tableName}] SET (LOCK_ESCALATION = TABLE)");
                builder.AppendLine();
                builder.AppendLine("GO");

                SqlOperation($"Add New Column {column.Name} on table [{schema}].[{tableName}]", builder.ToString(), ViewMode.Add, $"{schema}.{tableName}", column.Name, SQLObject.Column);

                sb.Append(builder);
            }

            return sb.ToString();
        }

        string GenerateNewIndexes(IEnumerable<Index> indexes, string tableName, string schema, bool indexExists)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New Indexes -------------------");
            sb.AppendLine("-----------------------------------------------------------");
            foreach (var index in indexes)
            {
                var builder = new StringBuilder();

                builder.AppendLine("GO");
                builder.AppendFormat("CREATE {0} {1} INDEX [{2}] ON [{3}].[{4}]", index.is_unique.ToBoolean() ? "UNIQUE" : string.Empty,
                    index.type_desc,
                    index.Name,
                    schema,
                    tableName);

                builder.AppendLine("(");

                var columnsSpited = index.Columns.Split('|');
                for (var index1 = 0; index1 < columnsSpited.Length; index1++)
                {
                    var columnName = columnsSpited[index1];
                    builder.Append($"[{columnName}] ASC");
                    if (columnsSpited.Length > 1 && index1 < columnsSpited.Length - 1)
                        builder.AppendLine(",");
                }

                builder.AppendLine(")");
                if (index.has_filter.ToBoolean())
                    builder.AppendLine($"WHERE {index.filter_definition}");

                var options = IndexOptions(index, indexExists);
                if (options.HasValue())
                    builder.AppendLine(options);

                builder.AppendLine("ON [PRIMARY]");

                SqlOperation(
                    $"{(indexExists ? "Update" : "Add New")} Index {index.Name} on table [{schema}].[{tableName}]",
                    builder.ToString(), indexExists ? ViewMode.Update : ViewMode.Add, $"{schema}.{tableName}",
                    index.Name, SQLObject.Index);

                sb.Append(builder);
            }

            return sb.ToString();
        }

        static string IndexOptions(Index index, bool indexExists)
        {
            var sb = new StringBuilder();

            if (index.is_padded.HasValue())
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

            if (indexExists)
                sb.AppendFormat(", DROP_EXISTING = ON");

            return sb.Length > 0 ? string.Format(" WITH ({0})", sb.ToString().Trim(',', ' ')) : string.Empty;
        }
    }
}
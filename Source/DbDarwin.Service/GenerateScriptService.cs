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
            if (diffFile != null)
            {
                if (diffFile.Update?.Tables != null)
                    sb.AppendLine(GenerateUpdateTables(diffFile.Update.Tables));
                if (diffFile.Add?.Tables != null)
                    sb.AppendLine(GenerateAddTables(diffFile.Add.Tables));
                if (diffFile.Remove?.Tables != null)
                    sb.AppendLine(GenerateRemoveTables(diffFile.Remove.Tables));

                if (diffFile.Add?.Tables != null)
                    foreach (var table in diffFile.Add.Tables)
                    {
                        if (table.ForeignKeys.Any())
                            sb.Append(GenerateNewForeignKey(table.ForeignKeys, table.Name));
                    }
                if (diffFile.Update?.Tables != null)
                    foreach (var table in diffFile.Update.Tables)
                    {
                        if (table.ForeignKeys.Any())
                            sb.Append(GenerateNewForeignKey(table.ForeignKeys, table.Name));
                        if (table.Add?.ForeignKeys != null)
                            sb.Append(GenerateNewForeignKey(table.Add.ForeignKeys, table.Name));


                    }

            }
            sb.AppendLine("COMMIT");
            File.WriteAllText(model.MigrateSqlFile, sb.ToString());
        }

        private static string GenerateRemoveTables(IEnumerable<Table> tables)
        {
            var sb = new StringBuilder();
            foreach (var table in tables)
            {
                sb.AppendLine("GO");
                sb.AppendLine(string.Format("DROP TABLE [{0}]", table.Name));
            }
            return sb.ToString();
        }

        private static string GenerateAddTables(IEnumerable<Table> tables)
        {
            var sb = new StringBuilder();
            foreach (var table in tables)
            {
                sb.AppendLine(string.Format("CREATE TABLE [{0}] (", table.Name));

                if (table.Columns.Any())
                    sb.AppendLine(GenerateColumns(table.Columns, table.Name));
                if (table.PrimaryKey != null)
                    sb.AppendLine(GeneratePrimaryKeyCore(table.PrimaryKey));

                sb.AppendLine(")");
                sb.AppendLine(" ON [PRIMARY]");



                if (table.Indexes.Any())
                {
                    var indexExists = false;
                    sb.AppendLine(GenerateNewIndexes(table.Indexes, table.Name, indexExists));
                }


                //if (table.Add != null)
                //{
                //    sb.AppendLine("GO");
                //    if (table.Add.PrimaryKey != null)
                //        sb.Append(GenerateNewPrimaryKey(table.Add.PrimaryKey, table.Name));
                //    if (table.Add.Indexes.Any())
                //        sb.Append(GenerateNewIndexes(table.Add.Indexes, table.Name));
                //    if (table.Add.ForeignKeys.Any())
                //        sb.Append(GenerateNewForeignKey(table.Add.ForeignKeys, table.Name));
                //}


            }
            return sb.ToString();
        }

        private static string GenerateUpdateTables(IEnumerable<Table> tables)
        {
            var sb = new StringBuilder();
            foreach (var table in tables)
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
                    if (table.Remove.PrimaryKey != null)
                        sb.Append(GenerateDeletePkBeforeAddOrUpdate(table.Name));
                    if (table.Remove.Indexes.Any())
                        sb.Append(GenerateRemoveIndexes(table.Remove.Indexes, table.Name));
                    if (table.Remove.ForeignKeys.Any())
                        sb.Append(GenerateRemoveForeignKey(table.Remove.ForeignKeys, table.Name));
                    if (table.Remove.Columns.Any())
                        sb.Append(GenerateRemoveColumns(table.Remove.Columns, table.Name));
                }

                if (table.Add != null)
                {
                    sb.AppendLine("GO");
                    if (table.Add.Columns.Any())
                        sb.Append(GenerateNewColumns(table.Add.Columns, table.Name));
                    if (table.Add.PrimaryKey != null)
                        sb.Append(GenerateNewPrimaryKey(table.Add.PrimaryKey, table.Name));
                    if (table.Add.Indexes.Any())
                        sb.Append(GenerateNewIndexes(table.Add.Indexes, table.Name));

                }

                if (table.Update != null)
                {
                    sb.AppendLine("GO");
                    if (table.Update.Columns.Any())
                        sb.Append(GenerateUpdateColumns(table.Update.Columns, table.Name));
                    if (table.Update.PrimaryKey != null)
                        sb.Append(GenerateNewPrimaryKey(table.Update.PrimaryKey, table.Name));
                    if (table.Update.Indexes.Any())
                        sb.Append(GenerateUpdateIndexes(table.Update.Indexes, table.Name));

                }
            }
            return sb.ToString();
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

        /// <summary>
        /// Find Constraint by table name and delete Constraint
        /// </summary>
        /// <param name="tableName">Table Name</param>
        /// <param name="constraintName">Constraint name</param>
        /// <returns>SQL Script</returns>
        public static string GenerateDeleteConstraintBeforeAddOrUpdate(string tableName, string constraintName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendLine("------------- Remove Constraint Before Add Or Update -------------");
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendFormat(@"
IF EXISTS (SELECT name FROM sys.default_constraints WHERE type = 'D' AND OBJECT_NAME(parent_object_id) = N'{0}' and name = N'{1}')
BEGIN
	ALTER TABLE [{0}] DROP CONSTRAINT [{1}];
END
", tableName, constraintName);
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
            sb.AppendLine(string.Format("ALTER TABLE [{0}]  WITH CHECK ADD ", tableName));


            sb.AppendLine(GeneratePrimaryKeyCore(key));



            sb.AppendLine("GO");
            sb.AppendLine($"ALTER TABLE {tableName} SET (LOCK_ESCALATION = TABLE)");


            return sb.ToString();
        }

        private static string GeneratePrimaryKeyCore(PrimaryKey key)
        {
            var sb = new StringBuilder();
            if (!key.is_system_named)
                sb.AppendFormat("CONSTRAINT [{0}]", key.name);
            sb.AppendLine(string.Format(" PRIMARY KEY {0}", key.type_desc));
            sb.AppendLine("\t(");
            sb.AppendLine("\t" + key.Columns.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Aggregate((x, y) => $"[{x}] {(y.HasValue() ? $", [{y}]" : "")}")
                              .Trim(new[] { ',' }));
            sb.Append("\t)");

            sb.AppendLine(PrimaryKeyOptions(key));



            sb.AppendLine("ON [PRIMARY]");

            return sb.ToString();
        }

        private static string PrimaryKeyOptions(PrimaryKey key)
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

            return sb.Length > 0 ? string.Format(" WITH ({0})", sb).Trim(',', ' ') : string.Empty;
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
                    var listPropetyChanged = resultCompare.Differences.Select(x => x.PropertyName).ToList();
                    var detectChanges = new List<string>
                    {
                        nameof(column.CHARACTER_MAXIMUM_LENGTH),
                        nameof(column.NUMERIC_PRECISION),
                        nameof(column.NUMERIC_SCALE),
                        nameof(column.DATA_TYPE),
                        nameof(column.COLUMN_DEFAULT),
                    };
                    var intersected = listPropetyChanged.Intersect(detectChanges).ToList();
                    if (intersected.Any())
                    {

                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine("GO");
                        sb.AppendLine("PRINT 'Updating Column Type and Length...'");
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine("GO");

                        sb.AppendLine(GenerateDeleteConstraintBeforeAddOrUpdate(tableName,
                            $"DF_{tableName}_{column.Name}"));

                        var typeLen = GenerateLength(column);
                        sb.AppendFormat("ALTER TABLE [{0}] ALTER COLUMN [{1}] {2}", tableName, column.Name,
                            column.DATA_TYPE);
                        sb.AppendFormat("{0} {1};", typeLen, column.IS_NULLABLE == "NO" ? "NOT NULL" : "NULL");
                        sb.AppendLine();
                        sb.AppendLine("GO");


                        if (listPropetyChanged.Contains(nameof(column.COLUMN_DEFAULT)))
                        {

                            sb.AppendLine();
                            sb.AppendLine(string.Format(
                                "ALTER TABLE [{0}] ADD  CONSTRAINT [DF_{0}_{1}]  DEFAULT {2} FOR [{1}]", tableName,
                                column.Name, column.COLUMN_DEFAULT));
                            sb.AppendLine("GO");
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
                sb.AppendLine(string.Format(
                    "IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[{0}]') AND parent_object_id = OBJECT_ID(N'[{1}]'))",
                    key.Name, tableName));
                sb.AppendLine(string.Format("ALTER TABLE [{0}] DROP CONSTRAINT [{1}]", tableName, key.Name));
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
                sb.AppendFormat("REFERENCES [{0}] ([{1}]) ", key.Ref_TABLE_NAME, key.Ref_COLUMN_NAME);
                sb.AppendLine(string.Format("ON UPDATE {0} ON DELETE {1} ", key.UPDATE_RULE, key.DELETE_RULE));
                sb.AppendLine("\r\nGO");
                sb.AppendFormat("ALTER TABLE [{0}] CHECK CONSTRAINT [{1}]", tableName, key.Name);
                sb.AppendLine("\r\nGO");
            }

            return sb.ToString();
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

        static string GenerateNewColumns(IEnumerable<Column> columns, string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine("-------------------- Create New Columns -------------------");
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendFormat("ALTER TABLE {0} ADD ", tableName);
            sb.AppendLine();
            sb.AppendLine(GenerateColumns(columns, tableName));
            sb.AppendLine();
            sb.AppendLine("GO");
            if (columns.Any())
            {
                sb.Append($"ALTER TABLE {tableName} SET (LOCK_ESCALATION = TABLE)");
                sb.AppendLine();
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        static string GenerateNewIndexes(IEnumerable<Index> indexes, string tableName, bool indexExists = true)
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
                    sb.AppendLine(string.Format("WHERE {0}", index.filter_definition));

                var options = IndexOptions(index, indexExists);
                if (options.HasValue())
                    sb.AppendLine(options);

                sb.AppendLine("ON [PRIMARY]");
            }

            return sb.ToString();
        }

        private static string IndexOptions(Index index, bool indexExists)
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

            return sb.Length > 0 ? string.Format(" WITH ({0})", sb).Trim(',', ' ') : string.Empty;
        }
    }
}
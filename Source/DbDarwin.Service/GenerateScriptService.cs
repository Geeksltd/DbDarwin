using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using DbDarwin.Model;
using DbDarwin.Model.Schema;

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

            //      GO
            //    --GO
            //    --ALTER TABLE dbo.Table_1 SET (LOCK_ESCALATION = TABLE)
            //GO
            //    COMMIT
            //"

            if (diffFile != null)
            {
                foreach (var table in diffFile)
                {
                    if (table.Add != null)
                    {

                        sb.AppendLine("GO");
                        if (table.Add.Column.Count > 0)
                            sb.Append(GenerateNewColumns(table.Add.Column, table.Name));
                        if (table.Add.Index.Count > 0)
                            sb.Append(GenerateNewIndexes(table.Add.Index, table.Name));


                    }



                }
            }
            sb.AppendLine("COMMIT");

            File.WriteAllText(outputFile, sb.ToString());
        }

        private static string GenerateNewColumns(List<Column> columns, string name)
        {
            StringBuilder sb = new StringBuilder();
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
            sb.AppendLine();
            for (var i = 0; i < indexes.Count; i++)
            {
                sb.AppendLine("GO");
                var index = indexes[i];
                sb.AppendFormat("CREATE {0} NONCLUSTERED INDEX [{1}] ON {2}", index.is_unique.ToBoolean(), index.Name, tableName);
                sb.AppendLine("(");

                var splited = index.Columns.Split(new char[] { '|' });
                for (var index1 = 0; index1 < splited.Length; index1++)
                {
                    var c = splited[index1];
                    sb.AppendLine($"[{c}] ASC");
                    if (splited.Length > 1 && index1 < splited.Length - 1)
                        sb.AppendLine(",");
                }
                sb.Append(")");
                sb.Append(" WITH (");


                //if (!string.IsNullOrEmpty(index.is_padded))
                sb.AppendFormat("PAD_INDEX = {0}", index.is_padded.To_ON_OFF());

                if (!string.IsNullOrEmpty(index.ignore_dup_key) && index.is_padded.To_ON_OFF() == "ON")
                {
                    if (index.is_unique.ToBoolean())
                        sb.AppendFormat(", IGNORE_DUP_KEY = {0}", index.is_padded.To_ON_OFF());
                    else
                        Console.WriteLine("Ignore duplicate values is valid only for unique indexes");
                }
                //if (!string.IsNullOrEmpty(index.))
                //  sb.AppendFormat("STATISTICS_NORECOMPUTE = {0}", index.is_padded.Convert_ON_OFF());
                if (!string.IsNullOrEmpty(index.allow_row_locks))
                    sb.AppendFormat(", ALLOW_ROW_LOCKS = {0}", index.allow_row_locks.To_ON_OFF());
                if (!string.IsNullOrEmpty(index.allow_page_locks))
                    sb.AppendFormat(", ALLOW_PAGE_LOCKS = {0}", index.allow_page_locks.To_ON_OFF());
                if (index.fill_factor > 0)
                    sb.AppendFormat(", FILLFACTOR = {0}", index.fill_factor);
                sb.AppendLine(") ON [PRIMARY]");

            }

            return sb.ToString();
        }
    }
}

namespace DbDarwin.Model.Schema
{
    /// <summary>
    /// sys.index_columns
    /// </summary>
    public class index_columns
    {
        public int object_id { get; set; }

        public int index_id { get; set; }

        public int index_column_id { get; set; }

        public int column_id { get; set; }

        public byte key_ordinal { get; set; }

        public byte partition_ordinal { get; set; }

        public bool? is_descending_key { get; set; }

        public bool? is_included_column { get; set; }
    }
}
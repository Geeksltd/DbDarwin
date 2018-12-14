namespace DbDarwin.Model.Schema
{
    public class IdentityData
    {
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public int object_id { get; set; }

        public string name { get; set; }

        public int column_id { get; set; }

        public byte system_type_id { get; set; }

        public int user_type_id { get; set; }

        public short max_length { get; set; }

        public byte precision { get; set; }

        public byte scale { get; set; }

        public string collation_name { get; set; }

        public bool? is_nullable { get; set; }

        public bool is_ansi_padded { get; set; }

        public bool is_rowguidcol { get; set; }

        public bool is_identity { get; set; }

        public bool is_filestream { get; set; }

        public bool? is_replicated { get; set; }

        public bool? is_non_sql_subscribed { get; set; }

        public bool? is_merge_published { get; set; }

        public bool? is_dts_replicated { get; set; }

        public bool is_xml_document { get; set; }

        public int? xml_collection_id { get; set; }

        public int? default_object_id { get; set; }

        public int? rule_object_id { get; set; }

        public string seed_value { get; set; }

        public string increment_value { get; set; }

        public int last_value { get; set; }

        public bool? is_not_for_replication { get; set; }

        public bool is_computed { get; set; }

        public bool is_sparse { get; set; }

        public bool is_column_set { get; set; }
    }
}
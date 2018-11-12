using System;

namespace DbDarwin.Model.Schema
{
    public class SqlObject
    {
        public string schemaName { get; set; }
        public string name { get; set; }

        public int object_id { get; set; }

        public string principal_id { get; set; }

        public int schema_id { get; set; }

        public int parent_object_id { get; set; }

        public string type { get; set; }

        public string type_desc { get; set; }

        public DateTime create_date { get; set; }

        public DateTime modify_date { get; set; }

        public bool is_ms_shipped { get; set; }

        public bool is_published { get; set; }

        public bool is_schema_published { get; set; }
    }
}
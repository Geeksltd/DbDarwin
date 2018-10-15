using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace DbDarwin.Model.Schema
{
    [Serializable]
    public class Index
    {
        [XmlIgnore]
        public int object_id { get; set; }

        [XmlIgnore]
        public string Name => name;

        [XmlAttribute(AttributeName = "Name")]
        public string name { get; set; }

        [XmlAttribute]
        public string Columns { get; set; }

        [XmlAttribute]
        public int index_id { get; set; }

        [XmlAttribute]
        public byte type { get; set; }

        [XmlAttribute]
        public string type_desc { get; set; }

        [XmlAttribute]
        public string is_unique { get; set; }

        [XmlAttribute]
        public string data_space_id { get; set; }

        [XmlAttribute]
        public string ignore_dup_key { get; set; }

        [XmlAttribute]
        public string is_primary_key { get; set; }

        [XmlAttribute]
        public string is_unique_constraint { get; set; }

        [XmlAttribute]
        public byte fill_factor { get; set; }

        [XmlAttribute]
        public string is_padded { get; set; }

        [XmlAttribute]
        public string is_disabled { get; set; }

        [XmlAttribute]
        public string is_hypothetical { get; set; }

        [XmlAttribute]
        public string allow_row_locks { get; set; }

        [XmlAttribute]
        public string allow_page_locks { get; set; }

        [XmlAttribute]
        public string has_filter { get; set; }

        [XmlAttribute]
        public string filter_definition { get; set; }

    }

}

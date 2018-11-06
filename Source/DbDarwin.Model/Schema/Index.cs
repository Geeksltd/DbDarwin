using System;
using System.Xml.Serialization;

namespace DbDarwin.Model.Schema
{
    [Serializable]
    public class Index
    {
        [XmlAttribute(AttributeName = "Set-Name")]
        public string SetName { get; set; }

        [XmlIgnore]
        public int object_id { get; set; }

        [XmlIgnore]
        public string Name => name;

        [XmlAttribute(AttributeName = "Name")]
        public string name { get; set; }

        [XmlAttribute]
        public string Columns { get; set; }

        [XmlIgnore]
        public int index_id { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "Type")]
        public string type { get; set; }

        [XmlAttribute(AttributeName = "TypeDesc")]
        public string type_desc { get; set; }

        [XmlAttribute(AttributeName = "IsUnique")]
        public string is_unique { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "DataSpaceId")]
        public string data_space_id { get; set; }

        [XmlAttribute(AttributeName = "IgnoreDupKey")]
        public string ignore_dup_key { get; set; }

        [XmlAttribute(AttributeName = "IsPrimaryKey")]
        public string is_primary_key { get; set; }

        [XmlAttribute(AttributeName = "IsUniqueConstraint")]
        public string is_unique_constraint { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "FillFactor")]
        public byte fill_factor { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "IsPadded")]
        public string is_padded { get; set; }

        [XmlAttribute(AttributeName = "IsDisabled")]
        public string is_disabled { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "IsHypothetical")]
        public string is_hypothetical { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "AllowRowLocks")]
        public string allow_row_locks { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "AllowPageLocks")]
        public string allow_page_locks { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "HasFilter")]
        public string has_filter { get; set; }

        [XmlAttribute(AttributeName = "FilterDefinition")]
        public string filter_definition { get; set; }
    }


}
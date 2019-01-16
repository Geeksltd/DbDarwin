using Olive;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DbDarwin.Model.Schema
{
    [Serializable]
    public class PrimaryKey
    {
        public PrimaryKey()
        {
            is_system_named = true;
            is_unique = "True";
            ignore_dup_key = "False";
            type_desc = "CLUSTERED";
            is_disabled = "False";
        }
        [XmlAttribute(AttributeName = "Set-Name")]
        public string SetName { get; set; }

        [XmlIgnore]
        public int object_id { get; set; }

        // [XmlIgnore]
        public string Name => name;

        // [XmlIgnore]
        [XmlAttribute(AttributeName = "Name")]
        public string name { get; set; }

        [XmlAttribute]
        public string Columns { get; set; }

        [XmlIgnore]
        public int index_id { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "Type")]
        public string type { get; set; }

        [DefaultValue("CLUSTERED")]
        [XmlAttribute(AttributeName = "Type")]
        public string type_desc { get; set; }
        public bool ShouldSerializetype_desc()
        {
            return type_desc.HasValue() && type_desc.ToLower() == "NONCLUSTERED".ToLower();
        }

        [DefaultValue("True")]
        [XmlAttribute(AttributeName = "Unique")]
        public string is_unique { get; set; }

        public bool ShouldSerializeis_unique()
        {
            return is_unique.HasValue() && is_unique.ToLower() == "false";
        }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "DataSpaceId")]
        public string data_space_id { get; set; }

        [DefaultValue("False")]
        [XmlAttribute(AttributeName = "IgnoreDupKey")]
        public string ignore_dup_key { get; set; }

        public bool ShouldSerializeignore_dup_key()
        {
            return ignore_dup_key.HasValue() && ignore_dup_key.ToLower() == "true";
        }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "IsPrimaryKey")]
        public string is_primary_key { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "IsUniqueConstraint")]
        public string is_unique_constraint { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "FillFactor")]
        public byte fill_factor { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "IsPadded")]
        public string is_padded { get; set; }

        [DefaultValue("False")]
        [XmlAttribute(AttributeName = "IsDisabled")]
        public string is_disabled { get; set; }
        public bool ShouldSerializeis_disabled()
        {
            return is_disabled.HasValue() && is_disabled.ToLower() == "true";
        }

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

        [XmlIgnore]
        [XmlAttribute(AttributeName = "FilterDefinition")]
        public string filter_definition { get; set; }

        [DefaultValue(true)]
        [XmlAttribute(AttributeName = "IsSystemNamed")]
        public bool is_system_named { get; set; }
    }
}

using System;
using System.Xml;
using System.Xml.Serialization;

namespace DbDarwin.Model.Schema
{
    /// <summary>
    /// // REFERENTIAL_CONSTRAINTS
    /// </summary>
    [Serializable]

    public class ForeignKey
    {
        [XmlAttribute(AttributeName = "Set-Name")]
        public string SetName { get; set; }

        [XmlIgnore]
        public string CONSTRAINT_CATALOG { get; set; }

        [XmlAttribute(AttributeName = "ConstraintSchema")]
        public string CONSTRAINT_SCHEMA { get; set; }

        public string Name => CONSTRAINT_NAME;

        [XmlAttribute(AttributeName = "Name")]
        public string CONSTRAINT_NAME { get; set; }

        [XmlIgnore]
        public string UNIQUE_CONSTRAINT_CATALOG { get; set; }

        [XmlIgnore]
        public string UNIQUE_CONSTRAINT_SCHEMA { get; set; }

        [XmlAttribute(AttributeName = "UniqueConstraintName")]
        public string UNIQUE_CONSTRAINT_NAME { get; set; }

        [XmlAttribute(AttributeName = "MatchOption")]
        public string MATCH_OPTION { get; set; }

        [XmlAttribute(AttributeName = "UpdateRule")]
        public string UPDATE_RULE { get; set; }

        [XmlAttribute(AttributeName = "DeleteRule")]
        public string DELETE_RULE { get; set; }

        [XmlAttribute(AttributeName = "TableName")]
        public string TABLE_NAME { get; set; }

        [XmlAttribute(AttributeName = "ColumnName")]
        public string COLUMN_NAME { get; set; }

        [XmlAttribute(AttributeName = "OrdinalPosition")]
        public int ORDINAL_POSITION { get; set; }

        [XmlAttribute(AttributeName = "RefTableSchema")]
        public string Ref_TABLE_SCHEMA { get; set; }

        [XmlAttribute(AttributeName = "RefTableName")]
        public string Ref_TABLE_NAME { get; set; }

        [XmlAttribute(AttributeName = "RefColumnName")]
        public string Ref_COLUMN_NAME { get; set; }

        [XmlAttribute(AttributeName = "RefOrdinalPosition")]
        public int Ref_ORDINAL_POSITION { get; set; }
    }
}
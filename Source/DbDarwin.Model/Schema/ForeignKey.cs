using Olive;
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
        public ForeignKey()
        {
            UPDATE_RULE = "NO ACTION";
            DELETE_RULE = "NO ACTION";
            Ref_TABLE_SCHEMA = "dbo";
            CONSTRAINT_SCHEMA = "dbo";
            TABLE_SCHEMA = "dbo";
        }

        [XmlAttribute(AttributeName = "Set-Name")]
        public string SetName { get; set; }

        [XmlIgnore]
        public string CONSTRAINT_CATALOG { get; set; }

        [XmlAttribute(AttributeName = "ConstraintSchema")]
        public string CONSTRAINT_SCHEMA { get; set; }
        public bool ShouldSerializeCONSTRAINT_SCHEMA()
        {
            return CONSTRAINT_SCHEMA.HasValue() && CONSTRAINT_SCHEMA.ToLower() != "dbo".ToLower();
        }

        public string Name => CONSTRAINT_NAME;

        [XmlAttribute(AttributeName = "Name")]
        public string CONSTRAINT_NAME { get; set; }

        [XmlIgnore]
        public string UNIQUE_CONSTRAINT_CATALOG { get; set; }

        [XmlIgnore]
        public string UNIQUE_CONSTRAINT_SCHEMA { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "UniqueConstraintName")]
        public string UNIQUE_CONSTRAINT_NAME { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "MatchOption")]
        public string MATCH_OPTION { get; set; }

        [XmlAttribute(AttributeName = "UpdateRule")]
        public string UPDATE_RULE { get; set; }
        public bool ShouldSerializeUPDATE_RULE()
        {
            return UPDATE_RULE.HasValue() && UPDATE_RULE.ToLower() != "NO ACTION".ToLower();
        }

        [XmlAttribute(AttributeName = "DeleteRule")]
        public string DELETE_RULE { get; set; }
        public bool ShouldSerializeDELETE_RULE()
        {
            return DELETE_RULE.HasValue() && DELETE_RULE.ToLower() != "NO ACTION".ToLower();
        }

        public string FullTableName => TABLE_SCHEMA + "." + TABLE_NAME;

        [XmlAttribute(AttributeName = "TableSchema")]
        public string TABLE_SCHEMA { get; set; }
        public bool ShouldSerializeTABLE_SCHEMA()
        {
            return TABLE_SCHEMA.HasValue() && TABLE_SCHEMA.ToLower() != "dbo".ToLower();
        }

        [XmlAttribute(AttributeName = "TableName")]
        public string TABLE_NAME { get; set; }

        [XmlAttribute(AttributeName = "ColumnName")]
        public string COLUMN_NAME { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "OrdinalPosition")]
        public int ORDINAL_POSITION { get; set; }

        [XmlAttribute(AttributeName = "RefTableSchema")]
        public string Ref_TABLE_SCHEMA { get; set; }
        public bool ShouldSerializeRef_TABLE_SCHEMA()
        {
            return Ref_TABLE_SCHEMA.HasValue() && Ref_TABLE_SCHEMA.ToLower() != "dbo".ToLower();
        }

        [XmlAttribute(AttributeName = "RefTableName")]
        public string Ref_TABLE_NAME { get; set; }

        [XmlAttribute(AttributeName = "RefColumnName")]
        public string Ref_COLUMN_NAME { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "RefOrdinalPosition")]
        public int Ref_ORDINAL_POSITION { get; set; }
    }
}

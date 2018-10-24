using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Olive;

namespace DbDarwin.Model.Schema
{
    [Serializable]
    public class Column
    {
        [XmlIgnore]
        public string TABLE_CATALOG { get; set; }

        [XmlIgnore]
        public string TABLE_SCHEMA { get; set; }

        [XmlIgnore]
        public string TABLE_NAME { get; set; }

        [XmlAttribute(AttributeName = "Set-Name")]
        public string SetName { get; set; }

        public string Name => COLUMN_NAME;

        [XmlAttribute(AttributeName = "Name")]
        public string COLUMN_NAME { get; set; }

        [XmlAttribute(AttributeName = "OrdinalPosition")]
        public string ORDINAL_POSITION { get; set; }

        [XmlAttribute(AttributeName = "ColumnDefault")]
        public string COLUMN_DEFAULT { get; set; }

        // [DefaultValue("YES")]
        [XmlAttribute(AttributeName = "IsNullable")]
        public string IS_NULLABLE { get; set; }

        //public bool ShouldSerializeIS_NULLABLE()
        //{
        //    return IS_NULLABLE.HasValue() && IS_NULLABLE == "NO";
        //}

        [XmlAttribute(AttributeName = "DataType")]
        public string DATA_TYPE { get; set; }

        [XmlAttribute(AttributeName = "CharacterMaximumLength")]
        public string CHARACTER_MAXIMUM_LENGTH { get; set; }

        [XmlIgnore]
        public string CHARACTER_OCTET_LENGTH { get; set; }

        [XmlAttribute(AttributeName = "NumericPrecision")]
        public string NUMERIC_PRECISION { get; set; }

        [XmlAttribute(AttributeName = "NumericPrecisionRadix")]
        public string NUMERIC_PRECISION_RADIX { get; set; }

        [XmlAttribute(AttributeName = "NumericScale")]
        public string NUMERIC_SCALE { get; set; }

        [XmlAttribute(AttributeName = "DatetimePrecision")]
        public string DATETIME_PRECISION { get; set; }

        [XmlAttribute(AttributeName = "CharacterSetCatalog")]
        public string CHARACTER_SET_CATALOG { get; set; }

        [XmlAttribute(AttributeName = "CharacterSetSchema")]
        public string CHARACTER_SET_SCHEMA { get; set; }

        [XmlAttribute(AttributeName = "CharacterSetName")]
        public string CHARACTER_SET_NAME { get; set; }

        [XmlAttribute(AttributeName = "CollationCatalog")]
        public string COLLATION_CATALOG { get; set; }

        [XmlAttribute(AttributeName = "CollationSchema")]
        public string COLLATION_SCHEMA { get; set; }

        [XmlAttribute(AttributeName = "CollationName")]
        public string COLLATION_NAME { get; set; }

        [XmlAttribute(AttributeName = "DomainCatalog")]
        public string DOMAIN_CATALOG { get; set; }

        [XmlAttribute(AttributeName = "DomainSchema")]
        public string DOMAIN_SCHEMA { get; set; }

        [XmlAttribute(AttributeName = "DomainName")]
        public string DOMAIN_NAME { get; set; }
    }
}
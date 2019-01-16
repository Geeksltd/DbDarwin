using Olive;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DbDarwin.Model.Schema
{
    [Serializable]
    public class Column
    {
        public Column()
        {
            IS_NULLABLE = "YES";
            NUMERIC_PRECISION = "10";
            NUMERIC_SCALE = "0";
        }

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

        [XmlIgnore]
        [XmlAttribute(AttributeName = "OrdinalPosition")]
        public string ORDINAL_POSITION { get; set; }

        [XmlAttribute(AttributeName = "ColumnDefault")]
        public string COLUMN_DEFAULT { get; set; }

        [DefaultValue("YES")]
        [XmlAttribute(AttributeName = "Nullable")]
        public string IS_NULLABLE { get; set; }

        public bool ShouldSerializeIS_NULLABLE()
        {
            return IS_NULLABLE.HasValue() && IS_NULLABLE.ToUpper() == "NO";
        }

        [XmlAttribute(AttributeName = "DataType")]
        public string DATA_TYPE { get; set; }

        [XmlAttribute(AttributeName = "CharLimit")]
        public string CHARACTER_MAXIMUM_LENGTH { get; set; }

        [XmlIgnore]
        public string CHARACTER_OCTET_LENGTH { get; set; }

        [DefaultValue("10")]
        [XmlAttribute(AttributeName = "Precision")]
        public string NUMERIC_PRECISION { get; set; }

        public bool ShouldSerializeNUMERIC_PRECISION()
        {
            return NUMERIC_PRECISION.HasValue() && NUMERIC_PRECISION != "10";
        }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "NumericPrecisionRadix")]
        public string NUMERIC_PRECISION_RADIX { get; set; }

        [DefaultValue("0")]
        [XmlAttribute(AttributeName = "Scale")]
        public string NUMERIC_SCALE { get; set; }

        public bool ShouldSerializeNUMERIC_SCALE()
        {
            return NUMERIC_SCALE.HasValue() && NUMERIC_PRECISION != "0";
        }

        [XmlAttribute(AttributeName = "DatetimePrecision")]
        public string DATETIME_PRECISION { get; set; }

        [XmlAttribute(AttributeName = "CharacterSetCatalog")]
        public string CHARACTER_SET_CATALOG { get; set; }

        [XmlAttribute(AttributeName = "CharacterSetSchema")]
        public string CHARACTER_SET_SCHEMA { get; set; }

        [XmlIgnore]
        [XmlAttribute(AttributeName = "CharacterSetName")]
        public string CHARACTER_SET_NAME { get; set; }

        [XmlAttribute(AttributeName = "CollationCatalog")]
        public string COLLATION_CATALOG { get; set; }

        [XmlAttribute(AttributeName = "CollationSchema")]
        public string COLLATION_SCHEMA { get; set; }

        [XmlAttribute(AttributeName = "DomainCatalog")]
        public string DOMAIN_CATALOG { get; set; }

        [XmlAttribute(AttributeName = "DomainSchema")]
        public string DOMAIN_SCHEMA { get; set; }

        [XmlAttribute(AttributeName = "DomainName")]
        public string DOMAIN_NAME { get; set; }

        [CompareIgnore]
        [XmlAttribute(AttributeName = "IsIdentity")]
        public bool IsIdentity { get; set; }

        public bool ShouldSerializeIsIdentity => IsIdentity == true;

        [CompareIgnore]
        [XmlAttribute(AttributeName = "SeedValue")]
        public string SeedValue { get; set; }

        public bool ShouldSerializeSeedValue => IsIdentity == true;

        [CompareIgnore]
        [XmlAttribute(AttributeName = "IncrementValue")]
        public string IncrementValue { get; set; }

        public bool ShouldSerializeIncrementValue => IsIdentity == true;
    }
}

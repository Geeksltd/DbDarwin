using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using System.Xml.Serialization;

namespace DbDarwin.Schema
{

    [Serializable]
    public class InformationSchemaColumns
    {
        public string TABLE_CATALOG { get; set; }

        public string TABLE_SCHEMA { get; set; }

        public string TABLE_NAME { get; set; }

        [XmlAttribute]
        public string COLUMN_NAME { get; set; }

        [XmlElement(IsNullable = true)]
        [XmlAttribute]
        public int? ORDINAL_POSITION { get; set; }

        [XmlAttribute]
        public string COLUMN_DEFAULT { get; set; }

        [XmlAttribute]
        public string IS_NULLABLE { get; set; }

        [XmlAttribute]
        public string DATA_TYPE { get; set; }

        [XmlAttribute]
        public int? CHARACTER_MAXIMUM_LENGTH { get; set; }

        [XmlAttribute]
        public int? CHARACTER_OCTET_LENGTH { get; set; }

        [XmlAttribute]
        public byte? NUMERIC_PRECISION { get; set; }

        [XmlAttribute]
        public short? NUMERIC_PRECISION_RADIX { get; set; }

        [XmlAttribute]
        public int? NUMERIC_SCALE { get; set; }

        [XmlAttribute]
        public short? DATETIME_PRECISION { get; set; }

        [XmlAttribute]
        public string CHARACTER_SET_CATALOG { get; set; }

        [XmlAttribute]
        public string CHARACTER_SET_SCHEMA { get; set; }

        [XmlAttribute]
        public string CHARACTER_SET_NAME { get; set; }

        [XmlAttribute]
        public string COLLATION_CATALOG { get; set; }

        [XmlAttribute]
        public string COLLATION_SCHEMA { get; set; }

        [XmlAttribute]
        public string COLLATION_NAME { get; set; }

        [XmlAttribute]
        public string DOMAIN_CATALOG { get; set; }

        [XmlAttribute]
        public string DOMAIN_SCHEMA { get; set; }

        [XmlAttribute]
        public string DOMAIN_NAME { get; set; }

    }

}

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

        [XmlAttribute]
        public string CONSTRAINT_SCHEMA { get; set; }

        public string Name => CONSTRAINT_NAME;
        [XmlAttribute(AttributeName = "Name")]
        public string CONSTRAINT_NAME { get; set; }

        [XmlIgnore]
        public string UNIQUE_CONSTRAINT_CATALOG { get; set; }
        [XmlIgnore]
        public string UNIQUE_CONSTRAINT_SCHEMA { get; set; }

        [XmlAttribute]
        public string UNIQUE_CONSTRAINT_NAME { get; set; }
        [XmlAttribute]
        public string MATCH_OPTION { get; set; }

        [XmlAttribute]
        public string UPDATE_RULE { get; set; }

        [XmlAttribute]
        public string DELETE_RULE { get; set; }

        [XmlAttribute]
        public string TABLE_NAME { get; set; }
        [XmlAttribute]
        public string COLUMN_NAME { get; set; }

        [XmlAttribute]
        public int ORDINAL_POSITION { get; set; }

        [XmlAttribute]
        public string Ref_TABLE_SCHEMA { get; set; }

        [XmlAttribute]
        public string Ref_TABLE_NAME { get; set; }
        [XmlAttribute]
        public string Ref_COLUMN_NAME { get; set; }

        [XmlAttribute]
        public int Ref_ORDINAL_POSITION { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace DbDarwin.Model.Schema
{
    [XmlRoot("Database")]
    [Serializable]
    public class Database
    {
        [XmlElement("Table")]
        public List<Table> Tables { get; set; }

        [XmlElement("add")]
        public Database Add { get; set; }

        [XmlElement("remove")]
        public Database Remove { get; set; }

        [XmlElement("update")]
        public Database Update { get; set; }
    }
}

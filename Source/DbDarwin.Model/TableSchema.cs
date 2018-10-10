using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Xml.Serialization;
using DbDarwin.Model.Schema;

namespace DbDarwin.Model
{


    [XmlRoot("Table")]
    public class Table
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlElement("Column")]
        public List<InformationSchemaColumns> Column { get; set; }
    }


}

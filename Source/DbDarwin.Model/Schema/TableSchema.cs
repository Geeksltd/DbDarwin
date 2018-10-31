using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Xml.Serialization;
using DbDarwin.Model.Schema;

namespace DbDarwin.Model.Schema
{

    //[XmlRoot("Table")]
    [Serializable]
    public class Table
    {


        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute("Set-Name")]
        public string SetName { get; set; }

        [XmlElement("Column")]
        public List<Column> Columns { get; set; }

        [XmlElement("Index")]
        public List<Index> Indexes { get; set; }

        [XmlElement("ForeignKey")]
        public List<ForeignKey> ForeignKeys { get; set; }

        [XmlElement("PrimaryKey")]
        public PrimaryKey PrimaryKey { get; set; }





        [XmlElement("add")]
        public Table Add { get; set; }

        [XmlElement("remove")]
        public Table Remove { get; set; }

        [XmlElement("update")]
        public Table Update { get; set; }

    }


}

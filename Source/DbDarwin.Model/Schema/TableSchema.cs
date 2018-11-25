using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DbDarwin.Model.Schema;
using Olive;

namespace DbDarwin.Model.Schema
{
    [Serializable]
    public class TableData
    {
        [XmlElement("Row")]
        public List<dynamic> Rows { get; set; }

        [XmlElement("Add")]
        public TableData Add { get; set; }

        [XmlElement("Update")]
        public TableData Update { get; set; }

        [XmlElement("Delete")]
        public TableData Delete { get; set; }
    }

    //[XmlRoot("Table")]
    [Serializable]
    public class Table
    {
        public Table()
        {
            Schema = "dbo";
        }

        [XmlElement("Data")]
        public TableData Data { get; set; }

        public string FullName => Schema + "." + Name;

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Schema { get; set; }

        public bool ShouldSerializeSchema()
        {
            return Schema.HasValue() && Schema.ToLower() != "dbo".ToLower();
        }

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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Xml.Serialization;

namespace DbDarwin.SchemaXML
{


    [XmlRoot("Table")]
    public class Table
    {
        [XmlAttribute]
        public string Name { get; set; }

        public Column Column { get; set; }

        // The XmlArray attribute changes the XML element name  
        // from the default of "OrderedItems" to "Items".  
        //[XmlArray("Items")]
        //public OrderedItem[] OrderedItems;
        //public decimal SubTotal;
        //public decimal ShipCost;
        //public decimal TotalCost;
    }


    [Serializable]
    public class Column
    {

        [DefaultValue(false)]
        public bool ReadOnly { get; set; }

        [DefaultValue("")]
        public string Prefix { get; set; }

        [Browsable(false)]
        public int Ordinal { get; }

        public string Namespace { get; set; }

        [DefaultValue(-1)]
        public int MaxLength { get; set; }

        [Browsable(false)]
        public PropertyCollection ExtendedProperties { get; set; }

        [DefaultValue("")]
        [RefreshProperties(RefreshProperties.All)]
        public string Expression { get; set; }

        public object DefaultValue { get; set; }

        [DefaultValue(DataSetDateTime.UnspecifiedLocal)]
        public DataSetDateTime DateTimeMode { get; set; }

        [DefaultValue(typeof(string))]
        public Type DataType { get; set; }

        [DefaultValue("")]
        public string ColumnName { get; set; }

        [DefaultValue(1)]
        public long AutoIncrementStep { get; set; }

        public string Caption { get; set; }

        //[Browsable(false)]
        //public DataTable Table { get; }

        [DefaultValue(0)]
        public long AutoIncrementSeed { get; set; }

        [DefaultValue(false)]
        [RefreshProperties(RefreshProperties.All)]
        public bool AutoIncrement { get; set; }

        [DefaultValue(true)]
        public bool AllowDBNull { get; set; }

        [DefaultValue(MappingType.Element)]
        public virtual MappingType ColumnMapping { get; set; }

        [DefaultValue(false)]
        public bool Unique { get; set; }
    }
}

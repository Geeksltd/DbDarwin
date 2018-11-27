using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DbDarwin.Model.Schema;

namespace DbDarwin.Service
{
    public static class XmlExtention
    {
        public static XElement ToElement(this IDictionary<string, object> rows, string node)
        {
            var rowElement = new XElement(node);
            foreach (var column in rows)
            {
                rowElement.SetAttributeValue(
                    XmlConvert.EncodeName(column.Key) ?? column.Key,
                    column.Value.ToString());
            }

            return rowElement;
        }

        public static void Serialize(this XmlWriter writer, object element)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var serializer1 = new XmlSerializer(element.GetType());
            writer.WriteWhitespace("");
            serializer1.Serialize(writer, element, emptyNamespaces);
        }

        public static List<IDictionary<string, object>> ToDictionaryList(this TableData data)
        {
            var dictionary = new List<IDictionary<string, object>>();
            if (data == null)
                return dictionary;
            return data.Rows.ToDictionaryList();
        }

        public static List<IDictionary<string, object>> ToDictionaryList(this List<dynamic> data)
        {
            var dictionary = new List<IDictionary<string, object>>();
            foreach (XmlNode[] o in data)
                dictionary.Add(o.ToDictionary());
            return dictionary;
        }

        public static IDictionary<string, object> ToDictionary(this XmlNode[] data)
        {
            var expando = new ExpandoObject();
            foreach (XmlNode node in data)
                AddProperty(expando, node.Name, node.InnerText);
            return (IDictionary<string, object>)expando;
        }
        public static IDictionary<string, object> ToDictionary(dynamic data)
        {
            var expando = new ExpandoObject();
            foreach (XmlNode node in data)
                AddProperty(expando, node.Name, node.InnerText);
            return (IDictionary<string, object>)expando;
        }
        //https://www.oreilly.com/learning/building-c-objects-dynamically
        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
    }
}

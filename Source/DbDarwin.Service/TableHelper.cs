using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DbDarwin.Service
{
    public static class Helper
    {
        /// Based On https://codereview.stackexchange.com/questions/30714/converting-datatable-to-list-of-class and Modified by Hatef Rostamkhani
        /// 
        /// <summary>
        /// Converts a DataTable to a list with generic objects
        /// </summary>
        /// <typeparam name="T">Generic object</typeparam>
        /// <param name="table">DataTable</param>
        /// <returns>List with generic objects</returns>
        public static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();

                foreach (DataRow row in table.Rows)
                {
                    T obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);

                            var t = propertyInfo.PropertyType;
                            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                if (row[prop.Name] == null || row[prop.Name] == DBNull.Value)
                                    propertyInfo.SetValue(obj, default(T), null);
                                else
                                    propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], Nullable.GetUnderlyingType(t)), null);
                            }
                            else

                                propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                        }
                        catch (Exception ex)
                        {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return null;
            }
        }
    }
}

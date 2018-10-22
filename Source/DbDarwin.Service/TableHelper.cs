using System;
using System.Collections.Generic;
using System.Data;
using Olive;

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
                var list = new List<T>();

                foreach (DataRow row in table.Rows)
                {
                    var obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            var propertyInfo = obj.GetType().GetProperty(prop.Name);

                            var currentType = propertyInfo.PropertyType;
                            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                if (row[prop.Name] == null || row[prop.Name] == DBNull.Value)
                                    propertyInfo.SetValue(obj, default(T), null);
                                else
                                    propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], Nullable.GetUnderlyingType(currentType)), null);
                            }
                            else

                            {
                                var result = Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType);
                                if (propertyInfo.PropertyType == typeof(string) && (row[prop.Name] == null || row[prop.Name].ToString().IsEmpty()))
                                    propertyInfo.SetValue(obj, null, null);
                                else
                                    propertyInfo.SetValue(obj, result, null);


                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<T>();
            }
        }
    }
}

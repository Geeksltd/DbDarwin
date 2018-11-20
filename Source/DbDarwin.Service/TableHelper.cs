using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
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

                var columns = table.Columns.Cast<DataColumn>()
                    .Select(x => x.ColumnName)
                    .ToArray();

                //var objProperty = new T();
                //var propertyList = objProperty
                //    .GetType()
                //    .GetProperties()
                //    .Where(x => columns.Contains(x.Name))
                //    .ToArray();

                foreach (DataRow row in table.Rows)
                {
                    var obj = new T();

                    foreach (var propertyInfo in obj.GetType().GetProperties())
                    {
                        try
                        {
                            var currentType = propertyInfo.PropertyType;
                            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                if (row[propertyInfo.Name] == null || row[propertyInfo.Name] == DBNull.Value)
                                    propertyInfo.SetValue(obj, default(T), null);
                                else
                                    propertyInfo.SetValue(obj, Convert.ChangeType(row[propertyInfo.Name], Nullable.GetUnderlyingType(currentType)), null);
                            }
                            else

                            {
                                var result = Convert.ChangeType(row[propertyInfo.Name], propertyInfo.PropertyType);
                                if (propertyInfo.PropertyType == typeof(string) && (row[propertyInfo.Name] == null || row[propertyInfo.Name].ToString().IsEmpty()))
                                    propertyInfo.SetValue(obj, null, null);
                                else
                                    propertyInfo.SetValue(obj, result, null);


                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.Message);
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



        public static List<string> DataTableToListAsString(this DataTable table)
        {
            try
            {
                var list = new List<string>();
                var columns = table.Columns.Cast<DataColumn>()
                    .Select(x => x.ColumnName)
                    .ToArray();
                foreach (DataRow row in table.Rows)
                    list.Add(row[0].ToString());
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<string>();
            }
        }
        public static List<string> DataTableToString(this DataTable table)
        {
            try
            {
                var list = new List<string>();
                var columns = table.Columns.Cast<DataColumn>()
                    .Select(x => x.ColumnName)
                    .ToArray();
                foreach (DataRow row in table.Rows)
                    list.Add(row[0].ToString());
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<string>();
            }
        }
    }
}

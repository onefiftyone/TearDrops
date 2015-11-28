using System.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using OneFiftyOne.TearDrops.Common;

namespace OneFiftyOne.TearDrops.Repository
{
    /// <summary>
    /// Extention Methods related to data repositories
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Converts a list of T to a SortedList
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static SortedList ToSortedList<T>(this List<T> list, string keyName) where T : class
        {
            var sList = new SortedList();
            var property = typeof(T).GetProperty(keyName);
            if (property == null)
                throw new ArgumentException(string.Format("Argument '{0}' is not a valid property for {1}", keyName, typeof(T).FullName));
            foreach (var item in list)
            {
                var key = property.GetValue(item, null);
                sList.Add(key, item);
            }
            return sList;
        }


        /// <summary>
        /// Filters out objects properties that should not be inserted (appeasing the Oracle gods).
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static ExpandoObject CreateInsertableModel(this BaseModel item)
        {
            var dictionary = new ExpandoObject() as IDictionary<string, object>;
            foreach (var propertyInfo in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.DeclaringType != typeof(BaseModel)))
            {
                var attrs = propertyInfo.GetCustomAttributes(true);
                bool cont = attrs.Any(attr => attr is Computed || attr is ForeignKey || attr is Parameter);
                if (cont)
                    continue;
                dictionary.Add(propertyInfo.Name, propertyInfo.GetValue(item, null));
            }
            return (ExpandoObject)dictionary;
        }

        /// <summary>
        /// Converts a DataRow into a ExpandoObject that can be inserted by Simple.Data
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns></returns>
        public static ExpandoObject CreateInsertableModel(this DataRow row)
        {
            var dictionary = new ExpandoObject() as IDictionary<string, object>;
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                dictionary.Add(
                    row.Table.Columns[i].ColumnName,
                    row.ItemArray[i].GetType().Name == "DBNull" ? null : row.ItemArray[i]
                    );
            }

            return (ExpandoObject)dictionary;
        }

        /// <summary>
        /// Extension to mimic SqlParameterCollection.AddWithValue so it will also work with Oracle
        /// </summary>
        /// <param name="paramCollection">The parameter collection.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.Exception">Unsupported Database</exception>
        public static void AddWithValue(this IDataParameterCollection paramCollection, string name, object value)
        {
            throw new NotImplementedException();
            //if (paramCollection is SqlParameterCollection)
            //    ((SqlParameterCollection)paramCollection).AddWithValue(name, value);
            //else if (paramCollection is OracleParameterCollection)
            //    ((OracleParameterCollection)paramCollection).Add(name, value);
            //else
            //    throw new Exception("Unsupported Database");
        }

        /// <summary>
        /// Converts a list of dynamic records to a DataTable. If you dont specify the column list, it will use the key list from the first data row.
        /// If there are no data rows you will get an empty data table (no rows or columns)
        /// </summary>
        /// <param name="rows">The rows.</param>
        /// <param name="columns">The columns.</param>
        /// <returns></returns>
        public static DataTable ToDataTable(this IList<DynamicRecord> rows, List<string> columns = null)
        {
            DataTable dt = new DataTable();

            if (columns != null)
            {
                if (rows.Count > 0)
                {
                    var types = rows[0].GetTypeList();
                    for (int i = 0; i < columns.Count; i++)
                        dt.Columns.Add(columns[i], types[i]);
                }
                else
                    columns.ForEach(c => dt.Columns.Add(c));
            }
            else if (rows.Count > 0)
                foreach (var key in rows[0].Keys)
                    dt.Columns.Add(key, rows[0].GetColumnType(key));
            else
                return dt;

            foreach (var row in rows)
                dt.Rows.Add(row.Values.ToArray());

            return dt;
        }

        /// <summary>
        /// Converts a DynamicPagedResults object (of DynamicRecords) to a DataTable.
        /// </summary>
        /// <param name="rows">The rows.</param>
        /// <returns></returns>
        public static DataTable ToDataTable(this DynamicPagedResults<DynamicRecord> rows)
        {
            return rows.Items.ToDataTable(rows.Columns);
        }

        /// <summary>
        /// To the bindable list.
        /// </summary>
        /// <param name="models">The models.</param>
        /// <returns></returns>
        public static IEnumerable<IDictionary<string, object>> ToBindableList(this List<BaseModel> models)
        {
            foreach(var model in models)
            {
                var dictionary = new ExpandoObject() as IDictionary<string, object>;
                foreach (var propertyInfo in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.DeclaringType != typeof(BaseModel)))
                {
                    var attrs = propertyInfo.GetCustomAttributes(true);
                    dictionary.Add(propertyInfo.Name, propertyInfo.GetValue(model, null));
                }
                yield return (IDictionary<string, object>)dictionary;
            }
        }

        /// <summary>
        /// Creates a serializeable (JSON) list of models as expando objects. sets lazy properties to null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models">The models.</param>
        /// <returns></returns>
        public static IEnumerable<ExpandoObject> ToSerializableExpando(this IList models)
        {
            foreach (var model in models)
            {
                var dictionary = new ExpandoObject() as IDictionary<string, object>;
                foreach (var propertyInfo in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.DeclaringType != typeof(BaseModel)))
                {
                    if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Lazy<>))
                    {
                        dictionary.Add(propertyInfo.Name, null);
                    }
                    else
                    {
                        var value = propertyInfo.GetValue(model, null);
                        if (value is BaseModel)
                            value = ((BaseModel)value).ToSerializableExpando();
                        else if (value is IList)
                            value = ((IList)value).ToSerializableExpando();

                        dictionary.Add(propertyInfo.Name, value);
                    }
                }
                yield return (ExpandoObject)dictionary;
            }
        }

        /// <summary>
        /// Creates a serializeable (JSON) models as an expando objects. sets lazy properties to null.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static ExpandoObject ToSerializableExpando(this BaseModel model)
        {
            var dictionary = new ExpandoObject() as IDictionary<string, object>;
            foreach (var propertyInfo in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.DeclaringType != typeof(BaseModel)))
            {
                if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Lazy<>))
                {
                    dictionary.Add(propertyInfo.Name, null);
                }
                else
                {
                    var value = propertyInfo.GetValue(model, null);
                    if (value is BaseModel)
                        value = ((BaseModel)value).ToSerializableExpando();
                    else if (value is IList)
                        value = ((IList)value).ToSerializableExpando();

                    dictionary.Add(propertyInfo.Name, value);
                }
            }
            return (ExpandoObject)dictionary;
        }

        /// <summary>
        /// To the data model list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt">The dt.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">DataTable::ToDataModelList unable to convert DataTable to  + typeof(T).Name</exception>
        public static List<T> ToDataModelList<T>(this DataTable dt) where T : BaseModel, new()
        {
            List<T> models = new List<T>();

            try
            {
                foreach (DataRow dr in dt.Rows)
                {
                    T model = new T();
                    foreach (DataColumn dc in dt.Columns)
                        model[dc.ColumnName] = dr[dc.ColumnName];
                    models.Add(model);
                }
            }
            catch(Exception ex)
            {
                throw new Exception("DataTable::ToDataModelList unable to convert DataTable to " + typeof(T).Name, ex);
            }

            return models;
        }

        /// <summary>
        /// Gets the value from a DataReader at the specified column index, with oracle decimal precision handled.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public static object GetOracleSafeValue(this IDataReader dr, int index)
        {
            throw new NotImplementedException();
            ////Handle oracles decimal datatype fun...
            //if (BaseRepository.DBType == CPonDatabaseVendorType.Oracle)
            //{
            //    if (dr.GetFieldType(index) == typeof(decimal) && !dr.IsDBNull(index))
            //        return (decimal)(OracleDecimal.SetPrecision(((OracleDataReader)dr).GetOracleDecimal(index), 28));
            //    else
            //        return dr.GetValue(index);
            //}
            //else
            //    return dr.GetValue(index);
        }
     
    }
}

//This shenanigan is due to the fact that we cant have another ToDataTable extension method in the above namespace, 
//becuase the ones up there use dynamic objects and the compiler freaks out and tries to use the wrong one.
//So include this name space only if you actually need this function, and if you include this name space and need the other ToDataTable you are out of luck.
namespace OneFiftyOne.TearDrops.Repository.DataTableExtension
{
    /// <summary>
    /// Extention Methods related to data repositories
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// converts a list of models to a data table. only use if you absolutely have to
        /// </summary>
        /// <typeparam name="T">a sub class of BaseModel</typeparam>
        /// <param name="models">The models.</param>
        /// <param name="columnOverride">list of column names to override.</param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this IList<T> models, Dictionary<string,string> columnOverride = null, string tableNameOverride = "") where T : BaseModel, new()
        {
            var tableName = tableNameOverride.HasValue() ? tableNameOverride : new T().GetTableName();
            var dt = new DataTable(tableName);

            if (models.Count == 0)
                return dt;

            //get the properties for the type and ignore the indexer, becasue it breaks the entire world.
            var properties = models[0].GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0);

            foreach (var prop in properties)
            {
                string columnName = ((columnOverride != null && columnOverride.Any(kvp=>kvp.Key.EqualsIgnoreCase(prop.Name))) ? columnOverride[prop.Name] : prop.Name) as string;
                if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                    dt.Columns.Add(columnName, prop.PropertyType.GetGenericArguments()[0]);
                else
                    dt.Columns.Add(columnName, prop.PropertyType);
            }

            foreach (var model in models)
            {
                var dr = dt.NewRow();
                foreach (var prop in properties)
                {
                    string columnName = ((columnOverride != null && columnOverride.Any(kvp => kvp.Key.EqualsIgnoreCase(prop.Name))) ? columnOverride[prop.Name] : prop.Name) as string;
                    if (model[prop.Name] == null)
                        dr[columnName] = DBNull.Value;
                    else
                        dr[columnName] = model[prop.Name];
                }
                dt.Rows.Add(dr);
            }

            return dt;
        }
    }
}
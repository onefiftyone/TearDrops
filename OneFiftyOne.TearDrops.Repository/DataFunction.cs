using System;
using System.Collections.Generic;
using Simple.Data;
using System.Linq;

namespace OneFiftyOne.TearDrops.Repository
{
    /// <summary>
    ///     Class provides functions to manipulate queries within the context of calls to GenericRepository
    /// </summary>
    public static class DataFunction
    {
        #region IN

        /// <summary>
        ///     Function to determine if a database field is in a set of items
        /// </summary>
        /// <param name="field">The database field.</param>
        /// <param name="items">an array of values</param>
        /// <returns></returns>
        public static bool In(object field, params object[] items)
        {
            //Dummy function used only for intellisense in Repository where clause Expressions...actual function to build expression is below.
            throw new InvalidOperationException("DataFunctions can only be used in the context of Data Repository calls");
        }

        /// <summary>
        /// Function to determine if a database field is in a List<T> of items
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field">The database field.</param>
        /// <param name="items">an array of values</param>
        /// <returns></returns>
        public static bool In<T>(object field, List<T> items)
        {
            //Dummy function used only for intellisense in Repository where clause Expressions...actual function to build expression is below.
            throw new InvalidOperationException("DataFunctions can only be used in the context of Data Repository calls");
        }

        /// <summary>
        ///     The function that is actually called during expression parsing of the public 'In' function
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        internal static SimpleExpression In<DataModel>(DataStrategy db, string tableName, ObjectReference field, object[] items) where DataModel : BaseModel
        {
            var maxParam = (BaseRepository.ActiveConnection.Type == DatabaseType.SqlServer ? 2100 : 1000) - 5;

            if (items.Length <= maxParam)
                return new SimpleExpression(field, items, SimpleExpressionType.Equal);

            return new SimpleExpression(new Func<List<DataModel>, List<DataModel>>((input) =>
            {
                return input.Where(item => items.Contains(item[field.GetName()])).ToList();
            })
            , SimpleExpression.Empty, SimpleExpressionType.Empty);
        }

        /// <summary>
        ///     The function that is actually called during expression parsing of the public 'In' function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        internal static SimpleExpression In<T, DataModel>(DataStrategy db, string tableName, ObjectReference field, List<T> items) where DataModel : BaseModel
        {
            var maxParam = (BaseRepository.ActiveConnection.Type == DatabaseType.SqlServer ? 2100 : 1000) - 5;

            if (items.Count <= maxParam)
                return new SimpleExpression(field, items, SimpleExpressionType.Equal);

            return new SimpleExpression(new Func<List<DataModel>, List<DataModel>>((input) =>
            {
                return input.Where(item => items.Contains((T)item[field.GetName()])).ToList();
            })
            , SimpleExpression.Empty, SimpleExpressionType.Empty);
        }

        ///// <summary>
        /////     The function that is actually called during expression parsing of the public 'In' function
        ///// </summary>
        ///// <param name="db">The database.</param>
        ///// <param name="tableName">Name of the table.</param>
        ///// <param name="field">The field.</param>
        ///// <param name="items">The items.</param>
        ///// <returns></returns>
        //internal static SimpleExpression In_Dynamic<DataModel>(DataStrategy db, string tableName, ObjectReference field, object[] items) where DataModel : DynamicRecord
        //{
        //    var maxParam = (BaseRepository.ActiveConnection.Type == DatabaseType.SqlServer ? 2100 : 1000) - 5;

        //    if (items.Length <= maxParam)
        //        return new SimpleExpression(field, items, SimpleExpressionType.Equal);

        //    return new SimpleExpression(new Func<List<DataModel>, List<DataModel>>((input) =>
        //    {
        //        return input.Where(item => items.Contains(item[field.GetName()])).ToList();
        //    })
        //    , SimpleExpression.Empty, SimpleExpressionType.Empty);
        //}

        #endregion

        #region NOT IN

        /// <summary>
        ///     Function to determine if a database field is not in a set of items
        /// </summary>
        /// <param name="field">The database field.</param>
        /// <param name="items">an array of values</param>
        /// <returns></returns>
        public static bool NotIn(object field, params object[] items)
        {
            //Dummy function used only for intellisense in Repository where clause Expressions...actual function to build expression is below.
            throw new InvalidOperationException("DataFunctions can only be used in the context of Data Repository calls");
        }

        /// <summary>
        ///     Function to determine if a database field is not in a List<T> of items
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field">The database field.</param>
        /// <param name="items">an array of values</param>
        /// <returns></returns>
        public static bool NotIn<T>(object field, List<T> items)
        {
            //Dummy function used only for intellisense in Repository where clause Expressions...actual function to build expression is below.
            throw new InvalidOperationException("DataFunctions can only be used in the context of Data Repository calls");
        }


        /// <summary>
        ///     The function that is actually called during expression parsing of the public 'NotIn' function
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        internal static SimpleExpression NotIn<DataModel>(DataStrategy db, string tableName, ObjectReference field, object[] items) where DataModel : BaseModel
        {
            var maxParam = (BaseRepository.ActiveConnection.Type == DatabaseType.SqlServer ? 2100 : 1000) - 5;

            if (items.Length <= maxParam)
                return new SimpleExpression(field, items, SimpleExpressionType.NotEqual);

            return new SimpleExpression(new Func<List<DataModel>, List<DataModel>>((input) =>
            {
                return input.Where(item => !items.Contains(item[field.GetName()])).ToList();
            })
            , SimpleExpression.Empty, SimpleExpressionType.Empty);
        }

        /// <summary>
        ///     The function that is actually called during expression parsing of the public 'NotIn' function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        internal static SimpleExpression NotIn<T, DataModel>(DataStrategy db, string tableName, ObjectReference field, List<T> items) where DataModel : BaseModel
        {
            var maxParam = (BaseRepository.ActiveConnection.Type == DatabaseType.SqlServer ? 2100 : 1000) - 5;

            if (items.Count <= maxParam)
                return new SimpleExpression(field, items, SimpleExpressionType.NotEqual);

            return new SimpleExpression(new Func<List<DataModel>, List<DataModel>>((input) =>
            {
                return input.Where(item => !items.Contains((T)item[field.GetName()])).ToList();
            })
            , SimpleExpression.Empty, SimpleExpressionType.Empty);
        }

        #endregion

        #region BETWEEN

        /// <summary>
        ///     Function to determine if a database field is between a given range.
        /// </summary>
        /// <param name="field">The database field.</param>
        /// <param name="lowRange">The low range.</param>
        /// <param name="highRange">The high range.</param>
        /// <returns></returns>
        public static bool Between<T>(object field, T lowRange, T highRange) 
            where T : IComparable<T>
        {
            //Dummy function used only for intellisense in Repository where clause Expressions...actual function to build expression is below.
            throw new InvalidOperationException("DataFunctions can only be used in the context of Data Repository calls");
        }

        /// <summary>
        ///     The function that is actually called during expression parsing of the public 'Between' function
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="lowRange">The low range.</param>
        /// <param name="highRange">The high range.</param>
        /// <returns></returns>
        internal static SimpleExpression Between<T>(DataStrategy db, string tableName, ObjectReference field, T lowRange,
            T highRange)
            where T : IComparable<T>
        {
            Range<T> range = lowRange.to(highRange);
            return new SimpleExpression(field, range, SimpleExpressionType.Equal);
        }

        #endregion

        #region NOT BETWEEN

        /// <summary>
        ///     Function to determine if a database field is between a given range.
        /// </summary>
        /// <param name="field">The database field.</param>
        /// <param name="lowRange">The low range.</param>
        /// <param name="highRange">The high range.</param>
        /// <returns></returns>
        public static bool NotBetween<T>(object field, T lowRange, T highRange)
            where T : IComparable<T>
        {
            //Dummy function used only for intellisense in Repository where clause Expressions...actual function to build expression is below.
            throw new InvalidOperationException("DataFunctions can only be used in the context of Data Repository calls");
        }

        /// <summary>
        ///     The function that is actually called during expression parsing of the public 'Between' function
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="lowRange">The low range.</param>
        /// <param name="highRange">The high range.</param>
        /// <returns></returns>
        internal static SimpleExpression NotBetween<T>(DataStrategy db, string tableName, ObjectReference field, T lowRange,
            T highRange)
            where T : IComparable<T>
        {
            Range<T> range = lowRange.to(highRange);
            return new SimpleExpression(field, range, SimpleExpressionType.NotEqual);
        }

        #endregion

        #region LIKE

        /// <summary>
        ///     Function to determine if a database field is 'like' a search string ('%' wildcard allowed)
        /// </summary>
        /// <param name="field">The database field.</param>
        /// <param name="searchText">The search text.</param>
        /// <returns></returns>
        public static bool Like(object field, string searchText)
        {
            //Dummy function used only for intellisense in Repository where clause Expressions...actual function to build expression is below.
            throw new InvalidOperationException("DataFunctions can only be used in the context of Data Repository calls");
        }

        /// <summary>
        ///     The function that is actually called during expression parsing of the public 'Like' function
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="searchText">The search text.</param>
        /// <returns></returns>
        internal static SimpleExpression Like(DataStrategy db, string tableName, ObjectReference field, string searchText)
        {
            var func = new SimpleFunction("Like", new[] {searchText});
            return new SimpleExpression(field, func, SimpleExpressionType.Function);
        }

        #endregion

        #region NOT LIKE

        /// <summary>
        ///     Function to determine if a database field is not 'like' a search string ('%' wildcard allowed)
        /// </summary>
        /// <param name="field">The database field.</param>
        /// <param name="searchText">The search text.</param>
        /// <returns></returns>
        public static bool NotLike(object field, string searchText)
        {
            //Dummy function used only for intellisense in Repository where clause Expressions...actual function to build expression is below.
            throw new InvalidOperationException("DataFunctions can only be used in the context of Data Repository calls");
        }

        /// <summary>
        ///     The function that is actually called during expression parsing of the public 'NotLike' function
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="field">The field.</param>
        /// <param name="searchText">The search text.</param>
        /// <returns></returns>
        internal static SimpleExpression NotLike(DataStrategy db, string tableName, ObjectReference field, string searchText)
        {
            var func = new SimpleFunction("NotLike", new[] {searchText});
            return new SimpleExpression(field, func, SimpleExpressionType.Function);
        }

        #endregion
    }
}

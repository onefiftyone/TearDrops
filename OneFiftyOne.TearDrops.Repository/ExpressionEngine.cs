using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using OneFiftyOne.TearDrops.Common;

namespace OneFiftyOne.TearDrops.Repository
{
    /// <summary>
    /// Helper Extension Methods for data expressions
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Gets the hash key.
        /// </summary>
        /// <typeparam name="DataModel">The type of the ata model.</typeparam>
        /// <param name="exp">The exp.</param>
        /// <param name="postfix">The postfix.</param>
        /// <returns></returns>
        public static string GetHashKey<DataModel>(this Expression<Func<DataModel, bool>> exp, string postfix = "") where DataModel : BaseModel
        {
            return ExpressionEngine<DataModel>.GetHashKey(exp, postfix);
        }


        /// <summary>
        /// Returns a string representation of the expression
        /// </summary>
        /// <typeparam name="DataModel">The type of the ata model.</typeparam>
        /// <param name="exp">The exp.</param>
        /// <returns></returns>
        public static string ToExpressionString<DataModel>(this Expression<Func<DataModel, bool>> exp) where DataModel : BaseModel
        {
            return ExpressionEngine<DataModel>.GetExpressionString(exp);
        }

        /// <summary>
        /// Gets the name of the column represented by a expression
        /// </summary>
        /// <typeparam name="DataModel">The type of the ata model.</typeparam>
        /// <param name="columnExpression">The column expression.</param>
        /// <returns></returns>
        public static string GetColumnName<DataModel>(this Expression<Func<DataModel, object>> columnExpression) where DataModel : BaseModel
        {
            return ExpressionEngine<DataModel>.GetColumnReferenceString(columnExpression);
        }

        /// <summary>
        /// Parses to simple expression.
        /// </summary>
        /// <param name="whereClause">The where clause.</param>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public static Simple.Data.SimpleExpression ParseToSimpleExpression(this Expression<Func<DynamicRecord, bool>> whereClause, Simple.Data.DataStrategy db, string tableName)
        {
            return (Simple.Data.SimpleExpression)ExpressionEngine<BaseModel>.ParseWhereClauseForDynamicQuery(db, tableName, whereClause); 
    }

    /// <summary>
        /// Gets the column reference.
        /// </summary>
        /// <param name="columnExpression">The column expression.</param>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public static Simple.Data.ObjectReference GetColumnReference(this Expression<Func<DynamicRecord, object>> columnExpression, Simple.Data.DataStrategy db, string tableName)
        {
            var exp = ((LambdaExpression)columnExpression).Body;
            var colRef = ExpressionEngine<BaseModel>.GetColumnReferenceString(exp);
            return db[tableName][colRef];
        }
    }

    /// <summary>
    /// Class to parse Expressions into a form usable by Simple.Data
    /// </summary>
    /// <typeparam name="DataModel">The type of the data model.</typeparam>
    internal sealed class ExpressionEngine<DataModel>
        where DataModel : BaseModel
    {
        #region Public
        /// <summary>
        /// Parses the where clause Expression.
        /// </summary>
        /// <param name="db">The Simple.Data database object for this query.</param>
        /// <param name="whereClause">A LINQ expression representing the where clause of the query.</param>
        /// <param name="postProcessing">The post processing function list.</param>
        /// <returns></returns>
        public static Simple.Data.SimpleExpression ParseWhereClause(Simple.Data.DataStrategy db, Expression<Func<DataModel, bool>> whereClause, ref List<Func<List<DataModel>, List<DataModel>>> postProcessing)
        {
            return (Simple.Data.SimpleExpression)parseWhereClauseRecursive(db, GenericRepository<DataModel>.GetTableName(), whereClause.Body, ref postProcessing);
        }

        /// <summary>
        /// Parses the where clause Expression.
        /// </summary>
        /// <param name="db">The Simple.Data database object for this query.</param>
        /// <param name="whereClause">A LINQ expression representing the where clause of the query.</param>
        /// <returns></returns>
        public static Simple.Data.SimpleExpression ParseWhereClause(Simple.Data.DataStrategy db, Expression<Func<DataModel, bool>> whereClause)
        {
            List<Func<List<DataModel>, List<DataModel>>> ignore = null; //this 'disables' post processing
            return (Simple.Data.SimpleExpression)parseWhereClauseRecursive(db, GenericRepository<DataModel>.GetTableName(), whereClause.Body, ref ignore);
        }

        /// <summary>
        /// Parses the where clause for dynamic query.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="whereClause">The where clause.</param>
        /// <returns></returns>
        public static Simple.Data.SimpleExpression ParseWhereClauseForDynamicQuery(Simple.Data.DataStrategy db, string tableName, Expression<Func<DynamicRecord, bool>> whereClause)
        {
            List<Func<List<DataModel>, List<DataModel>>> ignore = null; //this 'disables' post processing
            return (Simple.Data.SimpleExpression)parseWhereClauseRecursive(db, tableName, whereClause.Body, ref ignore);
        }

        /// <summary>
        /// Builds the where clause by pk.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="db">The database.</param>
        /// <returns></returns>
        public static Simple.Data.SimpleExpression BuildWhereClauseByPK(Simple.Data.DataStrategy db, DataModel model)
        {
            var tableName = GenericRepository<DataModel>.GetTableName(model);
            Simple.Data.SimpleExpression whereClause = null;
            foreach (var prop in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if ((PrimaryKey)Attribute.GetCustomAttribute(prop, typeof(PrimaryKey)) != null)
                {
                    var value = prop.GetValue(model, null);

                    if (whereClause == null)
                        whereClause = db[tableName][prop.Name] == value;
                    else
                        whereClause &= db[tableName][prop.Name] == value;
                }
            }

            if (whereClause == null)
                throw new Exception("DataModel must have at least one primary key specified with a [PrimaryKey] attribute.");

            return whereClause;
        }

        /// <summary>
        /// Builds the where clause by anonymous object. Property names are converted to column names and are 
        /// checked for equality against the value the property is set to. ex. new { BRKEY = "000002"} would result in 
        /// WHERE BRKEY = '00002' in SQL
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="criteria">The criteria to filter on.</param>
        /// <returns></returns>
        public static Simple.Data.SimpleExpression BuildWhereClauseByObject(Simple.Data.DataStrategy db, object criteria)
        {
            if (criteria is ExpandoObject)
                return BuildWhereClauseByExpando(db, (ExpandoObject)criteria, GenericRepository<DataModel>.GetTableName());
            else
                return BuildWhereClauseByObject(db, criteria, GenericRepository<DataModel>.GetTableName());
        }

        public static Simple.Data.SimpleExpression BuildWhereClauseByObject(Simple.Data.DataStrategy db, object criteria, string tableName)
        {
            Simple.Data.SimpleExpression whereClause = null;
            foreach (var prop in criteria.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Needed to exclude the 'Item' property that the indexer adds
                if (prop.GetIndexParameters().Length != 0) continue;
                var value = prop.GetValue(criteria, null);

                if (whereClause == null)
                    whereClause = db[tableName][prop.Name] == value;
                else
                    whereClause &= db[tableName][prop.Name] == value;
            }

            return whereClause;
        }


        public static Simple.Data.SimpleExpression BuildWhereClauseByExpando(Simple.Data.DataStrategy db, ExpandoObject criteria, string tableName)
        {
            Simple.Data.SimpleExpression whereClause = null;
            foreach (var crit in (IDictionary<string, object>)criteria)
            {
                if (whereClause == null)
                    whereClause = db[tableName][crit.Key] == crit.Value;
                else
                    whereClause &= db[tableName][crit.Key] == crit.Value;
            }

            return whereClause;
        }

        /// <summary>
        /// Builds Hash key from an Expression (for caching)
        /// </summary>
        /// <param name="whereClause">The where clause.</param>
        /// <returns></returns>
        public static string GetHashKey(Expression<Func<DataModel, bool>> whereClause, string postfix = "")
        {
            string hashString = BaseRepository.GetDatabaseName() + "_" + typeof(DataModel).Name + "_";

            var tableName = GenericRepository<DataModel>.GetTableName();
            hashString += parseClauseToString(whereClause.Body, tableName);
            if (postfix.HasValue())
                hashString += "_" + postfix;

            return hashString.ToLowerInvariant().Replace(" ", string.Empty).GetHashCode().ToString();
        }

        /// <summary>
        /// Gets a string representation of the Expression
        /// </summary>
        /// <param name="whereClause">The where clause.</param>
        /// <returns></returns>
        public static string GetExpressionString(Expression<Func<DataModel, bool>> whereClause)
        {
            var tableName = GenericRepository<DataModel>.GetTableName();
            return parseClauseToString(whereClause.Body, tableName);
        }

        /// <summary>
        /// Returns a Simple.Data Column Reference from a LINQ Expression
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="columnExpression">The column expression.</param>
        /// <returns></returns>
        public static Simple.Data.ObjectReference GetColumnReference(Simple.Data.DataStrategy db, Expression<Func<DataModel, object>> columnExpression)
        {
            var tableName = GenericRepository<DataModel>.GetTableName();
            var exp = ((LambdaExpression)columnExpression).Body;
            //Unbox the object...
            if (exp is UnaryExpression)
                exp = ((UnaryExpression)exp).Operand;
            if (exp is MemberExpression)
            {
                var me = ((MemberExpression)exp);
                return db[tableName][me.Member.Name];
            }
            else if(exp is MethodCallExpression)
            {
                var mce = ((MethodCallExpression)exp);
                return db[tableName][((ConstantExpression)mce.Arguments[0]).Value.ToString()];
            }
            else
                throw new InvalidOperationException("Invalid Column Reference: " + exp.ToString());
        }

        /// <summary>
        /// Gets the column reference string.
        /// </summary>
        /// <param name="columnExpression">The column expression.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Invalid Column Expression:  + exp.ToString()</exception>
        public static string GetColumnReferenceString(Expression<Func<DataModel, object>> columnExpression)
        {
            var exp = ((LambdaExpression)columnExpression).Body;
            //Unbox the object...
            return GetColumnReferenceString(exp);
        }

        public static string GetColumnReferenceString(Expression exp)
        {
            if (exp is UnaryExpression)
                exp = ((UnaryExpression)exp).Operand;
            if (exp is MemberExpression)
            {
                var me = ((MemberExpression)exp);
                return me.Member.Name;
            }
            else if (exp is MethodCallExpression)
            {
                var mce = ((MethodCallExpression)exp);
                return ((ConstantExpression)mce.Arguments[0]).Value.ToString();
            }
            else
                throw new InvalidOperationException("Invalid Column Expression: " + exp.ToString());
        }

        /// <summary>
        /// Gets a column reference from a string containing the name in SQL notation (TableName.ColumnName).
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="colName">Name of the col.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unable to determine Column:  + colName</exception>
        public static Simple.Data.ObjectReference GetColumnReferenceFromString(Simple.Data.DataStrategy db, string colName)
        {
            colName = colName.Replace("[", "");
            colName = colName.Replace("]", "");
            var parts = colName.Split('.');
            if (parts.Count() == 1)
            {
                var tableName = GenericRepository<DataModel>.GetTableName();
                return db[tableName][parts[0]];
            }
            else if (parts.Count() > 1)
            {
                var iTbl = parts.Count() - 2;
                var iCol = parts.Count() - 1;
                return db[parts[iTbl]][parts[iCol]];
            }
            else
                throw new Exception("Unable to determine Column: " + colName);
        }

        /// <summary>
        /// Creates the join expression to get first level children for a specified query
        /// </summary>
        /// <param name="modelType">Type of the model.</param>
        /// <param name="db">The database.</param>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public static List<Tuple<PropertyInfo, bool>> QueryJoiner(Simple.Data.DataStrategy db, ref dynamic query)
        {
            List<Tuple<PropertyInfo, bool>> children = new List<Tuple<PropertyInfo, bool>>();
            var modelType = typeof(DataModel);
            var _tableName = GenericRepository<DataModel>.GetTableName();
            foreach (PropertyInfo prop in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var foreignKeys = (ForeignKey[])Attribute.GetCustomAttributes(prop, typeof(ForeignKey));
                if (!foreignKeys.Any()) continue;
                dynamic joinRef;
                Simple.Data.SimpleExpression joinExpression = null;// = new SimpleExpression()

                //get target table name
                object instance = Activator.CreateInstance(prop.PropertyType);
                var targetTable = "";
                bool isList = false;
                if (instance is IList)
                {
                    isList = true;
                    targetTable = ((Table)prop.PropertyType.GetGenericArguments().First().GetCustomAttributes(typeof(Table), false).First()).Name;
                }
                else if (instance is BaseModel)
                    targetTable = ((Table)instance.GetType().GetCustomAttributes(typeof(Table), false).First()).Name;
                else
                    continue; //[ForeignKey] was applied to something that isnt supported, skip 

                //if we got this far add property to list to retrieve grandchildren
                children.Add(Tuple.Create<PropertyInfo, bool>(prop, isList));


                query = query.LeftJoin(db[targetTable].As(prop.Name), out joinRef);
                foreach (var fk in foreignKeys)
                {
                    if (joinExpression == null)
                        joinExpression = new Simple.Data.SimpleExpression(db[_tableName][fk.Src], joinRef[fk.Dest], Simple.Data.SimpleExpressionType.Equal);
                    else
                        joinExpression &= db[_tableName][fk.Src] == joinRef[fk.Dest];
                }
                if (isList)
                    query = query.On(joinExpression).WithMany(joinRef);
                else
                    query = query.On(joinExpression).WithOne(joinRef);
            }
            return children;
        }

        #region Query String Filter Builder

        /// <summary>
        /// Builds the order column expression from query string.
        /// </summary>
        /// <param name="queryStringOrderColumn">The query string order column.</param>
        /// <returns></returns>
        public static Expression<Func<DataModel, object>> BuildOrderColumnExpressionFromQueryString(string queryStringOrderColumn)
        {
            var param = Expression.Parameter(typeof(DataModel), "m");

            PropertyInfo prop;
            try
            {
                prop = typeof(DataModel).GetProperty(queryStringOrderColumn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }
            catch (AmbiguousMatchException)
            {
                prop = typeof(DataModel).GetProperty(queryStringOrderColumn, BindingFlags.Public | BindingFlags.Instance);
            }

            var exp = Expression.Property(param, prop);
            return Expression.Lambda<Func<DataModel, object>>(exp, param);
        }

        /// <summary>
        /// Builds the order column expression from query string.
        /// </summary>
        /// <param name="queryStringOrderColumn">The query string order column.</param>
        /// <returns></returns>
        public static Expression<Func<DynamicRecord, object>> BuildDynamicOrderColumnExpressionFromQueryString(string queryStringOrderColumn)
        {
            var param = Expression.Parameter(typeof(DynamicRecord), "m");
            var meth = typeof(DynamicRecord).GetMethod("get_Item");
            var exp = Expression.Call(param, meth, new[] { Expression.Constant(queryStringOrderColumn) });

            //var exp = Expression.Property(param, prop);
            return Expression.Lambda<Func<DynamicRecord, object>>(exp, param);
        }

        /// <summary>
        /// Builds the expression from query string filter.
        /// </summary>
        /// <param name="queryStringFilter">The query string filter.</param>
        /// <returns></returns>
        //public static Expression<Func<DataModel, bool>> BuildExpressionFromQueryStringFilter(string queryStringFilter)
        //{
        //    var param = Expression.Parameter(typeof(DataModel), "m");
        //    Expression body = null;
        //    var tokens = Regex.Split(queryStringFilter, @"(?=[:\|])|(?<=[:\|])");
        //    if (tokens.Count() == 0)
        //        return null;

        //    bool isSingle = tokens.Count() == 1;

        //    if (!isSingle)
        //    {
        //        body = processExpressionPiece(param, tokens[0]);
        //        for (int i = 1; i < tokens.Count(); i += 2)
        //        {
        //            var right = processExpressionPiece(param, tokens[i + 1]);
        //            switch (tokens[i])
        //            {
        //                case ":":
        //                    body = Expression.AndAlso(body, right);
        //                    break;
        //                case "|":
        //                    body = Expression.OrElse(body, right);
        //                    break;
        //            }
        //        }
        //    }
        //    else
        //        body = processExpressionPiece(param, tokens[0]);


        //    return Expression.Lambda<Func<DataModel, bool>>(body, param);
        //}

        //public static Expression<Func<DynamicRecord, bool>> BuildDynamicExpressionFromQueryStringFilter(string queryStringFilter, List<DataDictModel> dataDictModels)
        //{
        //    var param = Expression.Parameter(typeof(DynamicRecord), "m");
        //    Expression body = null;
        //    var tokens = Regex.Split(queryStringFilter, @"(?=[:\|])|(?<=[:\|])");
        //    if (tokens.Count() == 0)
        //        return null;

        //    bool isSingle = tokens.Count() == 1;

        //    if (!isSingle)
        //    {
        //        body = processExpressionPiece(param, tokens[0], dataDictModels);
        //        for (int i = 1; i < tokens.Count(); i += 2)
        //        {
        //            var right = processExpressionPiece(param, tokens[i + 1], dataDictModels);
        //            switch (tokens[i])
        //            {
        //                case ":":
        //                    body = Expression.AndAlso(body, right);
        //                    break;
        //                case "|":
        //                    body = Expression.OrElse(body, right);
        //                    break;
        //            }
        //        }
        //    }
        //    else
        //        body = processExpressionPiece(param, tokens[0], dataDictModels);


        //    return Expression.Lambda<Func<DynamicRecord, bool>>(body, param);
        //}

        //private static Expression processExpressionPiece(ParameterExpression param, string p, List<DataDictModel> dataDictModels =null)
        //{
        //    var tokens = Regex.Split(p, @"(?=[=(!=)<(<=)>(>=)])|(?<=[=(!=)<(<=)>(>=)])");
        //    if (tokens.Count() != 3)
        //        throw new InvalidOperationException("Invalid expression: " + p);
        //    Expression left = null;
        //    ConstantExpression right = null;
        //    if (typeof(DataModel) != typeof(BaseModel))
        //    {
        //        //build expression
        //         PropertyInfo prop;
        //         try
        //         {
        //             prop = typeof(DataModel).GetProperty(tokens[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        //         }
        //         catch (AmbiguousMatchException)
        //         {
        //             prop = typeof(DataModel).GetProperty(tokens[0], BindingFlags.Public | BindingFlags.Instance);
        //         }

        //         left = Expression.Property(param, prop);
        //         object value = convertQueryStringExpressionValue(tokens[2], prop);
        //         // type = getTypeOfQueryStringExpression(tokens[2], out value);
        //         right = Expression.Constant(value, value.GetType());
        //     }
        //     else
        //     {
        //         var columnDef = dataDictModels.SingleOrDefault(d => d.COL_NAME.EqualsIgnoreCase(tokens[0]));
        //         if (columnDef == null)
        //             throw new InvalidOperationException("Data Dict missing info for " + tokens[0]);
        //         var colType = BaseRepository.GetTypeFromSQLType(columnDef.DATATYPE);
        //         var meth = typeof(DynamicRecord).GetMethod("get_Item");
        //         left = Expression.Convert(Expression.Call(param, meth, new[] { Expression.Constant(tokens[0]) }), colType);
        //         object value = Convert.ChangeType(tokens[2], colType);
        //         right = Expression.Constant(value, value.GetType());
        //     }


        //    //build operator expression
        //    switch(tokens[1])
        //    {
        //        case "=":
        //            return Expression.Equal(left, right);
        //        case "!=":
        //            return Expression.NotEqual(left, right);
        //        case "<":
        //            return Expression.LessThan(left, right);
        //        case "<=":
        //            return Expression.LessThanOrEqual(left, right);
        //        case ">":
        //            return Expression.GreaterThan(left, right);
        //        case ">=":
        //            return Expression.GreaterThanOrEqual(left, right);
        //        default:
        //            throw new InvalidOperationException("Invalid operator: " + tokens[1]);
        //    }
        //}

        private static object convertQueryStringExpressionValue(string p, PropertyInfo prop)
        {
            return Convert.ChangeType(p, prop.PropertyType); 
        }

        private static Type getTypeOfQueryStringExpression(string p, out object value)
        {
            bool isInt = false, isDouble = false, isDate = false;
            int intOut;
            double doubleOut = new double();
            DateTime dateOut = new DateTime(); ;
            isInt = Int32.TryParse(p, out intOut);
            if(!isInt)
                isDouble = Double.TryParse(p, out doubleOut);
            if (!isInt && !isDouble)
                isDate = DateTime.TryParse(p, out dateOut);

            if(isInt)
            {
                value = intOut;
                return typeof(int);
            }
            else if(isDouble)
            {
                value = doubleOut;
                return typeof(double);
            }
            else if(isDate)
            {
                value = dateOut;
                return typeof(DateTime);
            }
            else
            {
                value = p;
                return typeof(string);
            }

        }

        #endregion

        #endregion

        #region Private

        /// <summary>
        /// Recursive function to build a valid SimpleExpression from our LINQ expression
        /// </summary>
        /// <param name="db">The Simple.Data database object for this query.</param>
        /// <param name="tableName">Name of the table to query.</param>
        /// <param name="exp">The expression to parse.</param>
        /// <param name="postProcessing">List of post processing functions to be passed up to the calling function</param>
        /// <returns></returns>
        private static object parseWhereClauseRecursive(Simple.Data.DataStrategy db, string tableName, Expression exp, ref List<Func<List<DataModel>, List<DataModel>>> postProcessing)
        {
            if (exp is BinaryExpression)
            {
                var be = (BinaryExpression)exp;
                var left = parseWhereClauseRecursive(db, tableName, be.Left, ref postProcessing);
                var right = parseWhereClauseRecursive(db, tableName, be.Right, ref postProcessing);

                if (isArithmeticExpression(exp.NodeType))
                    return new Simple.Data.MathReference(left, right, mapArithmeticOperator(be.NodeType));

                return new Simple.Data.SimpleExpression(left, right, mapExpressionType(be.NodeType));
            }
            else if (exp is ConstantExpression)
            {
                var ce = (ConstantExpression)exp;
                return ce.Value;
            }
            else if (exp is MemberExpression)
            {
                var me = ((MemberExpression)exp);
                if (me.Expression is ParameterExpression)
                {
                    if (me.Expression.Type == typeof(DataModel))
                        return db[tableName][me.Member.Name];
                    else
                        return GetObjectMemberValue(((ConstantExpression)me.Expression).Value, me.Member, null);
                }
                else if (me.Expression is ConstantExpression)
                {
                    return GetObjectMemberValue(((ConstantExpression)me.Expression).Value, me.Member, null);
                }
                else if (me.Expression != null)
                {
                    object obj = Expression.Lambda<Func<Object>>(me.Expression).Compile()();
                    return GetObjectMemberValue(obj, me.Member, null);
                }
                else
                {
                    object obj = Expression.Lambda<Func<Object>>(me).Compile().Invoke();
                    return obj;
                }

            }
            else if (exp is MethodCallExpression)
            {
                var mce = ((MethodCallExpression)exp);
                MemberInfo methodCall = mce.Method;
                List<object> arguments = new List<object>();

                //Handle DataFunction functions
                if (mce.Method.DeclaringType == typeof(DataFunction))
                {
                    List<Type> parameterTypes = new List<Type>();
                    parameterTypes.Add(db.GetType());
                    parameterTypes.Add(tableName.GetType());

                    arguments.Add(db);
                    arguments.Add(tableName);

                    foreach (var arg in mce.Arguments)
                    {
                        var resolvedArg = parseWhereClauseRecursive(db, tableName, arg, ref postProcessing);
                        arguments.Add(resolvedArg);
                        parameterTypes.Add(resolvedArg.GetType());
                    }

                    //two different ways for getting the method to call depending if it is a generic message or not
                    var genericArguments = mce.Method.GetGenericArguments().ToList();
                    if (genericArguments.Count() > 0)
                    {
                        genericArguments.Add(typeof(DataModel));
                        var methodVersion = typeof(DataFunction).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                                 .Where(m => m.Name.Equals(mce.Method.Name) && m.ContainsGenericParameters == true && m.GetGenericArguments().Count() == genericArguments.Count);
                        if (methodVersion == null || methodVersion.Count() == 0)
                        {
                            genericArguments.Remove(typeof(DataModel));
                            methodVersion = typeof(DataFunction).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                               .Where(m => m.Name.Equals(mce.Method.Name) && m.ContainsGenericParameters == true && m.GetGenericArguments().Count() == genericArguments.Count);
                        }
                        if (methodVersion == null || methodVersion.Count() == 0)
                            throw new RepositoryException($"Unable to locate usable Data Fucntion overload for '{mce.Method.Name}'");

                        methodCall = methodVersion.FirstOrDefault().MakeGenericMethod(genericArguments.ToArray());
                    }
                    else
                    {
                        methodCall = typeof(DataFunction).GetMethod(mce.Method.Name
                            , BindingFlags.Static | BindingFlags.NonPublic
                            , Type.DefaultBinder
                            , parameterTypes.ToArray(), null);

                        if (((MethodBase)methodCall).ContainsGenericParameters)
                        {
                            if (typeof(DataModel) != typeof(BaseModel))
                            {
                            methodCall = typeof(DataFunction).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                             .Where(m => m.Name.Equals(mce.Method.Name) && m.ContainsGenericParameters == true && m.GetGenericArguments().Count() == genericArguments.Count)
                             .FirstOrDefault()
                             .MakeGenericMethod(new Type[] { typeof(DataModel) });
                        }
                            else //support for dynamic queries
                            {
                                methodCall = typeof(DataFunction).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                                                            .Where(m => m.Name.Equals(mce.Method.Name + "_Dynamic") && m.ContainsGenericParameters == true && m.GetGenericArguments().Count() == 1)
                                                            .FirstOrDefault()
                                                            .MakeGenericMethod(new Type[] { typeof(DynamicRecord) });
                            }
                        }
                    }
                    Simple.Data.SimpleExpression outExp = (Simple.Data.SimpleExpression)GetObjectMemberValue(parseWhereClauseRecursive(db, tableName, mce.Object, ref postProcessing), methodCall, arguments.ToArray());
                    //Handle post processing functions returned from DataFunction
                    if (outExp.Type == Simple.Data.SimpleExpressionType.Empty)
                    {
                        //in a situation where postprocessing is 'disabled' throw an error.
                        if (postProcessing == null)
                            throw new Exception("Invalid call to " + mce.Method.Name);

                        postProcessing.Add((Func<List<DataModel>, List<DataModel>>)outExp.LeftOperand);
                        return outExp.RightOperand;
                    }
                    else
                        return outExp;
                }
                else if (mce.Object != null && mce.Object.Type == typeof(DynamicRecord) && typeof(DataModel) == typeof(BaseModel))
                {
                    //if this is a where clause for a dynamic query (not using datamodels) this will get us the SimpleReference to the table name.
                    if (mce.Arguments[0] is ConstantExpression)
                        return db[tableName][((ConstantExpression)mce.Arguments[0]).Value.ToString()];
                    else if (mce.Arguments[0] is MemberExpression)
                        return db[tableName][(string)parseWhereClauseRecursive(db, tableName, mce.Arguments[0], ref postProcessing)];
                    else
                        throw new Exception("Invalid Expression type: " + mce.Arguments[0].GetType());
                }
                //Handle operations called on models (really, really should avoid this)
                else if (mce.Object is MemberExpression)
                {
                    var parameter = ((MemberExpression)mce.Object).Expression;
                    if (parameter is ParameterExpression)
                    {
                        var type = ((ParameterExpression)parameter).Type;
                        if (type == typeof(DataModel))
                        {
                            //Handle .Equals Nicely. Everything else gets an exception!
                            if (mce.Method.Name.Equals("Equals"))
                                return new Simple.Data.SimpleExpression(parseWhereClauseRecursive(db, tableName, mce.Object, ref postProcessing)
                                    , parseWhereClauseRecursive(db, tableName, mce.Arguments[0], ref postProcessing)
                                    , Simple.Data.SimpleExpressionType.Equal);
                        }
                        else
                            throw new RepositoryException($"Cannot apply method: {mce.Method.Name} to DataModel in an Expression");
                    }
                }
                else
                {
                    var annonPostProcessing = new List<Func<List<DataModel>, List<DataModel>>>();
                    arguments.AddRange(mce.Arguments.Select(arg => parseWhereClauseRecursive(db, tableName, arg, ref annonPostProcessing)));
                    postProcessing.AddRange(annonPostProcessing);
                }

                return GetObjectMemberValue(parseWhereClauseRecursive(db, tableName, mce.Object, ref postProcessing), methodCall, arguments.ToArray());
            }
            else if (exp is UnaryExpression)
            {
                var unaryExp = (UnaryExpression)exp;
                if (unaryExp.Operand != null || unaryExp.NodeType == ExpressionType.Convert || unaryExp.NodeType == ExpressionType.ConvertChecked)
                    return parseWhereClauseRecursive(db, tableName, unaryExp.Operand, ref postProcessing);

                throw new InvalidOperationException("Invalid Expression: " + exp.ToString());
            }
            else if (exp is NewArrayExpression)
            {
                var nae = (NewArrayExpression)exp;
                foreach (var innerE in nae.Expressions)
                {
                    if (innerE is ListInitExpression)
                        return parseWhereClauseRecursive(db, tableName, innerE, ref postProcessing);
                }
                return Expression.Lambda<Func<Array>>(exp).Compile()();
            }
            else if (exp is ListInitExpression)
            {
                return Expression.Lambda<Func<object>>(exp).Compile()();
            }
            else if (exp is LambdaExpression)
            {
                //Shouldn't pass in the lamda...but if someone does it, we shouldn't punish them.
                return parseWhereClauseRecursive(db, tableName, ((LambdaExpression)exp).Body, ref postProcessing);
            }

            return null;
        }

        private static string parseClauseToString(Expression exp, string tableName)
        {
            if (exp is BinaryExpression)
            {
                var be = (BinaryExpression)exp;
                var left = parseClauseToString(be.Left, tableName);
                var right = parseClauseToString(be.Right, tableName);

                return left + " " + mapNodeTypeToString(be.NodeType) + " " + right;
            }
            else if (exp is ConstantExpression)
            {
                var ce = (ConstantExpression)exp;
                return (ce.Value != null) ? ce.Value.ToString() : "null";
            }
            else if (exp is MemberExpression)
            {
                var me = ((MemberExpression)exp);
                if (me.Expression is ParameterExpression)
                {
                    if (me.Expression.Type == typeof(DataModel))
                        return tableName + "." + me.Member.Name;
                    else
                        return stringifyValue(GetObjectMemberValue(((ConstantExpression)me.Expression).Value, me.Member, null));
                }
                else if (me.Expression is ConstantExpression)
                {
                    return stringifyValue(GetObjectMemberValue(((ConstantExpression)me.Expression).Value, me.Member, null));
                }
                else if (me.Expression != null)
                {
                    object obj = Expression.Lambda<Func<Object>>(me.Expression).Compile()();
                    return stringifyValue(GetObjectMemberValue(obj, me.Member, null));
                }
                else
                {
                    object obj = Expression.Lambda<Func<Object>>(me).Compile().Invoke();
                    if (obj is string)
                        return (string)obj;
                    else if (obj is List<string>)
                        return ((List<string>)obj).ToDelimitedList();
                    else
                        return obj.ToString();

                }
            }
            else if (exp is MethodCallExpression)
            {
                var mce = ((MethodCallExpression)exp);
                List<string> args = new List<string>();

                if (mce.Object != null && mce.Object.Type == typeof(DynamicRecord) && typeof(DataModel) == typeof(BaseModel))
                    return string.Format("{0}.{1}", tableName, ((ConstantExpression)mce.Arguments[0]).Value.ToString());

                foreach (var arg in mce.Arguments)
                {
                    var resolvedArg = parseClauseToString(arg, tableName);
                    args.Add(resolvedArg);
                }

                //Handle DataFunction functions
                if (mce.Method.DeclaringType == typeof(DataFunction))
                {
                    if (mce.Object != null)
                    {
                        var objectExpression = parseClauseToString(mce.Object, tableName);
                        if (objectExpression.HasValue())
                            return string.Format("{0}.{1}({2})", objectExpression, mce.Method.Name, args.ToDelimitedList());
                    }

                    return string.Format("{0}({1})", mce.Method.Name, args.ToDelimitedList());
                }

                //Handle operations called on models (really, really should avoid this)
                if (mce.Object is MemberExpression)
                {
                    var member = ((MemberExpression)mce.Object).Expression;
                    if (member is ParameterExpression)
                    {
                        var parameter = ((ParameterExpression)member).Type;
                        if (parameter == typeof(DataModel))
                        {
                            //Handle .Equals Nicely. Everything else gets an exception!
                            if (mce.Method.Name.Equals("Equals"))
                                return parseClauseToString(mce.Object, tableName) + "==" + args[0];
                        }
                        else
                            throw new RepositoryException($"Cannot apply method: {mce.Method.Name} to DataModel in an Expression");
                    }
                }
                var eatPostProcessing = new List<Func<List<DataModel>, List<DataModel>>>();
                return stringifyValue(GetObjectMemberValue(parseWhereClauseRecursive(null, tableName, mce.Object, ref eatPostProcessing), mce.Method, args.ToArray()));
            }
            else if (exp is UnaryExpression)
            {
                if (exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked)
                    return parseClauseToString(((UnaryExpression)exp).Operand, tableName);
                else if (((UnaryExpression)exp).Operand != null)
                    return parseClauseToString(((UnaryExpression)exp).Operand, tableName);

                throw new InvalidOperationException("Invalid Expression: " + exp.ToString());
            }
            else if (exp is NewArrayExpression)
            {
                var nae = (NewArrayExpression)exp;
                foreach (var innerE in nae.Expressions)
                {
                    if (innerE is ListInitExpression)
                        return parseClauseToString(innerE, tableName);
                }
                return exp.ToString();
            }
            else if (exp is ListInitExpression)
            {
                return exp.ToString();
            }
            else if (exp is LambdaExpression)
            {
                //Shouldn't pass in the lamda...but if someone does it, we shouldn't punish them.
                return parseClauseToString(((LambdaExpression)exp).Body, tableName);
            }

            throw new InvalidOperationException("Invalid Expression: " + exp.ToString());
        }

        /// <summary>
        /// Helper to map an expression type to Simple.Data arithmetic operator
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private static Simple.Data.MathOperator mapArithmeticOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Add:
                    return Simple.Data.MathOperator.Add;
                case ExpressionType.Subtract:
                    return Simple.Data.MathOperator.Subtract;
                case ExpressionType.Divide:
                    return Simple.Data.MathOperator.Divide;
                case ExpressionType.Multiply:
                    return Simple.Data.MathOperator.Multiply;
                case ExpressionType.Modulo:
                    return Simple.Data.MathOperator.Modulo;
                default:
                    throw new InvalidOperationException("Invalid arithmetic operator specified. Operator not supported by Simple.Data");
            }
        }

        /// <summary>
        /// Determines if the expression type represents an is arithmetic expression.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private static bool isArithmeticExpression(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Divide:
                case ExpressionType.Multiply:
                case ExpressionType.Modulo:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Maps a LINQ Experssion type to a Simple.Data Expression Type
        /// </summary>
        /// <param name="type">The LINQ Expression type.</param>
        /// <returns></returns>
        private static Simple.Data.SimpleExpressionType mapExpressionType(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                    return Simple.Data.SimpleExpressionType.Equal;
                case ExpressionType.NotEqual:
                    return Simple.Data.SimpleExpressionType.NotEqual;
                case ExpressionType.AndAlso:
                    return Simple.Data.SimpleExpressionType.And;
                case ExpressionType.OrElse:
                    return Simple.Data.SimpleExpressionType.Or;
                case ExpressionType.LessThan:
                    return Simple.Data.SimpleExpressionType.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return Simple.Data.SimpleExpressionType.LessThanOrEqual;
                case ExpressionType.GreaterThan:
                    return Simple.Data.SimpleExpressionType.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return Simple.Data.SimpleExpressionType.GreaterThanOrEqual;

                default:
                    throw new InvalidOperationException("Invalid expression operation specified. Expression not supported by Simple.Data");
            }
        }

        /// <summary>
        /// Maps the node type to string.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private static string mapNodeTypeToString(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                    return "==";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.AndAlso:
                    return "&&";
                case ExpressionType.OrElse:
                    return "||";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.Power:
                    return "^";
                default:
                    return type.ToString();
            }
        }

        /// <summary>
        /// Helper function to get the value of a member of an object (field, property, Method)
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="memberArgs">The member arguments.</param>
        /// <returns></returns>
        private static object GetObjectMemberValue(object obj, MemberInfo memberInfo, object[] memberArgs)
        {
            if (memberInfo == null)
                throw new ArgumentNullException("memberInfo");

            // Get the value
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(obj);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(obj, memberArgs);
                case MemberTypes.Method:
                    return ((MethodInfo)memberInfo).Invoke(obj, memberArgs);
                default:
                    throw new Exception(String.Format("the type of the member {0}.{1} is not supported", obj.GetType().Name, memberInfo.Name));
            }
        }

        private static string stringifyValue(object val)
        {
            string result = string.Empty;
            if (val is IList)
            {
                List<string> stringList = new List<string>();
                foreach (var item in ((IList)val))
                    stringList.Add(item.ToString());
                return stringList.ToDelimitedList();
            }
            if (val is IEnumerable)
            {
                List<string> stringList = new List<string>();
                foreach (var item in ((IEnumerable)val))
                    stringList.Add(item.ToString());
                return stringList.ToDelimitedList();
            }
            if (val is string[])
            {
                return ((string[])val).ToList().ToDelimitedList();
            }

            return val.ToString();
        }

        #endregion
    }
}

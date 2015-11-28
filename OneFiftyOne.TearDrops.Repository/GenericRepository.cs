using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Simple.Data;
using System.Dynamic;
using OneFiftyOne.TearDrops.Common;

namespace OneFiftyOne.TearDrops.Repository
{
    /// <summary>
    ///     Data repository that provides generic CRUD functions for a DataModel
    /// </summary>
    /// <typeparam name="DataModel">The type of the Data model.</typeparam>
    public class GenericRepository<DataModel> : BaseRepository where DataModel : BaseModel
    {
        public static string _tableName = GetTableName();

        #region GetTableName

        /// <summary>
        ///     Gets the name of the table for the model that is the type parameter of this repository
        /// </summary>
        /// <returns></returns>
        public static string GetTableName()
        {
            var table = (Table)Attribute.GetCustomAttribute(typeof(DataModel), typeof(Table));
            if (table == null || string.IsNullOrEmpty(table.Name))
                throw new Exception("GenericRepository cannot use models without a PonTable attribute");

            return table.Name.ToLower();
        }

        /// <summary>
        ///     Gets the name of the table for the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static string GetTableName(DataModel model)
        {
            return GetTableName((BaseModel)model);
        }

        /// <summary>
        ///     Gets the name of the table for the specified model
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static string GetTableName(BaseModel model)
        {
            var table = (Table)Attribute.GetCustomAttribute(model.GetType(), typeof(Table));
            if (table == null || string.IsNullOrEmpty(table.Name))
                return model.GetTableName();

            return table.Name.ToLower();
        }

        #endregion

        #region CREATE

        /// <summary>
        /// Adds the specified model to the database
        /// model will be returned with values from identity columns if they are specified with [PrimaryKey] attribute
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Create(DataModel model, RepositoryTransaction transaction)
        {
            Create(model, false, transaction);
        }
        

        /// <summary>
        /// Adds the specified model to the database
        /// model will be returned with values from identity columns if they are specified with [PrimaryKey] attribute
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="returnIdentity">[OPTIONAL] if set to <c>true</c> any identity columns will be returned with new values in the model</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Create(DataModel model, bool returnIdentity = false, RepositoryTransaction transaction = null)
        {
            try
            {
                var db = OpenConnection(transaction);
                DataModel result = db[_tableName].Insert(model.CreateInsertableModel());
                FireRowUpdated(result, _tableName);

                if (result != null && returnIdentity)
                {
                    foreach (
                        PropertyInfo prop in
                            typeof(DataModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(
                                    prop => (PrimaryKey)Attribute.GetCustomAttribute(prop, typeof(PrimaryKey)) != null)
                        )
                    {
                        prop.SetValue(model, prop.GetValue(result, null), null);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::Create Error: {0}  -- Model: {1}", e.Message, model.ToString()), e);
            }
        }

        #endregion

        #region UPSERT
        /// <summary>
        /// Insert or Updates the specified row in the database. All data in the model will be stored.
        /// </summary>
        /// <param name="model">The model (Must have primary key fields filled out).</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Upsert(DataModel model, RepositoryTransaction transaction = null)
        {
            try
            {
                var db = OpenConnection(transaction);
                db[_tableName].Upsert(model.CreateInsertableModel());
                FireRowUpdated(model, _tableName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::Upsert Error: {0}  -- Model: {1}", e.Message, model.ToString()), e);
            }
        }
        #endregion

        #region UPDATE

        /// <summary>
        /// Updates the specified models.
        /// </summary>
        /// <param name="models">The models.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Update(List<DataModel> models, RepositoryTransaction transaction = null)
        {
            try
            {
                var db = OpenConnection(transaction);
                db[_tableName].Update(models);
                FireRowUpdated(null, _tableName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::Update Error: {0}  -- Model: {1}", e.Message, models.ToString()), e);
            }
        }


        /// <summary>
        ///     Updates the specified row in the database. All data in the model will be stored.
        /// </summary>
        /// <param name="model">The model (Must have primary key fields filled out).</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Update(DataModel model, RepositoryTransaction transaction = null)
        {
            try
            {
                var db = OpenConnection(transaction);
                db[_tableName].Update(model.CreateInsertableModel());
                FireRowUpdated(model, _tableName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::Update Error: {0}  -- Model: {1}", e.Message, model.ToString()), e);
            }
        }

        /// <summary>
        ///     Updates the specified row in the database with only the data int the model object (must contain the primary keys to
        ///     the table).
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Update(object model, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);
                db[_tableName].Update(model);
                FireRowUpdated(model, _tableName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::Update Error: {0}  -- Model: {1}", e.Message, model.ToString()), e);
            }
        }


        /// <summary>
        ///     Updates the rows matched by [whereClause], with the data contained in [modelData]
        ///     both parameters should be annonymus objects with properties named to match the column names.
        ///     only include data in those objects that you want updated.
        /// </summary>
        /// <param name="modelData">The model data.</param>
        /// <param name="whereCriteria">The where criteria.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void UpdateAll(object modelData, object whereCriteria, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);

                if (whereCriteria != null)
                {
                    SimpleExpression simpleClause = ExpressionEngine<DataModel>.BuildWhereClauseByObject(db,
                        whereCriteria);
                    db[_tableName].UpdateAll(simpleClause, modelData.CreateExpando());
                    FireRowUpdated(whereCriteria, _tableName);
                }
                else
                {
                    db[_tableName].UpdateAll(modelData);
                    FireRowUpdated(null, _tableName);
                }
            }
            catch (Exception e)
            {
                throw new Exception(
                    string.Format("GenericRepository::UpdateAll Error: {0} \nmodelData: {1} \nwhereCriteria: {2} ", e.Message, modelData,
                        whereCriteria), e);
            }
        }

        /// <summary>
        ///     Updates the rows matched by [whereClause], with the data contained in [model]
        ///     model should be an anonymous object with properties named to match the column names.
        ///     only include data in the object that you want updated.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="whereClause">The where clause.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void UpdateAll(object model, Expression<Func<DataModel, bool>> whereClause, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);

                if (whereClause != null)
                {
                    SimpleExpression simpleClause = ExpressionEngine<DataModel>.ParseWhereClause(db, whereClause);
                    db[_tableName].UpdateAll(simpleClause, model.CreateExpando());
                    FireRowUpdated(null, _tableName);
                }
                else
                {
                    db[_tableName].UpdateAll(model);
                    FireRowUpdated(null, _tableName);
                }
            }
            catch (Exception e)
            {
                throw new Exception(
                    string.Format("GenericRepository::UpdateAll Error:{0} model: {1} whereClause: {2} ", e.Message, model, whereClause), e);
            }
        }

        #endregion

        #region DELETE

          /// <summary>
        /// Deletes the row specified by the primary keys in the model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="cascadeDeleteSubModels">if set to <c>true</c> [cascade delete sub models].</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Delete(DataModel model, RepositoryTransaction transaction)
        {
            Delete(model, false, transaction);
        }

        /// <summary>
        /// Deletes the row specified by the primary keys in the model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="cascadeDeleteSubModels">if set to <c>true</c> [cascade delete sub models].</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Delete(DataModel model, bool cascadeDeleteSubModels = false, RepositoryTransaction transaction = null)
        {
            try
            {
                //Tablename gets funky becasue of the cascade possiblity. 
                string trueTableName = GetTableName(model);
                dynamic db = OpenConnection(transaction);
                if (cascadeDeleteSubModels)
                {
                    foreach (BaseModel subModel in GetAllSubModels(model))
                    {
                        GenericRepository<BaseModel>.Delete(subModel, true);
                    }
                }
                SimpleExpression whereClause = ExpressionEngine<DataModel>.BuildWhereClauseByPK(db, model);
                db[trueTableName].DeleteAll(whereClause);
                FireRowUpdated(model, trueTableName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::Delete Error: {0}  -- Model: {1}", e.Message, model.ToString()), e);
            }
        }

        /// <summary>
        ///     Deletes rows based on specified filter criteria.
        /// </summary>
        /// <param name="filterCriteria">The filter criteria.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Delete(object filterCriteria, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);

                if (filterCriteria != null)
                {
                    SimpleExpression whereClause = ExpressionEngine<DataModel>.BuildWhereClauseByObject(db,
                        filterCriteria);
                    db[_tableName].DeleteAll(whereClause);
                    FireRowUpdated(filterCriteria, _tableName);
                }
                else
                {
                    db[_tableName].DeleteAll();
                    FireRowUpdated(null, _tableName);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::Delete Error: {0}  -- filterCriteria: {1}", e.Message, filterCriteria.ToString()), e);
            }
        }

        /// <summary>
        ///     Deletes rows based on the LINQ Expression represented by [whereClause]
        /// </summary>
        /// <param name="whereClause">The where clause.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static void Delete(Expression<Func<DataModel, bool>> whereClause, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);

                if (whereClause != null)
                {
                    SimpleExpression simpleClause = ExpressionEngine<DataModel>.ParseWhereClause(db, whereClause);
                    db[_tableName].DeleteAll(simpleClause);
                    FireRowUpdated(null, _tableName);
                }
                else
                {
                    db[_tableName].DeleteAll();
                    FireRowUpdated(null, _tableName);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::Delete Error: {0}  -- whereClause: {1}", e.Message, whereClause.ToString()), e);
            }
        }

        #endregion

        #region GET

        /// <summary>
        /// Gets the specified row based on primary key data in [model].
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="skipChildren">[OPTIONAL] if set to <c>true</c> will ignore child models when returning data.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static DataModel Get(DataModel model, bool skipChildren = false, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);
                SimpleExpression whereClause = ExpressionEngine<DataModel>.BuildWhereClauseByPK(db, model);
                DataModel result;
                dynamic query = db[_tableName].FindAll(whereClause);

                List<Tuple<PropertyInfo, bool>> children = null;

                if (!skipChildren)
                    children = ExpressionEngine<DataModel>.QueryJoiner(db, ref query);

                result = query.SingleOrDefault();

                if (!skipChildren)
                    getGrandchildren(children, result, ActiveConnection, transaction);
                else if (result != null && skipChildren)
                    setListPropertiesToEmpty(new List<DataModel> { result });

                return result;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::GetEx Error: {0}  -- Model: {1}", e.Message, model.ToString()), e);
            }
        }

        /// <summary>
        ///     Gets all rows based on a supplied search criteria.
        ///     [searchCriteria] is an anonymous object with property names that match the column(s) to filter on.
        ///     ex: GetAll(new { ForeignKeyColumn = 1 } ); //would bring back all rows where ForeignKeyColumn = 1
        ///     if [searchCriteria] is null, all rows will be returned.
        /// </summary>
        /// <param name="searchCriteria">The search criteria.</param>
        /// <param name="skipChildren">[OPTIONAL] if set to <c>true</c> will ignore child models when returning data.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static List<DataModel> GetAll(object searchCriteria, bool skipChildren = false, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);
                List<DataModel> models;
                dynamic query;

                if (searchCriteria != null)
                {
                    SimpleExpression whereClause = ExpressionEngine<DataModel>.BuildWhereClauseByObject(db,
                        searchCriteria);
                    if (whereClause != null)
                        query = db[_tableName].FindAll(whereClause);
                    else
                        query = db[_tableName].All();
                }
                else
                    query = db[_tableName].All();

                List<Tuple<PropertyInfo, bool>> children = null;
                if (!skipChildren)
                    children = ExpressionEngine<DataModel>.QueryJoiner(db, ref query);

                models = query.ToList<DataModel>();

                //Sub model for each row returned here
                if (models != null && !skipChildren)
                {
                    Connection threadSafeConnection = new Connection(ActiveConnection);
                    System.Threading.Tasks.Parallel.ForEach(models, model =>
                        getGrandchildren(children, model, threadSafeConnection, transaction));
                }
                else if (models != null && skipChildren)
                    setListPropertiesToEmpty(models);

                return models ?? new List<DataModel>();
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::GetAll Error: {0}  -- searchCriteria: {1}", e.Message, searchCriteria.ToString()), e);
            }
        }

        /// <summary>
        ///     Gets all rows based on the LINQ Expression represented by [whereClause]
        /// </summary>
        /// <param name="whereClause">LINQ Expression representing the where clause</param>
        /// <param name="skipChildren">[OPTIONAL] if set to <c>true</c> will ignore child models when returning data.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static List<DataModel> GetAll(Expression<Func<DataModel, bool>> whereClause, bool skipChildren = false, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);
                List<DataModel> models;
                var postProcessingFunctions = new List<Func<List<DataModel>, List<DataModel>>>();

                dynamic query;

                if (whereClause != null)
                {
                    SimpleExpression simpleClause = ExpressionEngine<DataModel>.ParseWhereClause(db, whereClause, ref postProcessingFunctions);
                    query = db[_tableName].FindAll(simpleClause);
                }
                else
                    query = db[_tableName].All();

                List<Tuple<PropertyInfo, bool>> children = null;
                if (!skipChildren)
                    children = ExpressionEngine<DataModel>.QueryJoiner(db, ref query);

                models = query.ToList<DataModel>();
                
                //run each post processing function on the models.
                foreach (var func in postProcessingFunctions)
                    models = func(models);

                //Sub model for each row returned here
                if (models != null && !skipChildren)
                {
                    Connection threadSafeConnection = new Connection(ActiveConnection);
                    System.Threading.Tasks.Parallel.ForEach(models, model => getGrandchildren(children, model, threadSafeConnection, transaction));
                }
                else if (models != null && skipChildren)
                    setListPropertiesToEmpty(models);

                return models ?? new List<DataModel>();
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::GetAll Error: {0}  -- whereClause: {1}", e.Message, whereClause.ToString()), e);
            }
        }

        /// <summary>
        /// Gets all rows based on the LINQ Expression represented by [whereClause], with support for ordering by [orderColumn]
        /// </summary>
        /// <param name="whereClause">The where clause.</param>
        /// <param name="orderColumn">The order column.</param>
        /// <param name="descending">if set to <c>true</c> [descending].</param>
        /// <param name="skipChildren">if set to <c>true</c> [skip children].</param>
        /// <param name="transaction">The transaction.</param>
        /// <returns></returns>
        public static List<DataModel> GetAll(Expression<Func<DataModel, bool>> whereClause, Expression<Func<DataModel, object>> orderColumn, bool descending = false
            , bool skipChildren = false, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);
                List<DataModel> models;
                var postProcessingFunctions = new List<Func<List<DataModel>, List<DataModel>>>();
                dynamic query;
                Simple.Data.ObjectReference columnRef = null;

                if (orderColumn == null)
                {
                    //grab first primary key column, order by that.
                    foreach (var prop in typeof(DataModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if ((PrimaryKey)Attribute.GetCustomAttribute(prop, typeof(PrimaryKey)) != null)
                        {
                            columnRef = db[_tableName][prop.Name];
                            break;
                        }
                    }
                }
                else
                    columnRef = ExpressionEngine<DataModel>.GetColumnReference(db, orderColumn);
                if (whereClause != null)
                {
                    SimpleExpression simpleClause = ExpressionEngine<DataModel>.ParseWhereClause(db, whereClause, ref postProcessingFunctions);
                    query = (descending) ? db[_tableName].FindAll(simpleClause).OrderByDescending(columnRef) : db[_tableName].FindAll(simpleClause).OrderBy(columnRef);
                }
                else
                    query = (descending) ? db[_tableName].All().OrderByDescending(columnRef) : db[_tableName].All().OrderBy(columnRef);

                List<Tuple<PropertyInfo, bool>> children = null;
                if (!skipChildren)
                    children = ExpressionEngine<DataModel>.QueryJoiner(db, ref query);

                models = query.ToList<DataModel>();

                //run each post processing function on the models.
                foreach (var func in postProcessingFunctions)
                    models = func(models);

                //Sub model for each row returned here
                if (models != null && !skipChildren)
                {
                    Connection threadSafeConnection = new Connection(ActiveConnection);
                    System.Threading.Tasks.Parallel.ForEach(models, model => getGrandchildren(children, model, threadSafeConnection, transaction));
                }
                else if (models != null && skipChildren)
                    setListPropertiesToEmpty(models);

                return models ?? new List<DataModel>();
        }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::GetAll Error: {0}  -- whereClause: {1} orderBy: {2}", e.Message, whereClause.ToString(), orderColumn.ToString()), e);
            }
        }

        /// <summary>
        /// Gets all rows based on the LINQ Expression represented by [whereClause]
        /// </summary>
        /// <param name="whereClause">LINQ Expression representing the where clause</param>
        /// <param name="start">The start.</param>
        /// <param name="count">The count.</param>
        /// <param name="orderColumn">The order column.</param>
        /// <param name="descending">if set to <c>true</c> [descending].</param>
        /// <param name="skipChildren">[OPTIONAL] if set to <c>true</c> will ignore child models when returning data.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static PagedResults<DataModel> GetPaged(Expression<Func<DataModel, bool>> whereClause, int start, int count, Expression<Func<DataModel, object>> orderColumn
            , bool descending = false, bool skipChildren = false, RepositoryTransaction transaction = null)
        {
            try
            {
                Simple.Data.Future<int> rowCount;
                dynamic db = OpenConnection(transaction);
                List<DataModel> models;
                dynamic query;

                Simple.Data.ObjectReference columnRef = null;
                if (orderColumn == null)
                {
                    //grab first primary key column, order by that.
                    foreach (var prop in typeof(DataModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if ((PrimaryKey)Attribute.GetCustomAttribute(prop, typeof(PrimaryKey)) != null)
                        {
                            columnRef = db[_tableName][prop.Name];
                            break;
                        }
                    }
                }
                else
                    columnRef = ExpressionEngine<DataModel>.GetColumnReference(db, orderColumn);

                if (whereClause != null)
                {
                    Simple.Data.SimpleExpression simpleClause = ExpressionEngine<DataModel>.ParseWhereClause(db, whereClause);
                    query = (descending) ? db[_tableName].FindAll(simpleClause).OrderByDescending(columnRef).WithTotalCount(out rowCount).Skip(start).Take(count)
                        : db[_tableName].FindAll(simpleClause).OrderBy(columnRef).WithTotalCount(out rowCount).Skip(start).Take(count);
                }
                else
                    query = (descending) ? db[_tableName].All().OrderByDescending(columnRef).WithTotalCount(out rowCount).Skip(start).Take(count)
                       : db[_tableName].All().OrderBy(columnRef).WithTotalCount(out rowCount).Skip(start).Take(count);

                List<Tuple<PropertyInfo, bool>> children = null;
                if (!skipChildren)
                    children = ExpressionEngine<DataModel>.QueryJoiner(db, ref query);
                models = query.ToList<DataModel>();

                //Sub model for each row returned here
                if (models != null && !skipChildren)
                {
                    Connection threadSafeConnection = new Connection(ActiveConnection);
                    System.Threading.Tasks.Parallel.ForEach(models, model => getGrandchildren(children, model, threadSafeConnection, transaction));
                }
                else if (models != null && skipChildren)
                    setListPropertiesToEmpty(models);

                return new PagedResults<DataModel>(rowCount, models);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("GenericRepository::GetPaged Error: {0}  -- whereClause: {1}", e.Message, whereClause.ToString()), e);
            }
        }

        /// <summary>
        ///     Returns the first column (Top 1) for a model, filtered by the specified where clause. You can also specifiy a
        ///     column to order on, and decending/ascending(default) order
        /// </summary>
        /// <param name="whereClause">The where clause.</param>
        /// <param name="orderColumn">The order column.</param>
        /// <param name="descending">if set to <c>true</c> [descending].</param>
        /// <param name="skipChildren">[OPTIONAL] if set to <c>true</c> will ignore child models when returning data.</param>
        /// <param name="transaction">[OPTIONAL] The transaction.</param>
        public static DataModel First(Expression<Func<DataModel, bool>> whereClause,
            Expression<Func<DataModel, object>> orderColumn, bool descending = false, bool skipChildren = false, RepositoryTransaction transaction = null)
        {
            try
            {
                dynamic db = OpenConnection(transaction);
                DataModel result = null;
                ObjectReference columnRef = null;
                dynamic query;
                if (orderColumn == null)
                {
                    //grab first primary key column, order by that.
                    foreach (PropertyInfo prop in typeof(DataModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => Attribute.GetCustomAttribute(prop, typeof(PrimaryKey)) != null))
                    {
                        columnRef = db[_tableName][prop.Name];
                        break;
                    }
                }
                else
                    columnRef = ExpressionEngine<DataModel>.GetColumnReference(db, orderColumn);

                if (whereClause != null)
                {
                    SimpleExpression simpleClause = ExpressionEngine<DataModel>.ParseWhereClause(db, whereClause);
                    query = (descending)
                        ? db[_tableName].All().Where(simpleClause).OrderByDescending(columnRef)
                        : db[_tableName].All().Where(simpleClause).OrderBy(columnRef);
                }
                else
                    query = (descending)
                        ? db[_tableName].All().OrderByDescending(columnRef)
                        : db[_tableName].All().OrderBy(columnRef);

                List<Tuple<PropertyInfo, bool>> children = null;
                if (!skipChildren)
                    children = ExpressionEngine<DataModel>.QueryJoiner(db, ref query);

                //oracle sorting with "First" is broken in simple.data (ROWNUM applies before order by)
                if (ActiveConnection.Type == DatabaseType.Oracle)
                {
                    List<DataModel> list = query.ToList<DataModel>();
                    result = list.FirstOrDefault();
                }
                else
                    result = query.SingleOrDefault();

                if (!skipChildren)
                    getGrandchildren(children, result, ActiveConnection, transaction);
                else if (result != null && skipChildren)
                    setListPropertiesToEmpty(new List<DataModel> { result });

                return result;
            }
            catch (Exception e)
            {
                throw new RepositoryException($"GenericRepository::First Error: {e.Message} \nwhereClause: {whereClause.ToString()}\n orderColumn: {orderColumn.ToString()}", e);
            }
        }

        #endregion

        #region PARAMETERS

        /// <summary>
        ///     Helper to retrieve data for model properties with [Property] attribute.
        /// </summary>
        /// <param name="model">The model.</param>
        private static void getParameters(BaseModel model, Connection connection, RepositoryTransaction transaction = null)
        {
            //Type modelType = model.GetType();
            //foreach (PropertyInfo prop in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            //{
            //    var parameter = (Parameter)Attribute.GetCustomAttribute(prop, typeof(Parameter));
            //    if (parameter != null)
            //    {
            //        dynamic db = transaction != null ? transaction.Transaction : Database.OpenConnection(connection.ConnectionString, connection.Provider);
            //        dynamic data = db["PARAMTRS"].Find(db["PARAMTRS"].TABLE_NAME == GetTableName(model)
            //            && db["PARAMTRS"].FIELD_NAME == parameter.Field
            //            && db["PARAMTRS"].PARMVALUE == (string)modelType.GetProperty(parameter.Field).GetValue(model, null));

            //        if (data == null)
            //            continue;

            //        prop.SetValue(model,
            //            (parameter.Type == Parameter.DescriptionType.Short) ? data.SHORTDESC : data.LONGDESC, null);
            //    }
            //}
        }

        #endregion

        #region SubModel Magic

        protected static void getGrandchildren(List<Tuple<PropertyInfo, bool>> children, BaseModel model, Connection threadSafeConnection, RepositoryTransaction transaction = null)
        {
            if (model == null)
                return;
            getParameters(model, threadSafeConnection, transaction);
            if (children == null)
                return;

            //params and grandchildren models.
            foreach (var child in children)
            {
                if (child.Item2 == true)
                {
                    var subModelList = (IList)child.Item1.GetValue(model, null);
                    if (subModelList == null)
                    {
                        var emptyList = Activator.CreateInstance(child.Item1.PropertyType);
                        child.Item1.SetValue(model, emptyList, null);
                        continue;
                    }
                    foreach (var m in subModelList)
                    {
                        ((BaseModel)m).RecursionLevel = model.RecursionLevel + 1;
                        if (m != null)
                            retrieveChildProperties((BaseModel)m, threadSafeConnection, transaction);
                    }
                }
                else
                {
                    BaseModel childModel = (BaseModel)child.Item1.GetValue(model, null);
                    if (childModel != null)
                    {
                        childModel.RecursionLevel = model.RecursionLevel + 1;
                        retrieveChildProperties(childModel, threadSafeConnection, transaction);
                    }     
                }
            }
        }

        /// <summary>
        /// Retrieves the child properties for a model.
        /// A wrapper to existing functions so can be called in a Parallel ForEach
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="connectionName">Name of the connection.</param>
        /// <param name="transaction">The transaction.</param>
        private static void retrieveChildProperties(BaseModel model, Connection connection, RepositoryTransaction transaction = null)
        {
            getParameters(model, connection, transaction);
            buildSubModels(model, connection, transaction);
        }

        /// <summary>
        /// Gets all sub models of a model (1 level deep).
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="setKeys">if set to <c>true</c> [set keys].</param>
        /// <returns></returns>
        private static List<BaseModel> GetAllSubModels(BaseModel model, bool setKeys = false)
        {
            var subModels = new List<BaseModel>();
            Type modelType = model.GetType();
            foreach (PropertyInfo prop in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var foreignKeys = (ForeignKey[])Attribute.GetCustomAttributes(prop, typeof(ForeignKey));
                if (!foreignKeys.Any()) continue;

                object subModel = prop.GetValue(model, null);
                var bms = subModel as IList;
                if (bms != null)
                {
                    foreach (BaseModel bm in bms)
                    {
                        //sets the value of the foreign key on the submodel
                        if (setKeys)
                        {
                            foreach (ForeignKey fk in foreignKeys)
                            {
                                bm.GetType()
                                    .GetProperty(fk.Dest)
                                    .SetValue(bm, modelType.GetProperty(fk.Src).GetValue(model, null), null);
                            }
                        }
                        subModels.Add(bm);
                    }
                }
                else
                {
                    var item = subModel as BaseModel;
                    if (item == null) continue;

                    //sets the value of the foreign key on the submodel
                    if (setKeys)
                    {
                        foreach (ForeignKey fk in foreignKeys)
                        {
                            item.GetType()
                                .GetProperty(fk.Dest)
                                .SetValue(item, modelType.GetProperty(fk.Src).GetValue(model, null), null);
                        }
                    }
                    subModels.Add(item);
                }
            }

            return subModels;
        }

        private static void buildSubModels(BaseModel model, Connection connection, RepositoryTransaction transaction = null)
        {
            foreach (PropertyInfo prop in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var foreignKeys = (ForeignKey[])Attribute.GetCustomAttributes(prop, typeof(ForeignKey));
                if (!foreignKeys.Any()) continue;

                Type modelType;
                dynamic searchCriteria = new Dictionary<string, object>();

                object instance = Activator.CreateInstance(prop.PropertyType);

                if (instance is IList)
                    modelType = prop.PropertyType.GetGenericArguments().First();
                else if (instance is BaseModel)
                    modelType = instance.GetType();
                else
                    continue; //[ForeignKey] was applied to something that isnt supported, skip 

                //populate foreign key values on the model
                foreach (ForeignKey key in foreignKeys)
                    searchCriteria.Add(key.Dest, model.GetType().GetProperty(key.Src).GetValue(model, null));

                //If looking for a list
                var list1 = instance as IList;
                if (list1 != null)
                {
                    IList list = list1;
                    Type genType = typeof(GenericRepository<>).MakeGenericType(modelType);
                    MethodInfo method = genType.GetMethod("getSubModel", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo genMethod = method.MakeGenericMethod(modelType);
                    var models = genMethod.Invoke(null, new object[] { modelType, searchCriteria, connection, false, transaction });
                    if (models != null)
                    {
                        foreach (var m in (IList)models)
                        {
                            ((BaseModel)m).RecursionLevel = model.RecursionLevel + 1;
                            list.Add(m);
                        }
                    }
                }
                else
                {
                    Type genType = typeof(GenericRepository<>).MakeGenericType(modelType);
                    MethodInfo method = genType.GetMethod("getSubModel", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo genMethod = method.MakeGenericMethod(modelType);
                    var models = genMethod.Invoke(null, new object[] { modelType, searchCriteria, connection, true, transaction });
                    if (models != null)
                    {
                        var list = (IList)models;
                        if (list.Count == 1)
                        {
                            instance = list[0];
                            if (instance != null)
                                ((BaseModel)instance).RecursionLevel = model.RecursionLevel + 1;

                        }
                        else
                            instance = null;
                    }
                    else
                        instance = null;
                }

                prop.SetValue(model, instance, null);
            }
        }

        private static IList<T> getSubModel<T>(Type modelType, IDictionary<string, object> searchCriteria, Connection connection,
            bool single = false, RepositoryTransaction transaction = null) where T : BaseModel
        {
            var table = (Table)Attribute.GetCustomAttribute(modelType, typeof(Table));
            if (table == null || string.IsNullOrEmpty(table.Name))
                throw new Exception("GenericRepository cannot use models without a PonTable attribute");

            dynamic db = Simple.Data.Database.OpenConnection(connection.ConnectionString, connection.Provider);
            dynamic query;

            if (searchCriteria == null || searchCriteria.Count == 0)
            {
                query = db[table.Name].All();
            }
            else
            {
                SimpleExpression whereClause = null;
                foreach (var criteria in searchCriteria)
                {
                    if (whereClause == null)
                        whereClause = db[table.Name][criteria.Key] == criteria.Value;
                    else
                        whereClause &= db[table.Name][criteria.Key] == criteria.Value;
                }
                query = db[table.Name].FindAll(whereClause);
            }

            List<Tuple<PropertyInfo, bool>> children = ExpressionEngine<T>.QueryJoiner(db, ref query);
            List<T> models = (single) ? new List<T> { ((T)query.FirstOrDefault()) } : query.ToList<T>();

            //Sub model for each row returned here
            if (models != null)
            {
                 Connection threadSafeConnection = new Connection(ActiveConnection);
                System.Threading.Tasks.Parallel.ForEach(models, model => getGrandchildren(children, model, threadSafeConnection, transaction));
            }

            return models;
        }

        private static object convertDatabaseValues(PropertyInfo property, KeyValuePair<string, object> field)
        {
            object value;
            if (property == null)
                return field.Value;
            if (field.Value != null && property.PropertyType != field.Value.GetType())
            {
                if (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    Type convType = property.PropertyType.GetGenericArguments()[0];
                    value = field.Value.GetType() != convType ? Convert.ChangeType(field.Value, convType) : field.Value;
                }
                else
                {
                    value = Convert.ChangeType(field.Value, property.PropertyType);
                }
            }
            else
                value = field.Value;

            return value;
        }

        #endregion

        #region EVENTS

        /// <summary>
        ///     Fires the row updated event.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="tableName">Name of the table.</param>
        protected static void FireRowUpdated(object model, string tableName)
        {
            //var emptyModel = (BaseModel)Activator.CreateInstance(typeof(DataModel));
            //List<Func<object, string, bool>> handlers = getRowUpdatedEventHandlers(emptyModel);

            //foreach (var handler in handlers)
            //{
            //    if (handler(model, tableName))
            //        break;
            //}
        }

        #endregion

        protected static void setListPropertiesToEmpty(List<DataModel> models)
        {
            var listProps = typeof(DataModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => typeof(IList).IsAssignableFrom(p.PropertyType) && p.CanWrite);
            if (listProps.Count() > 0)
            {
                System.Threading.Tasks.Parallel.ForEach(models, model =>
                {
                    foreach (var c in listProps)
                    {
                        c.SetValue(model, Activator.CreateInstance(c.PropertyType), null);
                    }
                });
            }
        }

        //public static Expression<Func<DataModel, bool>> BuildExpressionFromQueryStringFilter(string queryStringFilter)
        //{
        //    return ExpressionEngine<DataModel>.BuildExpressionFromQueryStringFilter(queryStringFilter);
        //}

        //public static Expression<Func<DynamicRecord, bool>> BuildDynamicExpressionFromQueryStringFilter(string queryStringFilter, List<DataDictModel> dataDictModels)
        //{
        //    return ExpressionEngine<BaseModel>.BuildDynamicExpressionFromQueryStringFilter(queryStringFilter, dataDictModels);
        //}

        //public static Expression<Func<DataModel, object>> BuildOrderColumnExpressionFromQueryString(string queryStringOrderColumn)
        //{
        //    return ExpressionEngine<DataModel>.BuildOrderColumnExpressionFromQueryString(queryStringOrderColumn);
        //}

        //public static Expression<Func<DynamicRecord, object>> BuildDynamicOrderColumnExpressionFromQueryString(string queryStringOrderColumn)
        //{
        //    return ExpressionEngine<BaseModel>.BuildDynamicOrderColumnExpressionFromQueryString(queryStringOrderColumn);
        //}

        //public static Simple.Data.SimpleExpression BuildWhereClauseByExpando(Simple.Data.DataStrategy db, ExpandoObject criteria, string tableName)
        //{
        //    return ExpressionEngine<DataModel>.BuildWhereClauseByExpando(db, criteria, tableName, false);
        //}
    }
}

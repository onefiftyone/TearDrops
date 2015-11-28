using OneFiftyOne.TearDrops.Common;
using OneFiftyOne.TearDrops.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OneFiftyOne.TearDrops.Repository
{
    #region SUPPORT Items
    public enum DatabaseType
    {
        Unset,
        SqlServer,
        Oracle,
        MySQL
    }

    public sealed class Connection
    {
        internal Connection(string name, string connectionString, string provider, DatabaseType type = DatabaseType.Unset)
        {
            Name = name;
            ConnectionString = connectionString;
            Provider = provider;

            if (type == DatabaseType.Unset)
            {
                if (provider.Equals("System.Data.SqlClient", StringComparison.CurrentCultureIgnoreCase))
                    Type = DatabaseType.SqlServer;
                else if (provider.Equals("Oracle.DataAccess.Client", StringComparison.CurrentCultureIgnoreCase)
                    || provider.Equals("Oracle.ManagedDataAccess.Client", StringComparison.CurrentCultureIgnoreCase)
                    || provider.Equals("Devart.Data.Oracle", StringComparison.CurrentCultureIgnoreCase)
                    || provider.Equals("System.Data.OracleClient", StringComparison.CurrentCultureIgnoreCase))
                {
                    Type = DatabaseType.Oracle;
                }
                else if (provider.Equals("Devart.Data.MySql", StringComparison.CurrentCultureIgnoreCase))
                {
                    Type = DatabaseType.MySQL;
                }
                else
                    Type = DatabaseType.Unset;
            }
            else
                Type = type;
        }

        internal Connection(Connection src)
        {
            Name = src.Name;
            ConnectionString = src.ConnectionString;
            Provider = src.Provider;
            Type = src.Type;
        }

        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string Provider { get; set; }
        public DatabaseType Type { get; set; }
    }
    #endregion

    /// <summary>
    ///     Base class for data repositories
    /// </summary>
    public abstract class BaseRepository
    {
        private static Dictionary<string, List<Func<object, string, bool>>> rowUpdatedEvents = new Dictionary<string, List<Func<object, string, bool>>>();
        private static readonly string cacheClearEventKey = "_CacheEvent";
        private static object activeConnectionStorage = null;

        public static readonly string CONNECTION_STORAGE_KEY = "td_CONNECTION_STORAGE_KEY";

        /// <summary>
        /// Gets the active connection object from the connection store.
        /// </summary>
        /// <value>
        /// The active connection.
        /// </value>
        /// <exception cref="OneFiftyOne.TearDrops.Repository.RepositoryException">Cannot find indexer method for activeConnectionStorage. Object set for connection storage must have a method defined as 'public object this[string index]'</exception>
        public static Connection ActiveConnection
        {
            get
            {
                object conn = null;
                if (activeConnectionStorage != null)
                    if (!activeConnectionStorage.TryGetIndexer(CONNECTION_STORAGE_KEY, out conn))
                        throw new RepositoryException("Cannot find indexer method for activeConnectionStorage. Object set for connection storage must have a method defined as 'public object this[string index]'");

                return conn as Connection;
            }
            private set
            {
                if (activeConnectionStorage != null)
                    if (!activeConnectionStorage.TrySetIndexer(CONNECTION_STORAGE_KEY, value))
                        throw new RepositoryException("Cannot find set indexer method for activeConnectionStorage. Object set for connection storage must have a method defined as 'public object this[string index]'");
            }
        }

        protected static string _paramMarker
        {
            get
            {
                return (ActiveConnection.Type == DatabaseType.SqlServer ? "@" : ":");
            }
        }

        /// <summary>
        /// Gets the maximum parameter count for database commands.
        /// </summary>
        /// <value>
        /// The maximum parameter count.
        /// </value>
        public static int MaxParameterCount
        {
            get
            {
                return (ActiveConnection.Type == DatabaseType.SqlServer ? 2100 : 1000) - 2;
            }
        }

        /// <summary>
        /// Returns the configurable size to use for all batch operations. Referenced config setting is "BatchOperationSize".
        /// Default: 5000
        /// </summary>
        /// <value>
        /// The size of the batch operation.
        /// </value>
        public static int BatchOperationSize
        {
            get
            {
                var batchSizeString = Configuration.Settings?.TearDrops?.RepositoryDrop?.BatchOperationSize;
                int batchSize = 0;
                if (batchSizeString != null && Int32.TryParse(batchSizeString, out batchSize))
                    return batchSize;

                return 5000;
            }
        }

        /// <summary>
        /// Opens a Simple.Data connection.
        /// </summary>
        /// <returns></returns>
        internal static Simple.Data.DataStrategy OpenConnection(RepositoryTransaction transaction = null)
        {
            try
            {
                return transaction != null ? transaction.Transaction : Simple.Data.Database.OpenConnection(ActiveConnection.ConnectionString, ActiveConnection.Provider);
            }
            catch (Exception e)
            {
                throw new RepositoryException($"Unable to open connection: {ActiveConnection?.ConnectionString ?? string.Empty} provider: {ActiveConnection?.Provider ?? string.Empty}", e);
            }
        }

        #region CACHE STUFF -- FIX
        //protected static void FireRowUpdated(object model, string tableName)
        //{
        //    if (rowUpdatedEvents.ContainsKey(tableName))
        //        foreach (var func in rowUpdatedEvents[tableName])
        //            func(model, tableName);

        //    List<Func<object, string, bool>> value1;
        //    if (rowUpdatedEvents.TryGetValue(cacheClearEventKey, out value1))
        //        foreach (var func in value1)
        //            func(model, tableName);
        //}

        ///// <summary>
        /////     Fires the row updated event.
        ///// </summary>
        ///// <param name="model">The model.</param>
        ///// <param name="tableName">Name of the table.</param>
        ///// <summary>
        /////     Adds the row updated event.
        ///// </summary>
        ///// <param name="model">The model.</param>
        ///// <param name="fn">The function.</param>
        //public static void AddRowUpdatedEvent(BaseModel model, Func<object, string, bool> fn)
        //{
        //    string genericCacheKey = model.GetCacheKey(true);
        //    List<Func<object, string, bool>> value;
        //    if (!rowUpdatedEvents.TryGetValue(genericCacheKey, out value))
        //        rowUpdatedEvents.Add(genericCacheKey, new List<Func<object, string, bool>> {fn});
        //    else
        //        value.Add(fn);
        //}

        ///// <summary>
        ///// Adds the cache clear handler.
        ///// </summary>
        ///// <param name="fn">The function.</param>
        //public static void AddCacheClearHandler( Func<object, string, bool> fn)
        //{
        //    List<Func<object, string, bool>> value;
        //    if (!rowUpdatedEvents.TryGetValue(cacheClearEventKey, out value))
        //        rowUpdatedEvents.Add(cacheClearEventKey, new List<Func<object, string, bool>> { fn });
        //    else
        //        value.Add(fn);
        //}

        ///// <summary>
        ///// Removes the row updated event.
        ///// </summary>
        ///// <param name="model">The model.</param>
        ///// <param name="fn">The function.</param>
        //public static void RemoveRowUpdatedEvent(BaseModel model, Func<object, string, bool> fn)
        //{
        //    string genericCacheKey = model.GetCacheKey(true);
        //    List<Func<object, string, bool>> value;
        //    if (rowUpdatedEvents.TryGetValue(genericCacheKey, out value))
        //        value.Remove(fn);
        //}

        ///// <summary>
        /////     Gets the row updated event handlers.
        ///// </summary>
        ///// <param name="model">The model.</param>
        ///// <returns></returns>
        //protected static List<Func<object, string, bool>> getRowUpdatedEventHandlers(BaseModel model)
        //{
        //    var eventList = new List<Func<object, string, bool>>();
        //    var genericCacheKey = model.GetCacheKey(true);
        //    List<Func<object, string, bool>> value;
        //    if (rowUpdatedEvents.TryGetValue(genericCacheKey, out value))
        //        eventList =  value;

        //    //Always add cache clear event(s)
        //    List<Func<object, string, bool>> value1;
        //    if (rowUpdatedEvents.TryGetValue(cacheClearEventKey, out value1))
        //        eventList.AddRange(value1);

        //    return eventList;
        //}

        #endregion

        /// <summary>
        ///     Gets the name of the database in the current connection string.
        /// </summary>
        /// <returns></returns>
        public static string GetDatabaseName()
        {
            string connectionString = ActiveConnection.ConnectionString;
            return ActiveConnection.Type == DatabaseType.Oracle
                ? Regex.Match(connectionString, @"User ID=(.*?);", RegexOptions.IgnoreCase)
                    .Value.Replace("User ID=", "")
                    .TrimEnd(';')
                : Regex.Match(connectionString, @"Initial Catalog=(.*?);", RegexOptions.IgnoreCase)
                    .Value.Replace("Initial Catalog=", "")
                    .TrimEnd(';');
        }

        /// <summary>
        /// Changes the connection string used.
        /// </summary>
        /// <param name="connectionStore">The connection store.</param>
        /// <param name="name">The name.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="dbType">Type of the database.</param>
        public static void ChangeConnection(object connectionStore, string name, string connectionString, string providerName, DatabaseType dbType = DatabaseType.Unset)
        {
            var newConnection = new Connection(name, connectionString, providerName, dbType);
            if (activeConnectionStorage == null)
            {
                if (connectionStore == null)
                    activeConnectionStorage = new Dictionary<string, Connection>();
                else
                    activeConnectionStorage = connectionStore;
            }

            ActiveConnection = newConnection;

            //if (ConfigurationManager.ConnectionStrings[connectionString] == null)
            //{
            //    //allow the connection string section of web.config to be written to (Memory only, not saved to disk)
            //    typeof(ConfigurationElementCollection).GetField("bReadOnly",
            //        BindingFlags.Instance | BindingFlags.NonPublic)
            //        .SetValue(ConfigurationManager.ConnectionStrings, false);
            //    switch (type)
            //    {
            //        case CPonDatabaseVendorType.SqlServer:
            //            ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings
            //            {
            //                Name = connectionString,
            //                ConnectionString = connectionString,
            //                ProviderName = "System.Data.SqlClient"
            //            });
            //            break;
            //        case CPonDatabaseVendorType.Oracle:
            //            ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings
            //            {
            //                Name = connectionString,
            //                ConnectionString = connectionString,
            //                ProviderName = "Oracle.ManagedDataAccess.Client"
            //            });
            //            break;
            //        default:
            //            throw new Exception("Unsupported Database Type");
            //    }
            //}

            //if (!createButDontChange)
            //{
            //    DBType = type;
            //    _connectionName = connectionString;
            //}
        }

        ///// <summary>
        /////     Sets up the connection string based on the odbc connection info
        ///// </summary>
        ///// <param name="odbcInfo">The ODBC information.</param>
        ///// <param name="dbType">Type of the database.</param>
        ///// <param name="uid">The uid.</param>
        ///// <param name="password">The password.</param>
        //public static String ChangeConnection(ODBCDSN odbcInfo, CPonDatabaseVendorType dbType, string uid, string password, bool createButDontChange = false)
        //{
        //    string connString;
        //    switch (dbType)
        //    {
        //        case CPonDatabaseVendorType.SqlServer:
        //            if (odbcInfo.IsTrustedConnection())
        //           { 
        //                connString =
        //                    "Data Source={0};Initial Catalog={1};Integrated Security=yes;".FormatString(
        //                        odbcInfo.GetDSNServerName()
        //                   , odbcInfo.GetDatabase());
        //            }
        //            else
        //            {
        //                connString =
        //                    "Data Source={0};Initial Catalog={1};User ID={2};Password={3}".FormatString(
        //                        odbcInfo.GetDSNServerName()
        //                    , odbcInfo.GetDatabase()
        //                    , uid
        //                    , password);
        //            }
        //            break;
        //        case CPonDatabaseVendorType.Oracle:
        //            if (odbcInfo.IsTrustedConnection())
        //            {
        //                connString = "Data Source={0};Integrated Security=yes;Statement Cache Size=200".FormatString(odbcInfo.GetDSNServerName());
        //            }
        //            else
        //            {
        //                connString = "Data Source={0};User ID={1};Password={2};Statement Cache Size=200".FormatString(odbcInfo.GetDSNServerName()
        //                         , uid
        //                         , password);
        //            }
        //            break;
        //        default:
        //            throw new Exception("Unsupported Database Type");
        //    }

        //    ChangeConnection(connString, dbType, createButDontChange);
        //    return connString;
        //}

        /// <summary>
        ///     Converts a SQL type to a .NET type
        ///     Based on: http://msdn.microsoft.com/en-us/library/bb386947.aspx
        /// </summary>
        /// <param name="sqlType">Type of the SQL.</param>
        /// <returns></returns>
        public static Type GetTypeFromSQLType(string sqlType)
        {
            switch (sqlType.ToUpperInvariant())
            {
                case "TINYINT":
                case "BIT":
                    return typeof (bool);
                case "SMALLINT":
                    return typeof (Int16);
                case "INT":
                    return typeof (int);
                case "BIGINT":
                    return typeof (Int64);
                case "SMALLMONEY":
                case "MONEY":
                case "DECIMAL":
                case "NUMERIC":
                    return typeof (decimal);
                case "FLOAT":
                    return typeof (double);
                case "SMALLDATETIME":
                case "DATETIME":
                case "DATETIME2":
                case "DATE":
                case "TIME":
                    return typeof (DateTime);
                case "CHAR":
                case "NCHAR":
                case "VARCHAR":
                case "VARCHAR2":
                case "NVARCHAR":
                case "NVARCHAR2":
                case "TEXT":
                case "NTEXT":
                case "XML":
                    return typeof (string);
                case "UNIQUEIDENTIFIER":
                    return typeof (Guid);
                case "BINARY":
                case "VARBINARY":
                case "IMAGE":
                    return typeof (byte[]);
                default:
                    return typeof (object);
            }
        }


        ///// <summary>
        ///// Converts an Oracle object type to a CLR type (or pass through if CLR type)
        ///// </summary>
        ///// <param name="value">The value.</param>
        ///// <returns></returns>
        //public static object HandleOracleType(object value)
        //{
        //    try
        //    {
        //        if (value == null || !value.GetType().Name.ToLower().Contains("oracle"))
        //            return value;

        //        if (value is OracleDecimal)
        //            return (decimal)OracleDecimal.SetPrecision((OracleDecimal)value, 28);
        //        var nullProp = value.GetType().GetProperty("IsNull");
        //        if(nullProp!=null)
        //        {
        //            if ((bool)nullProp.GetValue(value, null) == true)
        //                return null;
        //        }
        //        var valProp = value.GetType().GetProperty("Value");
        //        if (valProp != null)
        //        {
        //            return valProp.GetValue(value, null);
        //        }
        //        else
        //            return value;
        //    }
        //    catch(Exception e)
        //    {
        //        return null;
        //    }
        //}

        #region TRANSACTION SUPPORT

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <returns></returns>
        public static RepositoryTransaction BeginTransaction()
        {
            try
            {
                var db = (Simple.Data.Database)OpenConnection();
                return new RepositoryTransaction(db.BeginTransaction());
            }
            catch(Exception e)
            {
                throw new RepositoryException("BaseRepository::BeginTransaction: Unable to begin transaction", e);
            }
        }

        /// <summary>
        /// Commits the transaction to the database, and closes the connection.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        public static void CommitTransaction(RepositoryTransaction transaction)
        {
            try
            {
                transaction.Transaction.Commit();
                transaction.Transaction.Dispose();
            }
            catch (Exception e)
            {
                throw new RepositoryException("BaseRepository::CommitTransaction: Unable to commit transaction", e);
            }
        }

        /// <summary>
        /// Rollbacks the transaction to the database and closes the connection.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        public static void RollbackTransaction(RepositoryTransaction transaction)
        {
            try
            {
                if (transaction != null && transaction.Transaction != null)
                {
                    transaction.Transaction.Rollback();
                    transaction.Transaction.Dispose();
                }
            }
            catch (Exception e)
            {
                throw new RepositoryException("BaseRepository::RollbackTransaction: Unable to rollback transaction", e);
            }
        }

        #endregion
    }
}

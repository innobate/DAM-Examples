using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DAM
{

    /// <summary>
    /// Thrown when a data access request fails.
    /// </summary>
    [Serializable]
    public class DataAccessException : Exception
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public DataAccessException() { }

        /// <summary>
        /// Creates a new instance initialized with the specified message string.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DataAccessException(string message) : base(message) { }

        /// <summary>
        /// Creates a new instance initialized with the specified message string and inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public DataAccessException(string message, Exception inner) : base(message, inner) { }

        internal DataAccessException(string message, Exception inner, DataAccessManager dam) : base(message, inner) {
            if (dam.Information != null)
            {
                message += Environment.NewLine + "Product Name: " + dam.Information.DataSourceProductName;
                message += Environment.NewLine + "Version: " + dam.Information.DataSourceProductVersion;
                message += Environment.NewLine + "Version Normalized: " + dam.Information.DataSourceProductVersionNormalized;
            }
        }

        /// <summary>
        /// Creates a new instance initialized with serialization data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected DataAccessException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Represents parameter information for a command into the DataAccessManager.
    /// </summary>
    public class DatabaseParameter
    {
        /// <summary>
        /// Gets or sets the SQL command parameter name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the expected type of the parameter value.
        /// </summary>
        public DbType Type { get; set; }

        /// <summary>
        /// Creates and initializes a new instance.
        /// </summary>
        /// <param name="name">The name of the database parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <remarks>
        /// The expected type is defaulted, which may create performance problems with implict typing in the database.
        /// </remarks>
        public DatabaseParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Creates and initializes a new instance.
        /// </summary>
        /// <param name="name">The name of the database parameter (eg. @MyParameter or :MyParameter)</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="type">The expected type of the parameter.</param>
        public DatabaseParameter(string name, object value, DbType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
    }

    /// <summary>
    /// Provides data provider agnostic queries and commands.
    /// </summary>
    public class DataAccessManager : IDisposable
    {
        #region IDisposable Implementation
        public void Dispose()
        {
            // Does nothing; included for future using statement support.
        }
        #endregion

        private DbProviderFactory ProviderFactory { get; set; }

        /// <summary>
        /// Gets or sets the external data store provider name (ex. System.Data.SqlClient).
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// Gets or sets the external data store connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets the information about source that the Data Access Manager is initialised to.
        /// </summary>
        public readonly SourceInformation Information;

        /// <summary>
        /// Creates and initializes a new instance.
        /// </summary>
        /// <param name="providerName">The data provider name (ex. System.Data.SqlClient).</param>
        /// <param name="connectionString">An appropriate connection string for the data provider.</param>
        public DataAccessManager(string providerName, string connectionString)
        {
            ProviderName = providerName;
            ConnectionString = connectionString;
            ProviderFactory = DbProviderFactories.GetFactory(ProviderName);
            Information = this.GetSourceInformation();
        }

        /// <summary>
        /// /// Creates and initializes a new instance.  This is the preferred method for .NET Core.
        /// </summary>
        /// <param name="provider">The data provider factory (ex. System.Data.SqlClientFactory).</param>
        /// <param name="connectionString">An appropriate connection string for the data provider.</param>
        public DataAccessManager(DbProviderFactory provider, string connectionString)
        {
            ConnectionString = connectionString;
            ProviderFactory = provider;
            ProviderName = provider.ToString();
            Information = this.GetSourceInformation();
        }

        #region Commands
        /// <summary>
        /// Selects a DataTable from the DbProvider.
        /// </summary>
        /// <param name="commandText">The select command text to execute.</param>
        /// <param name="args">Parameter definitions for the command.</param>
        /// <returns>A DataTable containing records selected from the DbProvider.</returns>
        public DataTable Select(string commandText, params DatabaseParameter[] args)
        {
            var result = new DataTable();

            try
            {
                using (var connection = GetConnection())
                {
                    using (var command = ProviderFactory.CreateCommand())
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = commandText;

                        foreach (var arg in args)
                        {
                            AddParameter(command, arg);
                        }

                        using (var adapter = ProviderFactory.CreateDataAdapter())
                        {
                            adapter.SelectCommand = command;
                            adapter.Fill(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "CommandText: " + commandText, ex, this);
            }

            return result;
        }

        private DataTable Select(string commandText, int page, int pageSize, params DatabaseParameter[] args)
        {
            var result = new DataTable();

            try
            {
                using (var connection = GetConnection())
                {
                    using (var command = ProviderFactory.CreateCommand())
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = commandText;

                        foreach (var arg in args)
                        {
                            AddParameter(command, arg);
                        }

                        using (var adapter = ProviderFactory.CreateDataAdapter())
                        {
                            adapter.SelectCommand = command;
                            adapter.Fill((page - 1) * pageSize, pageSize, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "CommandText: " + commandText, ex, this);
            }

            return result;
        }

        /// <summary>
        /// Executes a non-query command on the DbProvider.
        /// Call a stored procedure that doesn't return any result data
        /// </summary>
        /// <param name="commandText">The non-query command text to execute.</param>
        /// <param name="args">Parameter definitions for the command.</param>
        public void ExecuteCommand(string commandText, params DatabaseParameter[] args)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    using (var command = ProviderFactory.CreateCommand())
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = commandText;

                        foreach (var arg in args)
                        {
                            AddParameter(command, arg);
                        }

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "CommandText: " + commandText, ex, this);
            }
        }

        #endregion

        #region Validation
        /// <summary>
        /// Sanitizes an identifier for the provider by quoting it.
        /// </summary>
        /// <param name="identifier">The identifier to sanitize.</param>
        /// <returns>The sanitized identifier.</returns>
        public string QuotedIdentifier(string identifier)
        {
            using (var builder = ProviderFactory.CreateCommandBuilder())
            {
                return builder.QuoteIdentifier(identifier);
            }
        }

        /// <summary>
        /// Checks a table name to verify that it exists in the DbProvider.  
        /// Only works with databases that support SQL92 standard
        /// </summary>
        /// <param name="tableName">Name of the table to verify.</param>
        /// <param name="isQuoted">True if the table name is already quoted.</param>
        /// <returns>True if the table exists, false otherwise.</returns>
        public bool TableExists(string tableName, bool isQuoted)
        {
            using (var connection = GetConnection())
            {
                using (var tables = connection.GetSchema("Tables"))
                {
                    string newTable = isQuoted ? tableName : QuotedIdentifier(tableName);

                    foreach (DataRow row in tables.Rows)
                    {
                        string existingTable = QuotedIdentifier(row["TABLE_NAME"].ToString());

                        if (existingTable == newTable)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
        #endregion

        #region Miscellaneous Helpers
        /// <summary>
        /// Retrieves an open connection from the provider factory.
        /// </summary>
        /// <returns>An open DbConnection.</returns>
        private DbConnection GetConnection()
        {
            var connection = ProviderFactory.CreateConnection();

            connection.ConnectionString = ConnectionString;
            connection.Open();

            return connection;
        }

        /// <summary>
        /// Adds the provided database parameter to the provided command.
        /// </summary>
        /// <param name="command">The command the parameter will be added to.</param>
        /// <param name="parameter">The parameter settings.</param>
        private void AddParameter(DbCommand command, DatabaseParameter parameter)
        {
            var p = command.CreateParameter();

            p.ParameterName = parameter.Name;
            p.Value = parameter.Value != null ? parameter.Value : DBNull.Value;
            p.DbType = parameter.Type;

            command.Parameters.Add(p);
        }
        #endregion

        #region List from DataTable
        // function that creates a list of an object from the given data table
        private static List<T> CreateListFromTable<T>(DataTable tbl) where T : new()
        {
            // define return list
            List<T> lst = new List<T>();

            // go through each row
            foreach (DataRow r in tbl.Rows)
            {
                // add to the list
                lst.Add(CreateItemFromRow<T>(r));
            }

            // return the list
            return lst;
        }

        // function that creates an object from the given data row
        private static T CreateItemFromRow<T>(DataRow row) where T : new()
        {
            // create a new object
            T item = new T();

            // set the item
            SetItemFromRow(item, row);

            // return 
            return item;
        }

        private static void SetItemFromRow<T>(T item, DataRow row) where T : new()
        {
            // go through each column
            foreach (DataColumn c in row.Table.Columns)
            {
                // find the property for the column
                PropertyInfo p = item.GetType().GetProperty(c.ColumnName);

                // if exists, set the value
                if (p != null && row[c] != DBNull.Value)
                {
                    p.SetValue(item, row[c], null);
                }
            }
        }

        /// <summary>
        /// Selects a List of strongly typed values from the DbProvider.
        /// </summary>
        /// <param name="commandText">The select command text to execute.</param>
        /// <param name="args">Parameter definitions for the command.</param>
        /// <returns>A List containing records selected from the DbProvider.</returns>
        public List<T> Select<T>(string commandText, params DatabaseParameter[] args) where T : new()
        {
            return DataAccessManager.CreateListFromTable<T>(this.Select(commandText, args));
        }

        /// <summary>
        ///  Selects a paged List of strongly typed values from the DbProvider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandText">The select command text to execute.</param>
        /// <param name="page">The page index as an integer</param>
        /// <param name="pageSize">The number of items returned within a page.</param>
        /// <param name="args">Parameter definitions for the command.</param>
        /// <returns></returns>
        public List<T> Select<T>(string commandText, int page, int pageSize, params DatabaseParameter[] args) where T : new()
        {
            return DataAccessManager.CreateListFromTable<T>(this.Select(commandText, page, pageSize, args));
        }

        #endregion

        #region Schema Helpers
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Table> GetTables()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("Tables");

                    if (result.Columns.Contains("TABLE_CATALOG")) result.Columns["TABLE_CATALOG"].ColumnName = "TableCatalog";
                    if (result.Columns.Contains("TABLE_SCHEMA")) result.Columns["TABLE_SCHEMA"].ColumnName = "TableSchema";
                    if (result.Columns.Contains("TABLE_NAME")) result.Columns["TABLE_NAME"].ColumnName = "TableName";
                    if (result.Columns.Contains("TABLE_TYPE")) result.Columns["TABLE_TYPE"].ColumnName = "TableType";

                    //OLE DB Text Driver mapping corrections
                    if (result.Columns.Contains("TABLE_CAT")) result.Columns["TABLE_CAT"].ColumnName = "TableCatalog";
                    if (result.Columns.Contains("TABLE_SCHEM")) result.Columns["TABLE_SCHEM"].ColumnName = "TableSchema";

                }

                return CreateListFromTable<Table>(result);
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetTables", ex,this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal DataTable GetSchemaCollections()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema();
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetSchemaCollections", ex,this);
            }
        }

        internal DataTable GetViews()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("Views");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetViews", ex,this);
            }
        }

        internal DataTable GetProcedures()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("Procedures");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetProcedures", ex,this);
            }
        }

        /// <summary>
        /// Gets all the columns in the database that the Data Access Manager is connected to.
        /// </summary>
        /// <returns>
        /// A TableColumn typed list of all the columns in the connected database.
        /// </returns>
        public List<TableColumn> GetColumns()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("Columns");
                    if (result.Columns.Contains("TABLE_CATALOG")) result.Columns["TABLE_CATALOG"].ColumnName = "TableCatalog";
                    if (result.Columns.Contains("TABLE_SCHEMA")) result.Columns["TABLE_SCHEMA"].ColumnName = "TableSchema";
                    if (result.Columns.Contains("TABLE_NAME")) result.Columns["TABLE_NAME"].ColumnName = "TableName";
                    if (result.Columns.Contains("COLUMN_NAME")) result.Columns["COLUMN_NAME"].ColumnName = "ColumnName";
                    if (result.Columns.Contains("DATA_TYPE")) result.Columns["DATA_TYPE"].ColumnName = "SQLType";
                    if (result.Columns.Contains("IS_NULLABLE")) result.Columns["IS_NULLABLE"].ColumnName = "IsNullable";
                    result.Columns.Add("DataType", typeof(string));

                    //OLE DB Text Driver mapping corrections
                    if (result.Columns.Contains("TABLE_CAT")) result.Columns["TABLE_CAT"].ColumnName = "TableCatalog";
                    if (result.Columns.Contains("TABLE_SCHEM")) result.Columns["TABLE_SCHEM"].ColumnName = "TableSchema";
                    if (result.Columns.Contains("TYPE_NAME"))
                    {
                        if (result.Columns.Contains("SQLType")) result.Columns["SQLType"].ColumnName = "SQLTypeXXX";
                        result.Columns["TYPE_NAME"].ColumnName = "SQLType";
                    }
                }

                return CreateColumnListFromTable<TableColumn>(result, this.GetDataTypesInternal());
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetColumns", ex,this);
            }
        }

        // function that creates a list of TableColumn object from the given data table
        /// <summary>
        /// Creates a list of TableColumn objects from the given data table of the database the Data Access Manager is connected to.
        /// </summary>
        /// <typeparam name="TableColumn"></typeparam>
        /// <param name="tbl">DataTable to be converted to a list of TableColumn objects</param>
        /// <param name="dataTypes">A DataTable input with all the supported data types available within 
        /// the database source the Data Access Manager is connected to.
        /// </param>
        /// <returns>A typed list of TableColumn objects</returns>
        private static List<TableColumn> CreateColumnListFromTable<TableColumn>(DataTable tbl, DataTable dataTypes) where TableColumn : new()
        {
            // define return list
            List<TableColumn> lst = new List<TableColumn>();

            // go through each row
            foreach (DataRow r in tbl.Rows)
            {
                object[] findTheseVals = new object[22];
                var dataType = dataTypes.Rows.Find(findTheseVals[0] = r["SQLType"]);
                var isNullable = (string)r["IsNullable"];
                r["DataType"] = (string)dataType[5] != "System.String" && (isNullable == "YES") ? dataType[5] + "?" : dataType[5];
                // add to the list
                lst.Add(CreateItemFromRow<TableColumn>(r));
            }

            // return the list
            return lst;
        }


        private static List<TableColumn> CreateColumnListFromTable<TableColumn>(DataTable tbl, List<DataTypes> dataTypes) where TableColumn : new()
        {
            // define return list
            List<TableColumn> lst = new List<TableColumn>();

            // go through each row
            foreach (DataRow r in tbl.Rows)
            {
                var dataType = dataTypes.Where(x => x.TypeName == (string)r["SQLType"]).FirstOrDefault();
                var isNullable = (string)r["IsNullable"]; ;
                r["DataType"] = dataType.DataType != "System.String" && (isNullable == "YES") ? dataType.DataType + "?" : dataType.DataType;
                // add to the list
                lst.Add(CreateItemFromRow<TableColumn>(r));
            }

            // return the list
            return lst;
        }

        /// <summary>
        /// Gets all the columns of a table in the database that the Data Access Manager is connected to.
        /// </summary>
        /// <param name="tableName">A string value containing the Name of the table in the database
        /// that the columns belong to.</param>
        /// <returns>A typed list of TableColumn objects</returns>
        public List<TableColumn> GetColumns(string tableName)
        {
            try
            {
                var result = new DataTable();

                using (var connection = GetConnection())
                {
                    //Set restriction string array
                    string[] restrictions = new string[4];
                    restrictions[2] = tableName;
                    result = connection.GetSchema("Columns", restrictions);

                    if (result.Columns.Contains("TABLE_CATALOG")) result.Columns["TABLE_CATALOG"].ColumnName = "TableCatalog";
                    if (result.Columns.Contains("TABLE_SCHEMA")) result.Columns["TABLE_SCHEMA"].ColumnName = "TableSchema";
                    if (result.Columns.Contains("TABLE_NAME")) result.Columns["TABLE_NAME"].ColumnName = "TableName";
                    if (result.Columns.Contains("COLUMN_NAME")) result.Columns["COLUMN_NAME"].ColumnName = "ColumnName";
                    if (result.Columns.Contains("DATA_TYPE")) result.Columns["DATA_TYPE"].ColumnName = "SQLType";
                    if (result.Columns.Contains("IS_NULLABLE")) result.Columns["IS_NULLABLE"].ColumnName = "IsNullable";
                    result.Columns.Add("DataType", typeof(string));


                    //OLE DB Text Driver mapping corrections
                    if (result.Columns.Contains("TABLE_CAT")) result.Columns["TABLE_CAT"].ColumnName = "TableCatalog";
                    if (result.Columns.Contains("TABLE_SCHEM")) result.Columns["TABLE_SCHEM"].ColumnName = "TableSchema";
                    if (result.Columns.Contains("TYPE_NAME"))
                    {
                        if (result.Columns.Contains("SQLType")) result.Columns["SQLType"].ColumnName = "SQLTypeXXX";
                        result.Columns["TYPE_NAME"].ColumnName = "SQLType";
                    }
                }

                return CreateColumnListFromTable<TableColumn>(result, this.GetDataTypes());
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetColumns", ex,this);
            }
        }

        /// <summary>
        /// Gets information about all the data types that are supported by the database that the 
        /// Data Access Manager is currently connected to.
        /// </summary>
        /// <returns></returns>
        internal DataTable GetDataTypesInternal()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("DataTypes");

                    //set primary key so that can be searched using find on column.
                    var keys = new System.Data.DataColumn[1];
                    System.Data.DataColumn col;
                    col = result.Columns[0];
                    keys[0] = col;
                    result.PrimaryKey = keys;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetDataTypes", ex,this);
            }
        }

        /// <summary>
        /// Gets information about all the data types that are supported by the database that the 
        /// Data Access Manager is currently connected to.
        /// </summary>
        /// <returns>A List of DataType</returns>
        public List<DataTypes> GetDataTypes()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("DataTypes");
                }

                return CreateListFromTable<DataTypes>(result);
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetDataTypes", ex,this);
            }
        }

        /// <summary>
        /// Gets information about all of the schema collections supported by the .NET Framework 
        /// managed provider that is currently used by Date Access Manager to connect to the database.
        /// </summary>
        /// <returns>A List of MetaData</returns>
        public List<MetaData> GetMetaDataTypes()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema();
                }

                return CreateListFromTable<MetaData>(result);
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetMetaDataTypes", ex,this);
            }
        }


        //DataSourceInformation
        internal SourceInformation GetSourceInformation()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("DataSourceInformation");
                }

                return CreateItemFromRow<SourceInformation>(result.Rows[0]);
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetSourceInformation", ex);
            }
        }

        /// <summary>
        ///  Gets information about the words that are reserved by the database that the 
        /// Data Access Manager is currently connected to.
        /// </summary>
        /// <returns>
        /// A ReservedWord typed List containing words used by the database that the Data 
        /// Access Manager is currently connected to.
        /// </returns>
        public List<ReservedWord> GetReservedWords()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("ReservedWords");
                    result.Columns["ReservedWord"].ColumnName = "Word";
                }

                return CreateListFromTable<ReservedWord>(result);
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetReservedWords", ex,this);
            }
        }

        /// <summary>
        /// Gets information about the restrictions that are supported by the database that the 
        /// Data Access Manager is currently connected to.
        /// </summary>
        /// <returns></returns>
        public List<Restriction> GetRestrictions()
        {
            try
            {
                var result = new DataTable();
                using (var connection = GetConnection())
                {
                    result = connection.GetSchema("Restrictions");
                }

                return CreateListFromTable<Restriction>(result);
            }
            catch (Exception ex)
            {
                throw new DataAccessException("Provider: " + ProviderName + Environment.NewLine + "GetRestrictions", ex,this);
            }
        }
        #endregion
    }

    public class TableColumn
    {
        /// <summary>
        /// Catalog of the table.
        /// </summary>
        public string TableCatalog { get; set; }
        /// <summary>
        /// Schema that contains the table.
        /// </summary>
        public string TableSchema { get; set; }
        /// <summary>
        /// Table name.
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// Name of the column
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// The data provider-specific data type name.
        /// </summary>
        public string SQLType { get; set; }
        /// <summary>
        /// The name of the .NET Framework type of the data type.
        /// </summary>
        public string DataType { get; set; }
        /// <summary>
        /// true—The column can have null as a value.
        /// </summary>
        public string IsNullable { get; set; }
    }

    public class Table
    {
        /// <summary>
     /// Catalog of the table.
     /// </summary>
        public string TableCatalog { get; set; }
        /// <summary>
        /// Schema that contains the table.
        /// </summary>
        public string TableSchema { get; set; }
        /// <summary>
        /// Table name.
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// Type of table. Can be VIEW or BASE TABLE.
        /// </summary>
        public string TableType { get; set; }
    }

    /// <summary>
    /// Exposes information about the data types that are supported by the data provider that the Data Access Manager is using.
    /// </summary>
    public class DataTypes
    {
        /// <summary>
        /// The data provider-specific data type name.
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// The data source-specific type value that should be used when specifying a parameter's type. 
        /// For example, SqlDbType.Money or OracleType.Blob.
        /// </summary>
        public int ProviderDbType { get; set; }
        /// <summary>
        /// The length of a non-numeric column or parameter refers to either the maximum or the length defined for this type by the provider.
        /// 
        /// For character data, this is the maximum or defined length in units, defined by the data source.Oracle has the concept of specifying a length and then specifying the actual storage size for some character data types.This defines only the length in units for Oracle.
        /// 
        /// For date-time data types, this is the length of the string representation (assuming the maximum allowed precision of the fractional seconds component).
        /// 
        /// If the data type is numeric, this is the upper bound on the maximum precision of the data type.
        /// </summary>
        public long ColumnSize { get; set; }
        /// <summary>
        /// Format string that represents how to add this column to a data definition statement, such as CREATE TABLE. 
        /// Each element in the CreateParameter array should be represented by a "parameter marker" in the format string.
        /// </summary>
        public string CreateFormat { get; set; }
        /// <summary>
        /// The creation parameters that must be specified when creating a column of this data type. 
        /// Each creation parameter is listed in the string, separated by a comma in the order they are to be supplied.
        /// 
        /// For example, the SQL data type DECIMAL needs a precision and a scale.In this case, the creation parameters 
        /// should contain the string "precision, scale".
        /// 
        /// In a text command to create a DECIMAL column with a precision of 10 and a scale of 2, the value of the 
        /// CreateFormat column might be DECIMAL({ 0},{1})" and the complete type specification would be DECIMAL(10,2).
        /// </summary>
        public string CreateParameters { get; set; }
        /// <summary>
        /// The name of the .NET Framework type of the data type.
        /// </summary>
        public string DataType { get; set; }
        /// <summary>
        /// true—Values of this data type may be auto-incrementing.
        /// false—Values of this data type may not be auto-incrementing.
        /// 
        /// Note that this merely indicates whether a column of this data type may be auto-incrementing, not that all columns 
        /// of this type are auto-incrementing.
        /// </summary>
        public bool IsAutoincrementable { get; set; }
        /// <summary>
        /// true—The data type is the best match between all data types in the data store and the .NET Framework data type 
        /// indicated by the value in the DataType column.
        /// 
        /// false—The data type is not the best match.
        /// 
        /// For each set of rows in which the value of the DataType column is the same, the IsBestMatch column is set to true in only one row.
        /// </summary>
        public bool IsBestMatch { get; set; }
        /// <summary>
        /// 	true—The data type is a character type and is case-sensitive.
        /// 	
        /// false—The data type is not a character type or is not case-sensitive.
        /// </summary>
        public bool IsCaseSensitive { get; set; }
        /// <summary>
        /// true—Columns of this data type created by the data definition language (DDL) will be of fixed length.
        /// 
        /// false—Columns of this data type created by the DDL will be of variable length.
        /// 
        /// DBNull.Value—It is not known whether the provider will map this field with a fixed-length or variable - length column.
        /// </summary>
        public bool IsFixedLength { get; set; }
        /// <summary>
        /// true—The data type has a fixed precision and scale.
        /// 
        /// false—The data type does not have a fixed precision and scale.
        /// </summary>
        public bool IsFixedPrecisionScale { get; set; }
        /// <summary>
        /// true—The data type contains very long data; the definition of very long data is provider-specific.
        /// 
        /// false—The data type does not contain very long data.
        /// </summary>
        public bool IsLong { get; set; }
       /// <summary>
       /// true—The data type is nullable.
       /// 
       /// false—The data type is not nullable.
       /// 
       /// DBNull.Value—It is not known whether the data type is nullable.
       /// </summary>
        public bool IsNullable { get; set; }
        /// <summary>
        /// true—The data type can be used in a WHERE clause with any operator except the LIKE predicate.
        /// 
        /// false—The data type cannot be used in a WHERE clause with any operator except the LIKE predicate.
        /// </summary>
        public bool IsSearchable { get; set; }
        /// <summary>
        ///true—The data type can be used with the LIKE predicate
        ///
        /// false—The data type cannot be used with the LIKE predicate.
        /// </summary>
        public bool IsSearchableWithLike { get; set; }
        /// <summary>
        /// true—The data type is unsigned.
        /// 
        /// false—The data type is signed.
        /// 
        /// DBNull.Value—Not applicable to data type.
        /// </summary>
        public bool IsUnsigned { get; set; }
        /// <summary>
        /// If the type indicator is a numeric type, this is the maximum number of digits allowed to the right of the decimal point. 
        /// Otherwise, this is DBNull.Value.
        /// </summary>
        public short MaximumScale { get; set; }
        /// <summary>
        /// 	If the type indicator is a numeric type, this is the minimum number of digits allowed to the right of the decimal point. Otherwise, this is DBNull.Value.
        /// </summary>
        public short MinimumScale { get; set; }
        /// <summary>
        /// true – the data type is updated by the database every time the row is changed and the value of the column is different from all previous values
        /// false – the data type is note updated by the database every time the row is change.
        /// 
        /// DBNull.Value – the database does not support this type of data type
        /// </summary>
        public bool IsConcurrencyType { get; set; }
        /// <summary>
        /// true – the data type can be expressed as a literal
        /// 
        /// false – the data type can not be expressed as a literal
        /// </summary>
        public bool IsLiteralSupported { get; set; }
        /// <summary>
        /// The prefix applied to a given literal.
        /// </summary>
        public string LiteralPrefix { get; set; }
        /// <summary>
        /// The suffix applied to a given literal.
        /// </summary>
        public string LiteralSuffix { get; set; }
        /// <summary>
        /// NativeDataType is an OLE DB specific column for exposing the OLE DB type of the data type .
        /// </summary>
        public string NativeDataType { get; set; }
    }

    /// <summary>
    /// Exposes information about all of the schema collections supported by the .NET Framework managed provider 
    /// that is currently used by the Data Access Manager to connect to the database.
    /// </summary>
    public class MetaData
    {
        /// <summary>
        /// The name of the database schema collection
        /// </summary>
        public string CollectionName { get; set; }
        /// <summary>
        /// The number of restrictions that may be specified for the collection.
        /// </summary>
        public int NumberOfRestrictions { get; set; }
        /// <summary>
        /// 	The number of parts in the composite identifier/database object name. 
        /// 	For example, in SQL Server, this would be 3 for tables and 4 for columns. 
        /// 	In Oracle, it would be 2 for tables and 3 for columns
        /// </summary>
        public int NumberOfIdentifierParts { get; set; }
    }

    /// <summary>
    /// Exposes information about data source that the Data Access Manager is currently connect to.
    /// </summary>
    public class SourceInformation
    {
        /// <summary>
        /// The regular expression to match the composite separators in a composite identifier. For example, "\." (for SQL Server) or "@|\." (for Oracle).
        /// 
        /// A composite identifier is typically what is used for a database object name, for example: pubs.dbo.authors or pubs @dbo.authors.
        /// 
        /// For SQL Server, use the regular expression "\.". For OracleClient, use "@|\.".
        /// For ODBC use the Catalog_name_seperator.
        /// For OLE DB use DBLITERAL_CATALOG_SEPARATOR or DBLITERAL_SCHEMA_SEPARATOR.
        /// </summary>
        public string CompositeIdentifierSeparatorPattern { get; set; }
        /// <summary>
        /// he name of the product accessed by the provider, such as "Oracle" or "SQLServer".
        /// </summary>
        public string DataSourceProductName { get; set; }
        /// <summary>
        /// Indicates the version of the product accessed by the provider, in the data sources native format and not in Microsoft format.
        /// 
        /// In some cases DataSourceProductVersion and DataSourceProductVersionNormalized will be the same value.In the case of OLE DB 
        /// and ODBC, these will always be the same as they are mapped to the same function call in the underlying native API.
       /// </summary>
       public string DataSourceProductVersion { get; set; }
        /// <summary>
        /// A normalized version for the data source, such that it can be compared with String.Compare(). The format of this is consistent 
        /// for all versions of the provider to prevent version 10 from sorting between version 1 and version 2.
        /// 
        /// For example, the Oracle provider uses a format of "nn.nn.nn.nn.nn" for its normalized version, which causes an Oracle 8i data 
        /// source to return "08.01.07.04.01". SQL Server uses the typical Microsoft "nn.nn.nnnn" format.
        /// 
        /// In some cases, DataSourceProductVersion and DataSourceProductVersionNormalized will be the same value. In the case of OLE DB 
        /// and ODBC these will always be the same as they are mapped to the same function call in the underlying native API.
        /// </summary>
        public string DataSourceProductVersionNormalized { get; set; }
        /// <summary>
        /// Specifies the relationship between the columns in a GROUP BY clause and the non-aggregated columns in the select list.
        /// </summary>
        public GroupByBehavior GroupByBehavior { get; set; }
        /// <summary>
        /// A regular expression that matches an identifier and has a match value of the identifier. For example "[A-Za-z0-9_#$]".
        /// </summary>
        public string IdentifierPattern { get; set; }
        /// <summary>
        /// Indicates whether non-quoted identifiers are treated as case sensitive or not.
        /// </summary>
        IdentifierCase IdentifierCase { get; set; }
        /// <summary>
        ///Specifies whether columns in an ORDER BY clause must be in the select list. A value of true indicates that they are 
        ///required to be in the select list, a value of false indicates that they are not required to be in the select list.
        /// </summary>
        public bool OrderByColumnsInSelect { get; set; }
        /// <summary>
        /// A format string that represents how to format a parameter.
        /// 
        /// If named parameters are supported by the data source, the first placeholder in this string should be where the 
        /// parameter name should be formatted.
        /// 
        /// For example, if the data source expects parameters to be named and prefixed with an ':' this would be ":{0}". 
        /// When formatting this with a parameter name of "p1" the resulting string is ":p1".
        /// 
        /// If the data source expects parameters to be prefixed with the '@', but the names already include them, this 
        /// would be '{0}', and the result of formatting a parameter named "@p1" would simply be "@p1".
        /// 
        /// For data sources that do not expect named parameters and expect the use of the '?' character, the format 
        /// string can be specified as simply '?', which would ignore the parameter name. For OLE DB we return '?'.
        /// </summary>
        public string ParameterMarkerFormat { get; set; }
        /// <summary>
        /// A regular expression that matches a parameter marker. It will have a match value of the parameter name, if any.
        /// 
        /// For example, if named parameters are supported with an '@' lead-in character that will be included in the parameter 
        /// name, this would be: "(@[A-Za-z0-9_#]*)".   However, if named parameters are supported with a ':' as the lead-in 
        /// character and it is not part of the parameter name, this would be: ":([A-Za-z0-9_#]*)".
        /// 
        /// Of course, if the data source doesn't support named parameters, this would simply be "?".
        /// </summary>
        public string ParameterMarkerPattern { get; set; }
        /// <summary>
        /// The maximum length of a parameter name in characters. Visual Studio expects that if parameter names are supported, 
        /// the minimum value for the maximum length is 30 characters.
        /// 
        /// If the data source does not support named parameters, this property returns zero.
        /// </summary>
        public int ParameterNameMaxLength { get; set; }
        /// <summary>
        /// A regular expression that matches the valid parameter names. Different data sources have different rules regarding 
        /// the characters that may be used for parameter names.
        /// 
        /// Visual Studio expects that if parameter names are supported, the characters "\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Nd}" 
        /// are the minimum supported set of characters that are valid for parameter names.
        /// </summary>
        public string ParameterNamePattern { get; set; }
        /// <summary>
        /// A regular expression that matches a quoted identifier and has a match value of the identifier itself without the quotes. 
        /// For example, if the data source used double-quotes to identify quoted identifiers, this would be: "(([^\"]|\"\")*)".
        /// </summary>
        public string QuotedIdentifierPattern { get; set; }
        /// <summary>
        /// Indicates whether quoted identifiers are treated as case sensitive or not.
        /// </summary>
        public IdentifierCase QuotedIdentifierCase { get; set; }
        /// <summary>
        /// A regular expression that matches the statement separator.
        /// </summary>
        public string StatementSeparatorPattern { get; set; }
        /// <summary>
        /// A regular expression that matches a string literal and has a match value of the literal itself. For example, 
        /// if the data source used single-quotes to identify strings, this would be: "('([^']|'')*')"'
        /// </summary>
        public string StringLiteralPattern { get; set; }
        /// <summary>
        /// Specifies what types of SQL join statements are supported by the data source.
        /// </summary>
        public SupportedJoinOperators SupportedJoinOperators { get; set; }
    }


    /// <summary>
    /// Exposes information about the words that are reserved by the 
    /// database that the Data Access Manager is currently connected to.
    /// </summary>
    public class ReservedWord
    {
        /// <summary>
        /// Provider specific reserved word.
        /// </summary>
        public string Word { get; set; }
    }

    /// <summary>
    /// Exposes information about the restrictions that are supported by 
    /// the .NET Framework managed provider that is currently used by the Data Access Manager.
    /// </summary>
    public class Restriction
    {
        /// <summary>
        /// The name of the collection that these restrictions apply to.
        /// </summary>
        public string CollectionName { get; set; }
        /// <summary>
        /// The name of the restriction in the collection.
        /// </summary>
        public string RestrictionName { get; set; }
        /// <summary>
        /// Ignored
        /// </summary>
        public string RestrictionDefault { get; set; }
        /// <summary>
        /// The actual location in the collections restrictions that this particular restriction falls in
        /// </summary>
        public int RestrictionNumber { get; set; }
    }

}


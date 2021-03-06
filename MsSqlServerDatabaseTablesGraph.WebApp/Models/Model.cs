﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;

using Newtonsoft.Json;

namespace MsSqlServerDatabaseTablesGraph.WebApp.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class DALInputParams
    {
        public DALInputParams()
        {
        }
        public DALInputParams( string connectionString )
        {
            var builder = new SqlConnectionStringBuilder( connectionString );
            ServerName = builder.DataSource;            
            UserName   = builder.UserID;
            Password   = builder.Password;
            ConnectTimeout = builder.ConnectTimeout;
        }

        public static void ThrowIfWrong( DALInputParams inputParams, bool checkDatabaseName = true )
        {
            if ( inputParams == null ) throw (new ArgumentNullException( "inputParams" ));

            if ( string.IsNullOrWhiteSpace( inputParams.ServerName ) ) throw (new ArgumentNullException( "inputParams.ServerName" ));            
            if ( string.IsNullOrWhiteSpace( inputParams.UserName   ) ) throw (new ArgumentNullException( "inputParams.UserName" ));
        }

        public string ServerName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int?   ConnectTimeout { get; set; }

        public virtual string ConnectionString
        {
            get
            {
                var builder = new SqlConnectionStringBuilder()
                {
                    DataSource = ServerName,
                    UserID     = UserName,
                    Password   = Password ?? string.Empty,
                };
                if ( ConnectTimeout.HasValue )
                {
                    builder.ConnectTimeout = ConnectTimeout.Value;
                }
                return (builder.ConnectionString);
            }
        }
#if DEBUG
        public override string ToString()
        {
            return (ConnectionString);
        }
#endif
    }
    /// <summary>
    /// 
    /// </summary>
    public class DALGetTablesInputParams : DALInputParams
    {
        public DALGetTablesInputParams()
        {
        }
        public DALGetTablesInputParams( string connectionString )
        {
            var builder = new SqlConnectionStringBuilder( connectionString );
            ServerName   = builder.DataSource;
            DatabaseName = builder.InitialCatalog;
            UserName     = builder.UserID;
            Password     = builder.Password;
        }

        public static void ThrowIfWrong( DALGetTablesInputParams inputParams )
        {
            if ( inputParams == null ) throw (new ArgumentNullException( "inputParams" ));

            if ( string.IsNullOrWhiteSpace( inputParams.ServerName   ) ) throw (new ArgumentNullException( "inputParams.ServerName" ));            
            if ( string.IsNullOrWhiteSpace( inputParams.UserName     ) ) throw (new ArgumentNullException( "inputParams.UserName" ));
            if ( string.IsNullOrWhiteSpace( inputParams.DatabaseName ) ) throw (new ArgumentNullException( "inputParams.DatabaseName" ));
        }

        public string DatabaseName { get; set; }

        public override string ConnectionString
        {
            get
            {
                var builder = new SqlConnectionStringBuilder()
                {
                    DataSource     = ServerName,
                    InitialCatalog = DatabaseName ?? string.Empty,
                    UserID         = UserName,
                    Password       = Password ?? string.Empty,
                };
                return (builder.ConnectionString);
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class DALGetRefsInputParams : DALGetTablesInputParams
    {
        public DALGetRefsInputParams()
        {
        }
        public DALGetRefsInputParams( string connectionString ) : base( connectionString )
        {
        }

        [JsonProperty("RootTableNames")] 
        public string RootTableNames { get; set; }
        [JsonIgnore] 
        private ISet< string > _RootTableNamesSet;
        [JsonIgnore] 
        public ISet< string > RootTableNamesSet 
        {
            get 
            {
                if ( _RootTableNamesSet == null )
                {
                    _RootTableNamesSet = new SortedSet< string >();
                    if ( RootTableNames != null )
                    {
                        var tables = RootTableNames.Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
                        foreach ( var table in tables )
                        {
                            if ( !_RootTableNamesSet.Contains( table ) )
                            {
                                _RootTableNamesSet.Add( table );
                            }
                        }
                    }
                }
                return (_RootTableNamesSet);
            }
        }

        [JsonProperty] public int GraphWidth  { get; set; }
        [JsonProperty] public int GraphHeight { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class DALError
    {
        /// <summary>
        /// 
        /// </summary>
        public enum DALErrorTypeEnum
        {
            Common,

            ServerNotFoundOrNotAccessible,
            LoginFailed,
            CannotOpenDatabase,
        }

        protected DALError()
        {
        }
        protected DALError( Exception ex )
        {
            var sqlEx = ex as SqlException;
            if ( sqlEx == null )
            {
                Error     = ex.ToString();
                ErrorType = DALErrorTypeEnum.Common;
            }
            else
            {
                Error     = sqlEx.Message;
                ErrorType = DALErrorTypeEnum.Common;

                #region
                /*
		        ex.Number	2	int
		        ex.Class	20	byte
		        ex.Message	"A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: Named Pipes Provider, error: 40 - Could not open a connection to SQL Server)"	string

		        ex.Number	53	int
		        ex.Class	20	byte
		        ex.Message	"A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: Named Pipes Provider, error: 40 - Could not open a connection to SQL Server)"	string


		        ex.Number	18456	int
		        ex.Class	14	byte
		        ex.Message	"Login failed for user 'sa'."	string


		        ex.Number	4060	int
		        ex.Class	11	byte
		        ex.Message	"Cannot open database \"amchealth--\" requested by the login. The login failed.\r\nLogin failed for user 'sa'."	string
                */
                #endregion

                if ( (sqlEx.Class == 20) && ((sqlEx.Number == 2) || (sqlEx.Number == 53)) )
                {
                    ErrorType = DALErrorTypeEnum.ServerNotFoundOrNotAccessible;
                }
                else if ( sqlEx.Class == 14 && sqlEx.Number == 18456 )
                {
                    ErrorType = DALErrorTypeEnum.LoginFailed;
                }
                else if ( sqlEx.Class == 11 && sqlEx.Number == 4060 )
                {
                    ErrorType = DALErrorTypeEnum.CannotOpenDatabase;
                }                    
            }
        }
        protected DALError( string errorMessage )
        {
            Error     = errorMessage;
            ErrorType = DALErrorTypeEnum.Common;
        }

        [JsonProperty("error")]     public string Error { get; private set; }
        [JsonIgnore]                public DALErrorTypeEnum? ErrorType { get; private set; }
        [JsonProperty("errorType")] public string ErrorTypeAsString 
        {
            get { return (ErrorType.HasValue ? ErrorType.Value.ToString() : null); }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Table : IComparer< Table >
    {
        public Table( string name ) : this()
        {
            if ( string.IsNullOrWhiteSpace( name ) ) throw (new ArgumentNullException( "name" ));

            Name = name;
        }

        [JsonProperty("name")]
        public string Name { get; private set; }
#if DEBUG
        public override string ToString()
        {
            return ( Name );
        }
#endif
        public int Compare( Table x, Table y )
        {
            return ( string.Compare( x.Name, y.Name, true ));
        }
    }
    /// <summary>
    /// 
    /// </summary>    
    public sealed class TableCollection : DALError
    {
        public TableCollection( ISet< Table > tables )
        {
            Tables = tables;
        }
        public TableCollection( Exception ex ) : base( ex )
        {
        }

        [JsonProperty("tables")]
        public ISet< Table > Tables { get; private set; }

        public bool Contains( string tableName )
        {
            return (Tables.Contains( new Table( tableName ) ));
        }
    }
    /// <summary>
    /// 
    /// </summary>    
    public sealed class DatabaseCollection : DALError
    {
        public DatabaseCollection( ISet< string > databases )
        {
            Databases = databases;
        }
        public DatabaseCollection( Exception ex ) : base( ex )
        {
        }

        [JsonProperty( "databases" )]
        public ISet< string > Databases { get; private set; }
    }    

    /// <summary>
    /// 
    /// </summary>
    public struct RefItem
    {
        public int    Level            { get; internal set; }
        public bool   IsSelfRefs       { get; internal set; }
        public string FKName           { get; internal set; }
        public string TableName        { get; internal set; }
        public string Column           { get; internal set; }
        public string ForeignTableName { get; internal set; }
        public string ForeignColumn    { get; internal set; }

        public static RefItem CreateForSingleTable( string tableName )
        {
            var it = new RefItem()
            {
                Level            = 1,
                TableName        = tableName,
                ForeignTableName = tableName,
            };
            return (it);
        }
    }
}
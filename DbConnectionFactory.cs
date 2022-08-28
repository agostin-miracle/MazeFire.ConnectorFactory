using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

// https://blog.bitscry.com/2020/06/01/creating-a-dbconnectionfactory/

namespace MazeFire.ConnectorFactory
{

    public interface IDbConnectionFactory
    {
        IDbConnection CreateSqlConnection();

        IDbConnection CreateSqLiteConnection();

        IDbConnection CreateMySqlConnection();

        IDbConnection CreatePostgreSqlConnection();

        IDbConnection CreateOleDbConnection();

#if NETFULL
        IDbConnection CreateSqlServerCompactConnection(string connectionName);
#endif
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        //private readonly IDictionary<string, string> _connectionDictionary;
        private string connectionString = "";
        private MazeFire.Enums.DataAccessProviderTypes ConnectionType;

        public DbConnectionFactory(MazeFire.Enums.DataAccessProviderTypes type, string cs)
        {
            ConnectionType = type;
            connectionString = cs;
        }


        private bool HasConnectionString()
        {
            if (connectionString == null)
            {
                throw new Exception(string.Format("String de Conexão não encontrada"));
            }

            return true;
        }

        public IDbConnection CreateSqlConnection()
        {
            return CreateDbConnection();
        }

        public IDbConnection CreateSqLiteConnection()
        {
            return CreateDbConnection();
        }

        public IDbConnection CreateMySqlConnection()
        {
            return CreateDbConnection();
        }

        public IDbConnection CreatePostgreSqlConnection()
        {
            return CreateDbConnection();
        }

        public IDbConnection CreateOleDbConnection()
        {
            return CreateDbConnection();
        }


#if NETFULL

        public IDbConnection CreateSqlServerCompactConnection(string connectionName)
        {
            return CreateDbConnection(GetConnectionString(connectionName), DataAccessProviderTypes.SqlServerCompact);
        }
#endif

        private IDbConnection CreateDbConnection()
        {
            DbConnection connection = null;
            if (HasConnectionString())
            {
                DbProviderFactory factory = DbProviderFactoryUtils.GetDbProviderFactory(ConnectionType);

                connection = factory.CreateConnection();
                connection.ConnectionString = connectionString;
            }
            // Return the connection.
            return connection;
        }
    }

    internal static class DbProviderFactoryUtils
    {
        public static DbProviderFactory GetDbProviderFactory(MazeFire.Enums.DataAccessProviderTypes type)
        {
            if (type == MazeFire.Enums.DataAccessProviderTypes.SqlServer)
                return SqlClientFactory.Instance;

            if (type == MazeFire.Enums.DataAccessProviderTypes.SqLite)
            {
#if NETFULL
        return GetDbProviderFactory("System.Data.SQLite.SQLiteFactory", "System.Data.SQLite");
#else
                return GetDbProviderFactory("Microsoft.Data.Sqlite.SqliteFactory", "Microsoft.Data.Sqlite");
#endif
            }
            if (type == MazeFire.Enums.DataAccessProviderTypes.MySql)
                return GetDbProviderFactory("MySql.Data.MySqlClient.MySqlClientFactory", "MySql.Data");
            if (type == MazeFire.Enums.DataAccessProviderTypes.PostgreSql)
                return GetDbProviderFactory("Npgsql.NpgsqlFactory", "Npgsql");
#if NETFULL
    if (type == DataAccessProviderTypes.OleDb)
        return System.Data.OleDb.OleDbFactory.Instance;
    if (type == DataAccessProviderTypes.SqlServerCompact)
        return DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0");
#endif

            throw new NotSupportedException(string.Format("Provedor não compatível", type.ToString()));
        }

        public static DbProviderFactory GetDbProviderFactory(string providerName)
        {
#if NETFULL
    return DbProviderFactories.GetFactory(providerName);
#else
            var providername = providerName.ToLower();

            if (providerName == "system.data.sqlclient")
                return GetDbProviderFactory(MazeFire.Enums.DataAccessProviderTypes.SqlServer);
            if (providerName == "system.data.sqlite" || providerName == "microsoft.data.sqlite")
                return GetDbProviderFactory(MazeFire.Enums.DataAccessProviderTypes.SqLite);
            if (providerName == "mysql.data.mysqlclient" || providername == "mysql.data")
                return GetDbProviderFactory(MazeFire.Enums.DataAccessProviderTypes.MySql);
            if (providerName == "npgsql")
                return GetDbProviderFactory(MazeFire.Enums.DataAccessProviderTypes.PostgreSql);

            throw new NotSupportedException(string.Format("Provedor não compatível", providerName));
#endif
        }

        public static DbProviderFactory GetDbProviderFactory(string dbProviderFactoryTypename, string assemblyName)
        {
            var instance = GetStaticProperty(dbProviderFactoryTypename, "Instance");
            if (instance == null)
            {
                var a = LoadAssembly(assemblyName);
                if (a != null)
                    instance = GetStaticProperty(dbProviderFactoryTypename, "Instance");
            }

            if (instance == null)
                throw new InvalidOperationException(string.Format("Unable to retrieve DbProvider Factory Form", dbProviderFactoryTypename));

            return instance as DbProviderFactory;
        }

        #region Reflection Utilities
        //https://github.com/RickStrahl/Westwind.Utilities/blob/master/Westwind.Utilities/Utilities/ReflectionUtils.cs

        /// <summary>
        /// Retrieves a value from  a static property by specifying a type full name and property
        /// </summary>
        /// <param name="typeName">Full type name (namespace.class)</param>
        /// <param name="property">Property to get value from</param>
        /// <returns></returns>
        public static object GetStaticProperty(string typeName, string property)
        {
            Type type = GetTypeFromName(typeName);
            if (type == null)
                return null;

            return GetStaticProperty(type, property);
        }

        /// <summary>
        /// Returns a static property from a given type
        /// </summary>
        /// <param name="type">Type instance for the static property</param>
        /// <param name="property">Property name as a string</param>
        /// <returns></returns>
        public static object GetStaticProperty(Type type, string property)
        {
            object result = null;
            try
            {
                result = type.InvokeMember(property, BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty, null, type, null);
            }
            catch
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Helper routine that looks up a type name and tries to retrieve the
        /// full type reference using GetType() and if not found looking 
        /// in the actively executing assemblies and optionally loading
        /// the specified assembly name.
        /// </summary>
        /// <param name="typeName">type to load</param>
        /// <param name="assemblyName">
        /// Optional assembly name to load from if type cannot be loaded initially. 
        /// Use for lazy loading of assemblies without taking a type dependency.
        /// </param>
        /// <returns>null</returns>
        public static Type GetTypeFromName(string typeName, string assemblyName)
        {
            var type = Type.GetType(typeName, false);
            if (type != null)
                return type;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            // try to find manually
            foreach (Assembly asm in assemblies)
            {
                type = asm.GetType(typeName, false);

                if (type != null)
                    break;
            }
            if (type != null)
                return type;

            // see if we can load the assembly
            if (!string.IsNullOrEmpty(assemblyName))
            {
                var a = LoadAssembly(assemblyName);
                if (a != null)
                {
                    type = Type.GetType(typeName, false);
                    if (type != null)
                        return type;
                }
            }

            return null;
        }

        /// <summary>
        /// Overload for backwards compatibility which only tries to load
        /// assemblies that are already loaded in memory.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>        
        public static Type GetTypeFromName(string typeName)
        {
            return GetTypeFromName(typeName, null);
        }

        /// <summary>
        /// Try to load an assembly into the application's app domain.
        /// Loads by name first then checks for filename
        /// </summary>
        /// <param name="assemblyName">Assembly name or full path</param>
        /// <returns>null on failure</returns>
        public static Assembly LoadAssembly(string assemblyName)
        {
            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch { }

            if (assembly != null)
                return assembly;

            if (File.Exists(assemblyName))
            {
                assembly = Assembly.LoadFrom(assemblyName);
                if (assembly != null)
                    return assembly;
            }
            return null;
        }
        #endregion Reflection Utilities
    }
}
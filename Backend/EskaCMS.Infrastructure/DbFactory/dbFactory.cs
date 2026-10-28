using EskaCMS.Infrastructure.Enums;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using MySqlConnector;

namespace EskaCMS.Infrastructure.DbFactory
{
    public class dbFactory
    {
        public static DbConnection CreateConnection(string connectionString, EProvider Provider)
        {
            return Provider switch
            {
                EProvider.MySql => new MySqlConnection(connectionString),
                EProvider.PostgreSQL => new Npgsql.NpgsqlConnection(connectionString),
                EProvider.SqlServer => new SqlConnection(connectionString),
                EProvider.Oracle => new OracleConnection(connectionString),
                _ => throw new Exception("unknown Provider")
            };
        }

        public static DbCommand CreateCommand(string name, DbConnection connection, EProvider Provider)
        {
            DbCommand command = Provider switch
            {
                EProvider.SqlServer => new SqlCommand(name, (SqlConnection)connection),
                EProvider.MySql => new MySqlCommand(name, (MySqlConnection)connection),
                EProvider.PostgreSQL => new Npgsql.NpgsqlCommand(name, (Npgsql.NpgsqlConnection)connection),
                EProvider.Oracle => new OracleCommand(name, (OracleConnection)connection),
                _ => throw new Exception("unknown Provider")
            };

            DbProviderHelper.ConfigureCommand(command, Provider);

            return command;
        }

        public static DbParameter CreateParameter(EProvider Provider, string name, object? value)
        {
            var normalizedName = DbProviderHelper.NormalizeParameterName(Provider, name);

            return Provider switch
            {
                EProvider.MySql => new MySqlParameter(normalizedName, value),
                EProvider.SqlServer => new SqlParameter(normalizedName, value),
                EProvider.PostgreSQL => new Npgsql.NpgsqlParameter(normalizedName, value),
                EProvider.Oracle => new OracleParameter(normalizedName, value),
                _ => throw new Exception("unknown Provider")
            };
        }
    }
}

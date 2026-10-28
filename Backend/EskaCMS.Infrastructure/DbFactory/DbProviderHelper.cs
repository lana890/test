using EskaCMS.Infrastructure.Enums;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data.Common;

namespace EskaCMS.Infrastructure.DbFactory
{
    public static class DbProviderHelper
    {
        public static EProvider ParseProvider(string? provider)
        {
            return provider switch
            {
                "MySql" => EProvider.MySql,
                "PostgreSQL" => EProvider.PostgreSQL,
                "SqlServer" => EProvider.SqlServer,
                "Oracle" => EProvider.Oracle,
                _ => throw new InvalidOperationException($"Unsupported database provider: {provider}")
            };
        }

        public static string NormalizeParameterName(EProvider provider, string parameterName)
        {
            if (provider != EProvider.Oracle || string.IsNullOrWhiteSpace(parameterName))
            {
                return parameterName;
            }

            return parameterName.StartsWith("@")
                ? $":{parameterName[1..]}"
                : parameterName;
        }

        public static string NormalizeParameterName(DbCommand command, string parameterName)
        {
            if (command is OracleCommand)
            {
                return NormalizeParameterName(EProvider.Oracle, parameterName);
            }

            return parameterName;
        }

        public static void ConfigureCommand(DbCommand command, EProvider provider)
        {
            if (provider == EProvider.Oracle && command is OracleCommand oracleCommand)
            {
                oracleCommand.BindByName = true;
            }
        }

        public static void ConfigureCommand(DbCommand command)
        {
            if (command is OracleCommand oracleCommand)
            {
                oracleCommand.BindByName = true;
            }
        }
    }
}

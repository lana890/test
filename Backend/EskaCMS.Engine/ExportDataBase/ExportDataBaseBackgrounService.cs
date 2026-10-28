using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;
using Npgsql;
using System.Diagnostics;
using Oracle.ManagedDataAccess.Client;
using EskaCMS.Infrastructure.DbFactory;
using EskaCMS.Infrastructure.Enums;
using System.Data.SqlClient;

namespace EskaCMS.Engine.ExportDataBase
{
    public class ExportDataBaseBackgrounService : BaseBackgroundService
    {
        private readonly IConfiguration _configration;
        public readonly ILogger<ExportDataBaseBackgrounService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource tokenSource = new();
        private readonly string _BackgroundServiceName;
        private EProvider Provider;

        public ExportDataBaseBackgrounService(ILogger<ExportDataBaseBackgrounService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
            : base(nameof(ExportDataBaseBackgrounService), logger, serviceProvider, configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            //_BackgroundServiceName = BackgroundServiceName;
            _configration = configuration;
            Provider = DbProviderHelper.ParseProvider(configuration.GetValue<string>("ConnectionStrings:DatabaseProvider"));
        }
        public override async Task<bool> Execute()
        {
            try
            {
                var FolderName = "c:\\backup";

                string ConnectionString;
                string databaseName;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var DBInfo = scope.ServiceProvider.GetRequiredService<EskaDCMSDbContext>();
                    ConnectionString = DBInfo.Database.GetConnectionString() + ";Convert Zero Datetime=True";
                    databaseName = DBInfo.Database.GetDbConnection().Database;
                }
                var fileName = "\\" + databaseName + ".sql";
                var fullPath = Path.Combine(FolderName.Trim() + fileName);



                //if (!Directory.Exists(FolderName))
                //{

                //    Directory.CreateDirectory(FolderName);
                //}
                //if (Directory.GetFiles(Path.GetDirectoryName(fileName)) != null)
                //{
                //    File.Delete(fullPath);
                //}
                if (Provider == EProvider.MySql)
                {
                    using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                    {
                        using (MySqlCommand command = new MySqlCommand())
                        {
                            command.Connection = connection;
                            using (var sqlBackup = new MySqlBackup(command))
                            {
                                connection.Open();
                                sqlBackup.ExportToFile(fullPath);
                                connection.Close();
                            }
                        }
                    }
                    return true;
                }
                else if (Provider == EProvider.SqlServer)
                {
                    using (SqlConnection connection = new SqlConnection(ConnectionString))
                    {
                        connection.Open(); 
                        string backupQuery = $@" BACKUP DATABASE [{databaseName}] TO DISK = @backupPath WITH FORMAT, MEDIANAME = 'SQLServerBackups', NAME = 'Full Backup of {databaseName}';";
                        using (SqlCommand command = new SqlCommand(backupQuery, connection))
                        { 
                          command.CommandTimeout = 600; 
                          command.Parameters.AddWithValue("@backupPath", fullPath);
                            command.ExecuteNonQuery(); } connection.Close(); 
                    } return true;
                }
                // Ensure backup directory exists
                else if (Provider == EProvider.PostgreSQL)
                {
                    if (!Directory.Exists(FolderName))

                        Directory.CreateDirectory(FolderName);



                    // Delete existing backup file if exists

                    if (File.Exists(fullPath))

                        File.Delete(fullPath);



                    // PostgreSQL uses pg_dump for backups
                    var pgDumpPath = _configration["BackupSettings:PgDumpPath"];

                    // Parse connection string
                    var builder = new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString);

                    var psi = new ProcessStartInfo
                    {
                        FileName = pgDumpPath,
                        Arguments = $"-U {builder.Username} -h {builder.Host} -p {builder.Port} -F c -b -v -f \"{fullPath}\" {databaseName}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };



                    // PostgreSQL password handling
                    psi.EnvironmentVariables["PGPASSWORD"] = builder.Password;

                    using (var process = Process.Start(psi))
                    {

                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                            _logger.LogInformation(output);
                            if (!string.IsNullOrWhiteSpace(error))
                                _logger.LogWarning(error);
                        }
                    }

                    return true;
                }
                else if (Provider == EProvider.Oracle)
                {
                    var builder = new OracleConnectionStringBuilder(ConnectionString);
                    _logger.LogWarning("Oracle backup is not automated by this service. Configure RMAN or Data Pump for schema {SchemaName}.", builder.UserID);
                    return false;
                }

            }

            catch (TaskCanceledException ex)
            {
                _logger.LogInformation(ex.ToString());

            }
            return true;

        }
    }
    }

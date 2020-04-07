using Database.Initialization;
using Hangfire.SQLite;
using Hangfire.SqlServer;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Initialization
{
    public static class HangfireInitializer
    {
        public static List<string> Tables => new List<string>()
        {
            "AggregatedCounter",
            "Counter",
            "Hash",
            "JobParameter",
            "JobQueue",
            "List",
            "Schema",
            "Server",
            "Set",
            "State",
            "Job"
        };

        #region Ensure Db and Tables Created
        public static async Task<bool> EnsureDbAndTablesCreatedAsync(string connectionString, Action<JobStorageOptions> config = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    return await EnsureDbAndTablesCreatedAsync(connection, config, cancellationToken);
                }
            }
            else
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    return await EnsureDbAndTablesCreatedAsync(connection, config, cancellationToken);
                }
            }
        }

         public static async Task<bool> EnsureDbAndTablesCreatedAsync(DbConnection existingConnection, Action<JobStorageOptions> config = null, CancellationToken cancellationToken = default)
        {
            if (existingConnection is SqliteConnection)
            {
                await EnsureDbCreatedAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken);

                Action<JobStorageOptions> newConfig = (options) =>
                {
                    if (config != null)
                        config(options);

                    options.PrepareSchemaIfNecessary = true;
                };

                //Initialize Schema
                HangfireJobStorage.GetJobStorage(existingConnection, newConfig);

                return !persistedTables.Any(x => x.TableName.Contains("AggregatedCounter"));
            }
            else if (existingConnection is Microsoft.Data.SqlClient.SqlConnection || existingConnection is System.Data.SqlClient.SqlConnection)
            {
                var created = await EnsureDbCreatedAsync(existingConnection, cancellationToken).ConfigureAwait(false);
    
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(existingConnection.ConnectionString))
                {
                    var persistedTables = await DbInitializer.TablesAsync(connection, cancellationToken);

                    Action<JobStorageOptions> newConfig = (options) =>
                    {
                        if (config != null)
                            config(options);

                        options.PrepareSchemaIfNecessary = true;
                    };

                    //Initialize Schema
                    HangfireJobStorage.GetJobStorage(connection, newConfig);

                    return !persistedTables.Any(x => x.TableName.Contains("AggregatedCounter"));
                }             
            }
            else
            {
                throw new Exception("Unsupported Connection");
            }
        }
        #endregion

        #region Ensure Db Created
        public static Task<bool> EnsureDbCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureCreatedAsync(connectionString, cancellationToken);
        }

        public static Task<bool> EnsureDbCreatedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureCreatedAsync(existingConnection, cancellationToken);
        }
        #endregion

        #region Ensure Tables Deleted
        public static Task<bool> EnsureTablesDeletedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return EnsureTablesDeletedAsync(connectionString, "HangFire", cancellationToken);
        }
        public static async Task<bool> EnsureTablesDeletedAsync(string connectionString, string schemaName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    return await EnsureTablesDeletedAsync(connection, schemaName, cancellationToken);
                }
            }
            else
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    return await EnsureTablesDeletedAsync(connection, schemaName, cancellationToken);
                }
            }
        }

        public static Task<bool> EnsureTablesDeletedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
        {
            return EnsureTablesDeletedAsync(existingConnection, "HangFire", cancellationToken);
        }
        public static async Task<bool> EnsureTablesDeletedAsync(DbConnection existingConnection, string schemaName, CancellationToken cancellationToken = default)
        {
            var commands = new List<String>();
            if (existingConnection is SqliteConnection)
            {
                bool dbExists = await DbInitializer.ExistsAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                    var opened = false;
                    if(existingConnection.State != System.Data.ConnectionState.Open)
                    {
                        await existingConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        opened = true;
                    }

                    var deleteTables = Tables.Where(x => persistedTables.Any(p => (p.TableName == x || p.TableName == $"{schemaName}.{x}") && (p.Schema == schemaName || string.IsNullOrEmpty(p.Schema))));

                    //Drop tables
                    foreach (var t in deleteTables)
                    {
                        var commandSql = $"DROP TABLE [{schemaName}.{t}];";
                        commands.Add(commandSql);
                    }

                    bool deleted = false;

                    try
                    {
                        using (SqliteTransaction transaction = ((SqliteConnection)existingConnection).BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                deleted = true;
                                using (var command = new SqliteCommand(commandSql, ((SqliteConnection)existingConnection), transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            transaction.Commit();
                        }
                    }
                    finally
                    {
                        if(opened && existingConnection.State == System.Data.ConnectionState.Open)
                        {
                            existingConnection.Close();
                        }
                    }

                    return deleted;
                }
                else
                {
                    return await DbInitializer.EnsureDestroyedAsync(existingConnection, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (existingConnection is Microsoft.Data.SqlClient.SqlConnection || existingConnection is System.Data.SqlClient.SqlConnection)
            {
                bool dbExists = await DbInitializer.ExistsAsync(existingConnection, cancellationToken);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                    var opened = false;
                    if (existingConnection.State != System.Data.ConnectionState.Open)
                    {
                        await existingConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        opened = true;
                    }

                    var deleteTables = Tables.Where(x => persistedTables.Any(p => (p.TableName == x || p.TableName == $"{schemaName}.{x}") && (p.Schema == schemaName || string.IsNullOrEmpty(p.Schema))));

                    //Drop tables
                    foreach (var t in deleteTables)
                    {
                        var commandSql = $"DROP TABLE [{schemaName}].[{t}];";
                        commands.Add(commandSql);
                    }

                    bool deleted = false;
                    try
                    {
                        if(existingConnection is Microsoft.Data.SqlClient.SqlConnection sqlConnection)
                        {
                            using (Microsoft.Data.SqlClient.SqlTransaction transaction = sqlConnection.BeginTransaction())
                            {
                                foreach (var commandSql in commands)
                                {
                                    deleted = true;
                                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(commandSql, sqlConnection, transaction))
                                    {
                                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                    }
                                }

                                transaction.Commit();
                            }
                        }
                        else if (existingConnection is System.Data.SqlClient.SqlConnection systemSqlConnection)
                        {
                            using (System.Data.SqlClient.SqlTransaction transaction = systemSqlConnection.BeginTransaction())
                            {
                                foreach (var commandSql in commands)
                                {
                                    deleted = true;
                                    using (var command = new System.Data.SqlClient.SqlCommand(commandSql, systemSqlConnection, transaction))
                                    {
                                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                    }
                                }

                                transaction.Commit();
                            }
                        }
                    }
                    finally
                    {
                        if(opened && existingConnection.State == System.Data.ConnectionState.Open)
                        {
                            existingConnection.Close();
                        }
                    }

                    return deleted;
                }
                else
                {
                    return await DbInitializer.EnsureDestroyedAsync(existingConnection, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                throw new Exception("Unsupported Connection");
            }
        }
        #endregion

        #region Ensure Db Destroyed
        public static Task<bool> EnsureDbDestroyedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken);
        }

        public static Task<bool> EnsureDbDestroyedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureDestroyedAsync(existingConnection, cancellationToken);
        }
        #endregion
    }
}

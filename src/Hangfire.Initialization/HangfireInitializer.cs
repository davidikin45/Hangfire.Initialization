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
        public static List<(string Schema, string TableName)> Tables => new List<(string Schema, string TableName)>()
        {
            ("HangFire", "AggregatedCounter"),
            ("HangFire", "Counter"),
            ("HangFire", "Hash"),
            ("HangFire", "JobParameter"),
            ("HangFire", "JobQueue"),
            ("HangFire", "List"),
            ("HangFire", "Schema"),
            ("HangFire", "Server"),
            ("HangFire", "Set"),
            ("HangFire", "State"),
            ("HangFire", "Job")
        };

        #region Ensure Db and Tables Created
        public static async Task<bool> EnsureDbAndTablesCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    return await EnsureDbAndTablesCreatedAsync(connection, cancellationToken);
                }
            }
            else
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    return await EnsureDbAndTablesCreatedAsync(connection, cancellationToken);
                }
            }
        }

        public static async Task<bool> EnsureDbAndTablesCreatedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
        {
            if (existingConnection is SqliteConnection)
            {
                await EnsureDbCreatedAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken);

                var options = new SQLiteStorageOptions
                {
                    PrepareSchemaIfNecessary = true
                };

                //Initialize Schema
                var storage = new SQLiteStorage((SqliteConnection)existingConnection, options);

                return !persistedTables.Any(x => x.TableName.Contains("AggregatedCounter"));
            }
            else if (existingConnection is SqlConnection)
            {
                await EnsureDbCreatedAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken);

                var options = new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = true
                };

                //Initialize Schema
                var storage = new SqlServerStorage(existingConnection, options);

                return !persistedTables.Any(x => x.TableName.Contains("AggregatedCounter"));
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
        public static async Task<bool> EnsureTablesDeletedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    return await EnsureDbAndTablesCreatedAsync(connection, cancellationToken);
                }
            }
            else
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    return await EnsureDbAndTablesCreatedAsync(connection, cancellationToken);
                }
            }
        }

        public static async Task<bool> EnsureTablesDeletedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
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

                    var deleteTables = Tables.Where(x => persistedTables.Any(p => (p.TableName == x.TableName || p.TableName == $"{x.Schema}.{x.TableName}") && (p.Schema == x.Schema || string.IsNullOrEmpty(p.Schema))));

                    //Drop tables
                    foreach (var t in deleteTables)
                    {
                        var commandSql = $"DROP TABLE [{t.Schema}.{t.TableName}];";
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
            else if (existingConnection is SqlConnection)
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

                    var deleteTables = Tables.Where(x => persistedTables.Any(p => (p.TableName == x.TableName || p.TableName == $"{x.Schema}.{x.TableName}") && (p.Schema == x.Schema || string.IsNullOrEmpty(p.Schema))));

                    //Drop tables
                    foreach (var t in deleteTables)
                    {
                        var commandSql = $"DROP TABLE [{t.Schema}].[{t.TableName}];";
                        commands.Add(commandSql);
                    }

                    bool deleted = false;
                    try
                    {
                        using (SqlTransaction transaction = ((SqlConnection)existingConnection).BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                deleted = true;
                                using (var command = new SqlCommand(commandSql, ((SqlConnection)existingConnection), transaction))
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

using Database.Initialization;
using Hangfire.SQLite;
using Hangfire.SqlServer;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Initialization
{
    public static class HangfireInitializer
    {
        public static async Task EnsureTablesDeletedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            var tableNames = new List<string>();

            tableNames.Add($"[HangFire].[AggregatedCounter]");
            tableNames.Add($"[HangFire].[Counter]");
            tableNames.Add($"[HangFire].[Hash]");
            tableNames.Add($"[HangFire].[Job]");
            tableNames.Add($"[HangFire].[JobParameter]");
            tableNames.Add($"[HangFire].[JobQueue]");
            tableNames.Add($"[HangFire].[List]");
            tableNames.Add($"[HangFire].[Schema]");
            tableNames.Add($"[HangFire].[Server]");
            tableNames.Add($"[HangFire].[Set]");
            tableNames.Add($"[HangFire].[State]");

            var commands = new List<String>();
            if (string.IsNullOrEmpty(connectionString))
            {

            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                bool dbExists = await DbInitializer.ExistsAsync(connectionString, cancellationToken).ConfigureAwait(false);

                if (dbExists)
                {
                    using (var conn = new SqliteConnection(connectionString))
                    {
                        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (SqliteTransaction transaction = conn.BeginTransaction())
                        {
                            //Drop tables
                            foreach (var tableName in tableNames)
                            {
                                foreach (var t in tableNames)
                                {
                                    try
                                    {
                                        var commandSql = $"DROP TABLE IF EXISTS {t.Replace("].[", ".")};";
                                        using (var command = new SqliteCommand(commandSql, conn, transaction))
                                        {
                                            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                        }

                                        commands.Add(commandSql);
                                    }
                                    catch
                                    {

                                    }
                                }
                            }

                            transaction.Rollback();
                        }

                        using (SqliteTransaction transaction = conn.BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                using (var command = new SqliteCommand(commandSql, conn, transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            transaction.Commit();
                        }
                    }
                }
                else
                {
                    await DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                bool dbExists = await DbInitializer.ExistsAsync(connectionString, cancellationToken);

                if (dbExists)
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (SqlTransaction transaction = conn.BeginTransaction())
                        {
                            //Drop tables
                            foreach (var tableName in tableNames)
                            {
                                foreach (var t in tableNames)
                                {
                                    try
                                    {
                                        var commandSql = $"DROP TABLE IF EXISTS {t}";
                                        using (var command = new SqlCommand(commandSql, conn, transaction))
                                        {
                                            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                        }

                                        commands.Add(commandSql);
                                    }
                                    catch
                                    {

                                    }
                                }
                            }

                            transaction.Rollback();
                        }

                        using (SqlTransaction transaction = conn.BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                using (var command = new SqlCommand(commandSql, conn, transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            transaction.Commit();
                        }
                    }
                }
                else
                {
                    await DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public static Task<bool> EnsureDbCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureCreatedAsync(connectionString, cancellationToken);
        }

        public static async Task EnsureDbAndTablesCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            await EnsureDbCreatedAsync(connectionString, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(connectionString))
            {

            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                var options = new SQLiteStorageOptions
                {
                    PrepareSchemaIfNecessary = true
                };

                //Initialize Schema
                var storage = new SQLiteStorage(connectionString, options);
            }
            else
            {
                var options = new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = true
                };

                //Initialize Schema
                var storage = new SqlServerStorage(connectionString, options);
            }
        }

        public static Task EnsureDbDestroyedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken);
        }
    }
}

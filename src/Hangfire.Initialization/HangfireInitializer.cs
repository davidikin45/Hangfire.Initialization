using Database.Initialization;
using Hangfire.SQLite;
using Hangfire.SqlServer;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Initialization
{
    public static class HangfireInitializer
    {
        public static List<(string Schema, string Table)> TableNames => new List<(string Schema, string Table)>()
        {
            ("HangFire", "AggregatedCounter"),
            ("HangFire", "Counter"),
            ("HangFire", "Hash"),
            ("HangFire", "Job"),
            ("HangFire", "JobParameter"),
            ("HangFire", "JobQueue"),
            ("HangFire", "List"),
            ("HangFire", "Schema"),
            ("HangFire", "Server"),
            ("HangFire", "Set"),
            ("HangFire", "State"),
        };
        public static async Task<bool> EnsureTablesDeletedAsync(string connectionString, CancellationToken cancellationToken = default)
        {

            var commands = new List<String>();
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                bool dbExists = await DbInitializer.ExistsAsync(connectionString, cancellationToken).ConfigureAwait(false);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TableNamesAsync(connectionString, cancellationToken).ConfigureAwait(false);

                    using (var conn = new SqliteConnection(connectionString))
                    {
                        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (SqliteTransaction transaction = conn.BeginTransaction())
                        {
                            var deleteTables = TableNames.Where(x => persistedTables.Contains(x.Table));

                            //Drop tables
                            foreach (var tableName in deleteTables)
                            {
                                foreach (var t in deleteTables)
                                {
                                    try
                                    {
                                        var commandSql = $"DROP TABLE [{t.Schema}.{t.Table}];";
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

                        bool deleted = false;
                        using (SqliteTransaction transaction = conn.BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                deleted = true;
                                using (var command = new SqliteCommand(commandSql, conn, transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            transaction.Commit();
                        }

                        return deleted;
                    }
                }
                else
                {
                    return await DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                bool dbExists = await DbInitializer.ExistsAsync(connectionString, cancellationToken);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TableNamesAsync(connectionString, cancellationToken).ConfigureAwait(false);

                    using (var conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (SqlTransaction transaction = conn.BeginTransaction())
                        {
                            var deleteTables = TableNames.Where(x => persistedTables.Contains(x.Table));

                            //Drop tables
                            foreach (var tableName in deleteTables)
                            {
                                foreach (var t in deleteTables)
                                {
                                    try
                                    {
                                        var commandSql = $"DROP TABLE [{t.Schema}].[{t.Table}]";
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

                        bool deleted = false;
                        using (SqlTransaction transaction = conn.BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                deleted = true;
                                using (var command = new SqlCommand(commandSql, conn, transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            transaction.Commit();
                        }

                        return deleted;
                    }
                }
                else
                {
                    return await DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken).ConfigureAwait(false);
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

        public static Task<bool> EnsureDbDestroyedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken);
        }
    }
}

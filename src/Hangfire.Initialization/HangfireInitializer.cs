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
        public static async Task<bool> EnsureTablesDeletedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            var commands = new List<String>();
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            else if (Database.Initialization.ConnectionStringHelper.IsSQLite(connectionString))
            {
                bool dbExists = await DbInitializer.ExistsAsync(connectionString, cancellationToken).ConfigureAwait(false);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TablesAsync(connectionString, cancellationToken).ConfigureAwait(false);

                    using (var conn = new SqliteConnection(connectionString))
                    {
                        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                        var deleteTables = Tables.Where(x => persistedTables.Any(p => (p.TableName == x.TableName || p.TableName == $"{x.Schema}.{x.TableName}") && (p.Schema == x.Schema || string.IsNullOrEmpty(p.Schema))));

                        //Drop tables
                        foreach (var t in deleteTables)
                        {
                            var commandSql = $"DROP TABLE [{t.Schema}.{t.TableName}];";
                            commands.Add(commandSql);
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
                    var persistedTables = await DbInitializer.TablesAsync(connectionString, cancellationToken).ConfigureAwait(false);

                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        var deleteTables = Tables.Where(x => persistedTables.Any(p => (p.TableName == x.TableName || p.TableName == $"{x.Schema}.{x.TableName}") && (p.Schema == x.Schema || string.IsNullOrEmpty(p.Schema))));

                        //Drop tables
                        foreach (var t in deleteTables)
                        {
                            var commandSql = $"DROP TABLE [{t.Schema}.{t.TableName}];";
                            commands.Add(commandSql);
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

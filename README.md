# Hangfire Initialization for SQL Server, SQLite and SQLite InMemory
[![nuget](https://img.shields.io/nuget/v/Hangfire.Initialization.svg)](https://www.nuget.org/packages/Hangfire.Initialization/) ![Downloads](https://img.shields.io/nuget/dt/Hangfire.Initialization.svg "Downloads")

* I have created the following extension methods which aims to allow Hangfire to be initialized & destroyed independently in a similar manner to how EF Core initialization works. A key addition is the ability to create a new database if it doesn't already exist.
* Supports SQL Server, SQLite and SQLite InMemory.
* It also promotes [db initialization within program.cs Main method rather than startup class](https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/intro?view=aspnetcore-2.2). This is good practice especially when using the [WebApplicationFactory for Integration Tests](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-2.2).

## Installation

### NuGet
```
PM> Install-Package Hangfire.Initialization
```

### .Net CLI
```
> dotnet add package Hangfire.Initialization
```

## Nuget Packages
* Hangfire
* Hanfire.MemoryStorage
* Hangfire.SQLite.Core

## Usage
* await HangfireInitializer.EnsureTablesDeletedAsync(connectionString) = Ensures only tables related to hangfire are deleted.
* await HangfireInitializer.EnsureDbCreatedAsync(connectionString) = Ensures hangfire physical database is created in preparation for hangfire to create schema.
* await HangfireInitializer.EnsureDbAndTablesCreatedAsync(connectionString) = Ensures hangfire physical database is created and tables created. [See docs](http://docs.hangfire.io/en/latest/configuration/using-sql-server.html)
* await HangfireInitializer.EnsureDbDestroyedAsync(connectionString) = Deletes physical database.
* Ensure Language Version is set to 'C# latest minor version (latest) to allow async Main.
* Ensure Main method is async.
```
 public class Program
{
	public static async Task Main(string[] args)
	{
		
	}
}
```

## Development and Integration Environment Example
```
var sqlServerConnectionString = "Server=(localdb)\\mssqllocaldb;Database=HangfireDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;";
await HangfireInitializer.EnsureTablesDeletedAsync(sqlServerConnectionString);
await HangfireInitializer.EnsureDbAndTablesCreatedAsync(sqlServerConnectionString);

var sqliteConnectionString = "Data Source=Hangfire.db;";
await HangfireInitializer.EnsureTablesDeletedAsync(sqliteConnectionString);
await HangfireInitializer.EnsureDbAndTablesCreatedAsync(sqliteConnectionString);
```

## Staging and Production Environment Example
```
var sqlServerConnectionString = "Server=(localdb)\\mssqllocaldb;Database=HangfireDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;";
await HangfireInitializer.EnsureDbAndTablesCreatedAsync(sqlServerConnectionString);

var sqliteConnectionString = "Data Source=Hangfire.db;";
await HangfireInitializer.EnsureDbAndTablesCreatedAsync(sqliteConnectionString);
```

## Example
```
public static async Task Main(string[] args)
{
     var host = CreateWebHostBuilder(args).Build();

    using (var scope = host.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var configuration = services.GetRequiredService<IConfiguration>();
			var connectionString = configuration.GetConnectionString("DefaultConnection");
            await HangfireInitializer.EnsureTablesDeletedAsync(connectionString);
			await HangfireInitializer.EnsureDbAndTablesCreatedAsync(connectionString);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing Hangfire db.");
        }
    }

    host.Run();
}

public class Startup
{
     public IConfiguration Configuration { get; }

	 public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}
	
	public virtual void ConfigureServices(IServiceCollection services)
	{
		var connectionString = Configuration.GetConnectionString("DefaultConnection");
		services.AddHangfire(config =>
		{
			var options = new SqlServerStorageOptions
			{
				//Won't try and create database tables
				PrepareSchemaIfNecessary = false
			};

			config.UseSqlServerStorage(connectionString, options);
		});
	}

	public void Configure(IApplicationBuilder app)
	{
		app.UseHangfireDashboard();
		app.UseHangfireServer();
	}
}
```

## See Also
* [Database Initialization](https://github.com/davidikin45/Database.Initialization)
* [EntityFrameworkCore Initialization](https://github.com/davidikin45/EntityFrameworkCore.Initialization)
* [MiniProfiler Initialization](https://github.com/davidikin45/MiniProfilerDb.Initialization)
* [Hangfire.SqlServer](https://github.com/HangfireIO/Hangfire/tree/master/src/Hangfire.SqlServer)
* [Hangfire.SQLite](https://github.com/wanlitao/HangfireExtension)
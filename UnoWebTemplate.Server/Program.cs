using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetEnv;
using UnoWebTemplate.Server.Data;

namespace UnoWebTemplate.Server;

public class Program
{
    public static void Main(string[] args)
    {
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);

        // Database Configuration
        // Default to SQLite local file, but read from env to allow Docker or external overrides
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") 
            ?? "Data Source=app.db;";

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        // Initialize and configure Log4net
        builder.Logging.ClearProviders();
        builder.Logging.AddLog4Net("log4net.config");

        // Dynamically configure log4net database connection string at runtime
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository(entryAssembly);
            if (hierarchy != null)
            {
                var adoNetAppenders = hierarchy.GetAppenders().OfType<log4net.Appender.AdoNetAppender>();
                foreach (var appender in adoNetAppenders)
                {
                    // Map the SQLite connection string
                    appender.ConnectionString = connectionString;
                    appender.ActivateOptions();
                }
            }
        }

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Logger instance for bootstrap logging
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        // Automatically execute migrations on database at startup
        try
        {
            logger.LogInformation("Ensuring database is created...");
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            logger.LogInformation("Database is ready.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations.");
        }

        app.UseCors();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Configure custom content types to support Uno WebAssembly assets
        var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".dat"] = "application/octet-stream";
        contentTypeProvider.Mappings[".odb"] = "application/octet-stream";
        contentTypeProvider.Mappings[".pdb"] = "application/octet-stream";

        StaticFileOptions? fallbackOptions = null;

        if (app.Environment.IsDevelopment())
        {
            var clientWasmPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../UnoWebTemplate.Client/UnoWebTemplate.Client/bin/Debug/net10.0-browserwasm/wwwroot"));
            if (Directory.Exists(clientWasmPath))
            {
                var wasmFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientWasmPath);

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = wasmFileProvider,
                    ContentTypeProvider = contentTypeProvider
                });

                fallbackOptions = new StaticFileOptions
                {
                    FileProvider = wasmFileProvider
                };
            }
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = contentTypeProvider
        });

        // API endpoints
        var api = app.MapGroup("/api");

        api.MapGet("/status", () =>
        {
            logger.LogInformation("API Status endpoint queried.");
            return Results.Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow,
                Message = "Welcome to the UnoWebTemplate API"
            });
        });

        // Fallback SPA routing to host index.html
        if (fallbackOptions != null)
        {
            app.MapFallbackToFile("index.html", fallbackOptions);
        }
        else
        {
            app.MapFallbackToFile("index.html");
        }

        logger.LogInformation("Starting UnoWebTemplate server application...");
        app.Run();
    }
}

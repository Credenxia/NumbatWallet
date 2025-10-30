using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace NumbatWallet.Web.Api.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        // Create Serilog logger configuration
        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ApplicationName", environment.ApplicationName)
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .Enrich.WithProperty("Version", typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0");

        // Configure minimum log levels based on environment
        if (environment.IsDevelopment())
        {
            loggerConfiguration.MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Information);
        }
        else
        {
            loggerConfiguration.MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                .MinimumLevel.Override("System", LogEventLevel.Warning);
        }

        // Configure sinks based on environment
        ConfigureSinks(loggerConfiguration, configuration, environment);

        // Build the logger
        Log.Logger = loggerConfiguration.CreateLogger();

        // Replace the default logger with Serilog
        builder.Host.UseSerilog();

        // Add Serilog request logging
        builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);

        return builder;
    }

    private static void ConfigureSinks(LoggerConfiguration loggerConfiguration, ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        // Console sink with different formatting based on environment
        if (environment.IsDevelopment())
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate);
        }
        else
        {
            // Use compact JSON formatting in production for better parsing
            loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
        }

        // File sink for local logging
        var logPath = configuration["Logging:FilePath"] ?? "logs/numbatwallet-.log";
        loggerConfiguration.WriteTo.File(
            path: logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 104857600, // 100MB
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] [{RequestId}] {Message:lj}{NewLine}{Exception}",
            shared: true);

        // Application Insights sink can be added if Serilog.Sinks.ApplicationInsights package is installed
        // var appInsightsKey = configuration["ApplicationInsights:InstrumentationKey"] ??
        //                     configuration["ApplicationInsights:ConnectionString"];
        // if (!string.IsNullOrEmpty(appInsightsKey))
        // {
        //     loggerConfiguration.WriteTo.ApplicationInsights(
        //         appInsightsKey,
        //         new TraceTelemetryConverter(),
        //         restrictedToMinimumLevel: LogEventLevel.Information);
        // }

        // Seq sink if configured (for centralized logging) - requires Serilog.Sinks.Seq package
        // var seqUrl = configuration["Serilog:SeqUrl"];
        // var seqApiKey = configuration["Serilog:SeqApiKey"];
        // if (!string.IsNullOrEmpty(seqUrl))
        // {
        //     if (!string.IsNullOrEmpty(seqApiKey))
        //     {
        //         loggerConfiguration.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
        //     }
        //     else
        //     {
        //         loggerConfiguration.WriteTo.Seq(seqUrl);
        //     }
        // }

        // Azure Blob Storage sink if configured (for audit logging)
        var storageConnectionString = configuration["Azure:Storage:ConnectionString"];
        if (!string.IsNullOrEmpty(storageConnectionString) && !environment.IsDevelopment())
        {
            // Note: This would require additional package: Serilog.Sinks.AzureBlobStorage
            // Uncomment when package is added:
            // loggerConfiguration.WriteTo.AzureBlobStorage(
            //     connectionString: storageConnectionString,
            //     storageContainerName: "logs",
            //     storageFileName: "numbatwallet-{yyyy}/{MM}/{dd}/log.txt");
        }
    }

    public static WebApplication UseSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            // Customize the message template
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            // Emit debug level logs for successful requests
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null || httpContext.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }
                if (httpContext.Response.StatusCode >= 400)
                {
                    return LogEventLevel.Warning;
                }
                if (elapsed > 3000) // Slow requests
                {
                    return LogEventLevel.Warning;
                }
                return LogEventLevel.Information;
            };

            // Attach additional properties to the request log
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "Unknown");
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
                diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);

                // Add tenant information if available
                if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
                {
                    diagnosticContext.Set("TenantId", tenantId.ToString());
                }

                // Add user information if authenticated
                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "Unknown");
                    diagnosticContext.Set("UserName", httpContext.User.Identity.Name ?? "Unknown");
                }

                // Add response size
                if (httpContext.Response.ContentLength.HasValue)
                {
                    diagnosticContext.Set("ResponseContentLength", httpContext.Response.ContentLength.Value);
                }

                // Add custom headers for debugging
                if (httpContext.Request.Headers.TryGetValue("X-Request-Id", out var requestId))
                {
                    diagnosticContext.Set("CorrelationId", requestId.ToString());
                }
            };
        });

        return app;
    }

    /// <summary>
    /// Extension method to add structured logging for GraphQL requests
    /// </summary>
    public static IApplicationBuilder UseGraphQLLogging(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/graphql"))
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    await next();
                }
                finally
                {
                    stopwatch.Stop();

                    Log.Information("GraphQL Request completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                        stopwatch.ElapsedMilliseconds,
                        context.Response.StatusCode);
                }
            }
            else
            {
                await next();
            }
        });

        return app;
    }

    /// <summary>
    /// Extension method for logging health check results
    /// </summary>
    public static IApplicationBuilder UseHealthCheckLogging(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                var originalBodyStream = context.Response.Body;

                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await next();

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                if (context.Response.StatusCode != 200)
                {
                    Log.Warning("Health check failed with status {StatusCode}: {Response}",
                        context.Response.StatusCode, responseText);
                }

                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                await next();
            }
        });

        return app;
    }
}
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var postgresDb = postgres.AddDatabase("numbatwallet");

// Add Redis cache
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

// Add Azure Storage emulator (Azurite)
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .WithBlobPort(10000)
    .WithQueuePort(10001)
    .WithTablePort(10002);

var blobs = storage.AddBlobs("blobs");

// Add Application Insights (local development)
// Note: In production, this would connect to actual Azure Application Insights
var appInsights = builder.AddContainer("applicationinsights", "mcr.microsoft.com/dotnet/nightly/aspire-dashboard")
    .WithHttpEndpoint(port: 18888, targetPort: 18888, name: "dashboard");

// Add Web API project with references to infrastructure
var api = builder.AddProject<Projects.NumbatWallet_Web_Api>("webapi")
    .WithReference(postgresDb)
    .WithReference(redis)
    .WithReference(blobs)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ConnectionStrings__DefaultConnection", postgresDb.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__Redis", redis.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__BlobStorage", blobs.Resource.ConnectionStringExpression);

// Add Admin Portal with references
var admin = builder.AddProject<Projects.NumbatWallet_Web_Admin>("admin")
    .WithReference(postgresDb)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ConnectionStrings__DefaultConnection", postgresDb.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__Redis", redis.Resource.ConnectionStringExpression);

// Add health checks dashboard
builder.AddContainer("healthchecks", "xabarilcoding/healthchecksui")
    .WithHttpEndpoint(port: 5000, targetPort: 80, name: "health-ui")
    .WithEnvironment("HealthChecksUI__HealthChecks__0__Name", "Web API")
    .WithEnvironment("HealthChecksUI__HealthChecks__0__Uri", "http://webapi/health")
    .WithEnvironment("HealthChecksUI__HealthChecks__1__Name", "Admin Portal")
    .WithEnvironment("HealthChecksUI__HealthChecks__1__Uri", "http://admin/health")
    .WithEnvironment("HealthChecksUI__EvaluationTimeInSeconds", "30")
    .WithEnvironment("HealthChecksUI__MinimumSecondsBetweenFailureNotifications", "60");

// Add pgAdmin for database management
builder.AddContainer("pgadmin", "dpage/pgadmin4")
    .WithHttpEndpoint(port: 5050, targetPort: 80, name: "pgadmin")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@numbatwallet.local")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "admin123")
    .WithEnvironment("PGADMIN_CONFIG_SERVER_MODE", "False");

// Add Seq for structured logging (optional, better than console)
var seq = builder.AddSeq("seq")
    .WithDataVolume();

// Update API and Admin to use Seq
api.WithReference(seq);
admin.WithReference(seq);

builder.Build().Run();
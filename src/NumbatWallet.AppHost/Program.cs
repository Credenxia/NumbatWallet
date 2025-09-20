using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database with fixed port
var postgres = builder.AddPostgres("postgres")
    .WithEndpoint(port: 5432, targetPort: 5432, name: "postgres")
    .WithDataVolume()
    .WithPgAdmin();

var postgresDb = postgres.AddDatabase("numbatwallet");

// Add Redis cache with fixed port
var redis = builder.AddRedis("redis")
    .WithEndpoint(port: 6379, targetPort: 6379, name: "redis")
    .WithDataVolume()
    .WithRedisCommander();

// Add Azure Storage emulator (Azurite)
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

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
    .WithHttpEndpoint(targetPort: 80, name: "health-ui")
    .WithEnvironment("HealthChecksUI__HealthChecks__0__Name", "Web API")
    .WithEnvironment("HealthChecksUI__HealthChecks__0__Uri", "http://webapi/health")
    .WithEnvironment("HealthChecksUI__HealthChecks__1__Name", "Admin Portal")
    .WithEnvironment("HealthChecksUI__HealthChecks__1__Uri", "http://admin/health")
    .WithEnvironment("HealthChecksUI__EvaluationTimeInSeconds", "30")
    .WithEnvironment("HealthChecksUI__MinimumSecondsBetweenFailureNotifications", "60");

// pgAdmin is already added via .WithPgAdmin() on the postgres resource

// Add Seq for structured logging (optional, better than console)
// TODO: Enable Seq when package is added
// var seq = builder.AddSeq("seq")
//     .WithDataVolume();

// Update API and Admin to use Seq
// api.WithReference(seq);
// admin.WithReference(seq);

builder.Build().Run();
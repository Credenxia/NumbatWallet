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
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");

// Configure Azure services via environment variables for development
// In production, these would point to actual Azure resources

// Add Web API project with references to infrastructure
var api = builder.AddProject<Projects.NumbatWallet_Web_Api>("webapi")
    .WithReference(postgresDb)
    .WithReference(redis)
    .WithReference(blobs)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Azure__KeyVault__VaultUri", "https://localhost:7001/") // Local Key Vault emulator
    .WithEnvironment("Azure__ServiceBus__ConnectionString", "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fake-key") // Local Service Bus emulator
    .WithEnvironment("ApplicationInsights__ConnectionString", "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost:4317/") // Local OTEL endpoint
    .WithEnvironment("Azure__UseEmulators", "true"); // Flag for services to use local emulators

// Add Admin Portal - only references API, not database directly (Clean Architecture)
var admin = builder.AddProject<Projects.NumbatWallet_Web_Admin>("admin")
    .WithReference(api)  // Admin communicates only through API
    .WithExternalHttpEndpoints()  // Make admin portal accessible from outside
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

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

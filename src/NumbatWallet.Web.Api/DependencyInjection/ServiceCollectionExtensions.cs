using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using NumbatWallet.Web.Api.GraphQL;
using HotChocolate.Execution.Configuration;

namespace NumbatWallet.Web.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Controllers with JSON options
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Add API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("x-api-version"),
                new MediaTypeApiVersionReader("x-api-version"));
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins", builder =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:3000" };

                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Add Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        // Add Authorization
        services.AddAuthorization(options =>
        {
            // Policy for citizen users (via ServiceWA)
            options.AddPolicy("CitizenUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("user_type", "citizen");
            });

            // Policy for government officers
            options.AddPolicy("GovernmentOfficer", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("user_type", "officer");
                policy.RequireRole("Officer", "Admin");
            });

            // Policy for system administrators
            options.AddPolicy("SystemAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

            // Policy for API access
            options.AddPolicy("ApiAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "api.access");
            });
        });

        // Add Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NumbatWallet API",
                Version = "v1",
                Description = "Digital Wallet and Verifiable Credentials API for Western Australia",
                Contact = new OpenApiContact
                {
                    Name = "NumbatWallet Team",
                    Email = "support@numbatwallet.wa.gov.au"
                }
            });

            // Add JWT Authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    public static IRequestExecutorBuilder AddGraphQL(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddSubscriptionType<Subscription>()
            .AddType<WalletType>()
            .AddType<CredentialType>()
            .AddType<PersonType>()
            .AddType<IssuerType>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddErrorFilter<GraphQLErrorFilter>()
            .ModifyRequestOptions(opt =>
            {
                opt.IncludeExceptionDetails = configuration.GetValue<bool>("GraphQL:IncludeExceptionDetails");
            });
    }
}
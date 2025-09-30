using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace NumbatWallet.Web.Api.Tests.TestHelpers;

public abstract class ApiTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected WebApplicationFactory<Program> Factory { get; }

    protected ApiTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
    }

    protected HttpClient CreateAuthenticatedClient(Action<IServiceCollection>? configureServices = null)
    {
        return Factory.WithWebHostBuilder(builder =>
        {

            builder.ConfigureTestServices(services =>
            {
                // Remove ALL existing authentication related services
                var authServices = services.Where(s =>
                    s.ServiceType.FullName?.Contains("Authentication") == true ||
                    s.ServiceType.FullName?.Contains("Authorization") == true).ToList();

                foreach (var service in authServices)
                {
                    services.Remove(service);
                }

                // Add test authentication fresh
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    options => { });

                services.AddAuthorization();

                // Apply any additional service configuration
                configureServices?.Invoke(services);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected HttpClient CreateAnonymousClient(Action<IServiceCollection>? configureServices = null)
    {
        return Factory.WithWebHostBuilder(builder =>
        {

            builder.ConfigureTestServices(services =>
            {
                // Apply any additional service configuration
                configureServices?.Invoke(services);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
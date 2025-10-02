using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace NumbatWallet.Tests.Shared;

/// <summary>
/// Base class for all unit tests
/// Provides common utilities, mocking, and dependency injection
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly ITestOutputHelper Output;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly MockRepository MockRepository;
    protected readonly CancellationTokenSource CancellationTokenSource;

    protected TestBase(ITestOutputHelper output)
    {
        Output = output;
        MockRepository = new MockRepository(MockBehavior.Strict);
        CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Override to configure services for testing
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
    }

    /// <summary>
    /// Create a mock with strict behavior
    /// Automatically verifies all setups were called
    /// </summary>
    protected Mock<T> CreateMock<T>() where T : class
    {
        return MockRepository.Create<T>();
    }

    /// <summary>
    /// Create a mock with loose behavior
    /// Does not require all methods to be setup
    /// </summary>
    protected Mock<T> CreateLooseMock<T>() where T : class
    {
        return new Mock<T>(MockBehavior.Loose);
    }

    /// <summary>
    /// Get a service from the test service provider
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get an optional service from the test service provider
    /// </summary>
    protected T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// Write output to test console
    /// </summary>
    protected void WriteOutput(string message)
    {
        Output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
    }

    /// <summary>
    /// Write formatted output to test console
    /// </summary>
    protected void WriteOutput(string format, params object[] args)
    {
        Output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {string.Format(format, args)}");
    }

    /// <summary>
    /// Dispose of test resources
    /// Verifies all mock setups were called
    /// </summary>
    public virtual void Dispose()
    {
        try
        {
            MockRepository.VerifyAll();
        }
        finally
        {
            CancellationTokenSource.Dispose();
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Base class for unit tests that don't need DI or mocking
/// Lightweight alternative to TestBase
/// </summary>
public abstract class SimpleTestBase
{
    protected readonly ITestOutputHelper Output;
    protected readonly CancellationTokenSource CancellationTokenSource;

    protected SimpleTestBase(ITestOutputHelper output)
    {
        Output = output;
        CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    }

    protected void WriteOutput(string message)
    {
        Output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
    }

    protected void WriteOutput(string format, params object[] args)
    {
        Output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {string.Format(format, args)}");
    }

    public virtual void Dispose()
    {
        CancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}

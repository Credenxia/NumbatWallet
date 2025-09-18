using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.CQRS;
using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Tests.CQRS;

public class DispatcherTests
{
    private readonly IServiceCollection _services;
    private readonly Mock<ILogger<Dispatcher>> _loggerMock;

    public DispatcherTests()
    {
        _services = new ServiceCollection();
        _loggerMock = new Mock<ILogger<Dispatcher>>();
    }

    [Fact]
    public async Task SendAsync_WithCommand_ShouldExecuteHandler()
    {
        // Arrange
        var handlerMock = new Mock<ICommandHandler<TestCommand>>();
        _services.AddSingleton(handlerMock.Object);

        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = new Dispatcher(serviceProvider, _loggerMock.Object);
        var command = new TestCommand { Value = "test" };

        // Act
        await dispatcher.SendAsync(command);

        // Assert
        handlerMock.Verify(x => x.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithCommandResult_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = new TestResult { Data = "result" };
        var handlerMock = new Mock<ICommandHandler<TestCommandWithResult, TestResult>>();
        handlerMock.Setup(x => x.HandleAsync(It.IsAny<TestCommandWithResult>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResult);

        _services.AddSingleton(handlerMock.Object);
        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = new Dispatcher(serviceProvider, _loggerMock.Object);
        var command = new TestCommandWithResult { Value = "test" };

        // Act
        var result = await dispatcher.SendAsync(command);

        // Assert
        result.Should().Be(expectedResult);
        handlerMock.Verify(x => x.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueryAsync_ShouldExecuteQueryHandler()
    {
        // Arrange
        var expectedResult = new TestResult { Data = "query result" };
        var handlerMock = new Mock<IQueryHandler<TestQuery, TestResult>>();
        handlerMock.Setup(x => x.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResult);

        _services.AddSingleton(handlerMock.Object);
        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = new Dispatcher(serviceProvider, _loggerMock.Object);
        var query = new TestQuery { Filter = "test" };

        // Act
        var result = await dispatcher.QueryAsync(query);

        // Assert
        result.Should().Be(expectedResult);
        handlerMock.Verify(x => x.HandleAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithoutHandler_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = new Dispatcher(serviceProvider, _loggerMock.Object);
        var command = new UnhandledCommand();

        // Act
        var act = () => dispatcher.SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*UnhandledCommand*");
    }

    [Fact]
    public async Task QueryAsync_WithoutHandler_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = new Dispatcher(serviceProvider, _loggerMock.Object);
        var query = new UnhandledQuery();

        // Act
        var act = () => dispatcher.QueryAsync(query);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*UnhandledQuery*");
    }

    // Test classes
    public sealed class TestCommand : ICommand
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed class TestCommandWithResult : ICommand<TestResult>
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed class TestQuery : IQuery<TestResult>
    {
        public string Filter { get; set; } = string.Empty;
    }

    public sealed class TestResult
    {
        public string Data { get; set; } = string.Empty;
    }

    public sealed class UnhandledCommand : ICommand { }

    public sealed class UnhandledQuery : IQuery<TestResult> { }
}
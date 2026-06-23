using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Credentials.Handlers;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Tests.Commands.Credentials;

public class BulkVerifyCredentialsCommandHandlerTests
{
    private readonly Mock<ICommandHandler<VerifyCredentialCommand, VerificationResultDto>> _verifyHandlerMock;
    private readonly BulkVerifyCredentialsCommandHandler _handler;

    public BulkVerifyCredentialsCommandHandlerTests()
    {
        _verifyHandlerMock = new Mock<ICommandHandler<VerifyCredentialCommand, VerificationResultDto>>();
        var loggerMock = new Mock<ILogger<BulkVerifyCredentialsCommandHandler>>();
        _handler = new BulkVerifyCredentialsCommandHandler(_verifyHandlerMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithAllValid_ReturnsAllValid()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        _verifyHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<VerifyCredentialCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerificationResultDto { IsValid = true, VerifiedAt = DateTime.UtcNow });

        var command = new BulkVerifyCredentialsCommand(new List<Guid> { id1, id2 });

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
        result.Results.Should().OnlyContain(r => r.IsValid);
    }

    [Fact]
    public async Task HandleAsync_WithMixedValidity_ReportsPerId()
    {
        var validId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();

        _verifyHandlerMock
            .Setup(x => x.HandleAsync(It.Is<VerifyCredentialCommand>(c => c.CredentialId == validId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerificationResultDto { IsValid = true, VerifiedAt = DateTime.UtcNow });
        _verifyHandlerMock
            .Setup(x => x.HandleAsync(It.Is<VerifyCredentialCommand>(c => c.CredentialId == invalidId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerificationResultDto { IsValid = false, VerifiedAt = DateTime.UtcNow, ErrorMessage = "expired" });

        var command = new BulkVerifyCredentialsCommand(new List<Guid> { validId, invalidId });

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.Results.Should().Contain(r => r.CredentialId == validId && r.IsValid);
        result.Results.Should().Contain(r => r.CredentialId == invalidId && !r.IsValid && r.Error == "expired");
    }

    [Fact]
    public async Task HandleAsync_WhenVerifyThrows_RecordsErrorForThatId()
    {
        var id = Guid.NewGuid();
        _verifyHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<VerifyCredentialCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var command = new BulkVerifyCredentialsCommand(new List<Guid> { id });

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.TotalRequested.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
        result.Results.Should().ContainSingle().Which.IsValid.Should().BeFalse();
    }
}

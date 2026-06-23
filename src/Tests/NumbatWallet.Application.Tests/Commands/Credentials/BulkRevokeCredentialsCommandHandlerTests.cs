using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Credentials.Handlers;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Commands.Credentials;

public class BulkRevokeCredentialsCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<Application.Interfaces.IEventDispatcher> _eventDispatcherMock;
    private readonly BulkRevokeCredentialsCommandHandler _handler;

    public BulkRevokeCredentialsCommandHandlerTests()
    {
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _eventDispatcherMock = new Mock<Application.Interfaces.IEventDispatcher>();
        var loggerMock = new Mock<ILogger<BulkRevokeCredentialsCommandHandler>>();

        _handler = new BulkRevokeCredentialsCommandHandler(
            _credentialRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _eventDispatcherMock.Object,
            loggerMock.Object);
    }

    private static Credential CreateActiveCredential()
    {
        var credentialResult = Credential.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "MembershipCard",
            "{}",
            "schema:membershipcard:1.0");
        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();
        return credential;
    }

    [Fact]
    public async Task HandleAsync_WithAllValidCredentials_RevokesAll()
    {
        // Arrange
        var c1 = CreateActiveCredential();
        var c2 = CreateActiveCredential();
        var ids = new List<Guid> { c1.Id, c2.Id };

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(c1.Id, It.IsAny<CancellationToken>())).ReturnsAsync(c1);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(c2.Id, It.IsAny<CancellationToken>())).ReturnsAsync(c2);

        var command = new BulkRevokeCredentialsCommand(ids, "Compromised");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
        result.RevokedCredentialIds.Should().HaveCount(2);
        result.Errors.Should().BeEmpty();

        _credentialRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _eventDispatcherMock.Verify(x => x.DispatchAsync(It.IsAny<CredentialRevokedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithSomeFailures_RevokesValidOnesAndReportsErrors()
    {
        // Arrange
        var valid = CreateActiveCredential();
        var alreadyRevoked = CreateActiveCredential();
        alreadyRevoked.Revoke("Earlier");
        var notFoundId = Guid.NewGuid();
        var ids = new List<Guid> { valid.Id, alreadyRevoked.Id, notFoundId };

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(valid.Id, It.IsAny<CancellationToken>())).ReturnsAsync(valid);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(alreadyRevoked.Id, It.IsAny<CancellationToken>())).ReturnsAsync(alreadyRevoked);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(notFoundId, It.IsAny<CancellationToken>())).ReturnsAsync((Credential?)null);

        var command = new BulkRevokeCredentialsCommand(ids, "Bulk revoke");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.TotalRequested.Should().Be(3);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(2);
        result.RevokedCredentialIds.Should().ContainSingle().Which.Should().Be(valid.Id);
        result.Errors.Should().Contain(e => e.CredentialId == alreadyRevoked.Id);
        result.Errors.Should().Contain(e => e.CredentialId == notFoundId);
    }

    [Fact]
    public async Task HandleAsync_WhenAllFail_ReturnsAllErrors()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var ids = new List<Guid> { id1, id2 };

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Credential?)null);

        var command = new BulkRevokeCredentialsCommand(ids, "reason");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(2);
        result.RevokedCredentialIds.Should().BeEmpty();
        result.Errors.Should().HaveCount(2);
        _credentialRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Queries.Presentations;
using NumbatWallet.Application.Queries.Presentations.Handlers;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Tests.Queries.Presentations;

public class PresentationQueryHandlerTests
{
    private readonly Mock<IPresentationRepository> _presentationRepositoryMock = new();

    private static Presentation CreatePresentation(Guid? walletId = null)
    {
        var result = Presentation.Create(
            Guid.NewGuid(),
            walletId ?? Guid.NewGuid(),
            "verifier_123",
            "Age verification",
            """{"dateOfBirth":"1990-01-01","fullName":"John Doe"}""",
            DateTimeOffset.UtcNow.AddMinutes(15));
        return result.Value;
    }

    [Fact]
    public async Task GetPresentationById_ExistingPresentation_MapsSummaryWithClaimNamesOnly()
    {
        // Arrange
        var presentation = CreatePresentation();
        presentation.MarkVerified();
        _presentationRepositoryMock.Setup(x => x.GetByIdAsync(presentation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(presentation);

        var handler = new GetPresentationByIdQueryHandler(
            _presentationRepositoryMock.Object,
            new Mock<ILogger<GetPresentationByIdQueryHandler>>().Object);

        // Act
        var dto = await handler.HandleAsync(new GetPresentationByIdQuery(presentation.Id));

        // Assert
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(presentation.Id);
        dto.CredentialId.Should().Be(presentation.CredentialId);
        dto.WalletId.Should().Be(presentation.WalletId);
        dto.VerifierId.Should().Be("verifier_123");
        dto.Purpose.Should().Be("Age verification");
        dto.Status.Should().Be(PresentationStatus.Verified.ToString());
        dto.VerifiedAt.Should().NotBeNull();
        dto.VerificationCount.Should().Be(1);
        dto.DisclosedClaimNames.Should().BeEquivalentTo("dateOfBirth", "fullName");
    }

    [Fact]
    public async Task GetPresentationById_Missing_ReturnsNull()
    {
        // Arrange
        _presentationRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Presentation?)null);

        var handler = new GetPresentationByIdQueryHandler(
            _presentationRepositoryMock.Object,
            new Mock<ILogger<GetPresentationByIdQueryHandler>>().Object);

        // Act
        var dto = await handler.HandleAsync(new GetPresentationByIdQuery(Guid.NewGuid()));

        // Assert
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetPresentationsByWallet_ReturnsSummariesForWallet()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var presentations = new[] { CreatePresentation(walletId), CreatePresentation(walletId) };
        _presentationRepositoryMock.Setup(x => x.GetByWalletIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(presentations);

        var handler = new GetPresentationsByWalletQueryHandler(
            _presentationRepositoryMock.Object,
            new Mock<ILogger<GetPresentationsByWalletQueryHandler>>().Object);

        // Act
        var dtos = (await handler.HandleAsync(new GetPresentationsByWalletQuery(walletId))).ToList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos.Should().OnlyContain(d => d.WalletId == walletId);
        dtos.Should().OnlyContain(d => d.DisclosedClaimNames.Count == 2);
    }
}

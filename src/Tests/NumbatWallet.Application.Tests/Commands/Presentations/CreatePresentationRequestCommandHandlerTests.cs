using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Presentations;
using NumbatWallet.Application.Commands.Presentations.Handlers;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Commands.Presentations;

public class CreatePresentationRequestCommandHandlerTests
{
    private readonly Mock<IPresentationRequestRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly CreatePresentationRequestCommandHandler _handler;

    public CreatePresentationRequestCommandHandlerTests()
    {
        _handler = new CreatePresentationRequestCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _configurationMock.Object,
            new Mock<ILogger<CreatePresentationRequestCommandHandler>>().Object);
    }

    private static CreatePresentationRequestCommand ValidCommand() => new(
        VerifierId: "verifier_123",
        Purpose: "Age verification",
        RequestedCredentialType: "ProofOfAge",
        RequestedClaims: new List<string> { "dateOfBirth", "fullName" });

    [Fact]
    public async Task HandleAsync_ValidCommand_PersistsPendingRequestAndReturnsResult()
    {
        // Arrange
        PresentationRequest? persisted = null;
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<PresentationRequest>(), It.IsAny<CancellationToken>()))
            .Callback((PresentationRequest r, CancellationToken _) => persisted = r)
            .ReturnsAsync((PresentationRequest r, CancellationToken _) => r);

        // Act
        var result = await _handler.HandleAsync(ValidCommand());

        // Assert — persisted aggregate
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(PresentationRequestStatus.Pending);
        persisted.VerifierId.Should().Be("verifier_123");
        persisted.RequestedCredentialType.Should().Be("ProofOfAge");
        JsonSerializer.Deserialize<List<string>>(persisted.RequestedClaimsJson)
            .Should().BeEquivalentTo("dateOfBirth", "fullName");

        // Result mirrors the persisted request
        result.RequestId.Should().Be(persisted.Id);
        result.Nonce.Should().NotBeNullOrWhiteSpace();
        result.Nonce.Should().Be(persisted.Nonce);
        result.ExpiresAt.Should().Be(persisted.ExpiresAt);
        result.RequestUri.Should().EndWith($"/{persisted.Id}");
        result.PresentationDefinition.Should().Be(persisted.PresentationDefinitionJson);

        // Default lifetime 30 minutes when unconfigured
        result.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_BuildsDifPresentationExchangeV2Definition()
    {
        // Act
        var result = await _handler.HandleAsync(ValidCommand());

        // Assert — DIF PE v2 shape constraining the Phase-1 VC-JWT payload
        var definition = JsonSerializer.Deserialize<JsonElement>(result.PresentationDefinition);
        definition.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        definition.GetProperty("format").GetProperty("jwt_vp").GetProperty("alg")
            .EnumerateArray().Select(e => e.GetString()).Should().Contain("RS256");

        var descriptors = definition.GetProperty("input_descriptors");
        descriptors.GetArrayLength().Should().Be(1);
        var descriptor = descriptors[0];
        descriptor.GetProperty("id").GetString().Should().Be("ProofOfAge");

        var fields = descriptor.GetProperty("constraints").GetProperty("fields");
        var paths = fields.EnumerateArray()
            .SelectMany(f => f.GetProperty("path").EnumerateArray())
            .Select(p => p.GetString())
            .ToList();
        paths.Should().Contain("$.vc.type");
        paths.Should().Contain("$.vc.credentialSubject.dateOfBirth");
        paths.Should().Contain("$.vc.credentialSubject.fullName");

        // The type field constrains the array to contain the requested credential type.
        var typeField = fields.EnumerateArray().First(f =>
            f.GetProperty("path")[0].GetString() == "$.vc.type");
        typeField.GetProperty("filter").GetProperty("contains").GetProperty("const")
            .GetString().Should().Be("ProofOfAge");
    }

    [Fact]
    public async Task HandleAsync_ConfiguredLifetimeAndBaseUrl_AreUsed()
    {
        // Arrange
        _configurationMock.Setup(c => c["Presentation:RequestLifetimeMinutes"]).Returns("5");
        _configurationMock.Setup(c => c["Presentation:RequestBaseUrl"]).Returns("https://verify.example.com/req/");

        // Act
        var result = await _handler.HandleAsync(ValidCommand());

        // Assert
        result.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromMinutes(1));
        result.RequestUri.Should().Be($"https://verify.example.com/req/{result.RequestId}");
    }

    [Fact]
    public async Task HandleAsync_NoRequestedClaims_Throws()
    {
        var command = ValidCommand() with { RequestedClaims = new List<string>() };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.HandleAsync(command));
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<PresentationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_BlankClaimName_Throws()
    {
        var command = ValidCommand() with { RequestedClaims = new List<string> { "dateOfBirth", " " } };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_BlankVerifierId_Throws()
    {
        var command = ValidCommand() with { VerifierId = " " };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.HandleAsync(command));
    }
}

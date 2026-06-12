using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Commands.Presentations;
using NumbatWallet.Application.Commands.Presentations.Handlers;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Commands.Presentations;

public class SubmitPresentationCommandHandlerTests
{
    private const string RequestNonce = "request-nonce-42";
    private const string VerifierId = "verifier_123";

    private readonly Mock<IPresentationRequestRepository> _repositoryMock = new();
    private readonly Mock<ICommandHandler<VerifyPresentationCommand, PresentationVerificationResultDto>> _verifyHandlerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly SubmitPresentationCommandHandler _handler;

    public SubmitPresentationCommandHandlerTests()
    {
        _handler = new SubmitPresentationCommandHandler(
            _repositoryMock.Object,
            _verifyHandlerMock.Object,
            _unitOfWorkMock.Object,
            new Mock<ILogger<SubmitPresentationCommandHandler>>().Object);
    }

    private PresentationRequest SetupPendingRequest(
        string requestedType = "ProofOfAge",
        string requestedClaimsJson = "[\"dateOfBirth\"]",
        DateTimeOffset? expiresAt = null)
    {
        var request = PresentationRequest.Create(
            VerifierId,
            "Age verification",
            requestedType,
            requestedClaimsJson,
            "{\"id\":\"def-1\",\"input_descriptors\":[]}",
            RequestNonce,
            expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30)).Value;

        _repositoryMock.Setup(x => x.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        return request;
    }

    /// <summary>A well-formed (signed, but signature is irrelevant here) VP-JWT carrying nonce + aud.</summary>
    private static string MintVpToken(string nonce = RequestNonce, string audience = VerifierId)
    {
        var token = new JwtSecurityToken(
            issuer: $"urn:uuid:{Guid.NewGuid()}",
            audience: audience,
            claims: new[] { new Claim("nonce", nonce) },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("AnyTestSigningKeyThatIs256BitsLong!!!!!!!!!!")),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetupVerificationResult(PresentationVerificationResultDto result)
    {
        _verifyHandlerMock.Setup(x => x.HandleAsync(It.IsAny<VerifyPresentationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private static PresentationVerificationResultDto ValidVerification(
        string credentialType = "ProofOfAge",
        Dictionary<string, object>? disclosedClaims = null,
        Guid? presentationId = null) => new()
    {
        IsValid = true,
        PresentationId = presentationId ?? Guid.NewGuid(),
        CredentialId = Guid.NewGuid(),
        CredentialType = credentialType,
        DisclosedClaims = disclosedClaims ?? new Dictionary<string, object> { { "dateOfBirth", "1990-01-01" } },
        PresentedAt = DateTimeOffset.UtcNow,
        VerifiedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task HandleAsync_ValidSubmission_FulfilsRequestOneShot()
    {
        // Arrange
        var request = SetupPendingRequest();
        var presentationId = Guid.NewGuid();
        SetupVerificationResult(ValidVerification(presentationId: presentationId));

        // Act
        var result = await _handler.HandleAsync(new SubmitPresentationCommand(request.Id, MintVpToken()));

        // Assert
        result.IsValid.Should().BeTrue();
        request.Status.Should().Be(PresentationRequestStatus.Fulfilled);
        request.FulfilledByPresentationId.Should().Be(presentationId);
        request.FulfilledAt.Should().NotBeNull();

        _repositoryMock.Verify(x => x.UpdateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // The inner verification ran with the request's verifier for auditing.
        _verifyHandlerMock.Verify(x => x.HandleAsync(
            It.Is<VerifyPresentationCommand>(c => c.VerifierId == VerifierId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Replay_IsRejected()
    {
        // Arrange — first submission fulfils, second must be rejected WITHOUT re-verifying
        var request = SetupPendingRequest();
        SetupVerificationResult(ValidVerification());
        var token = MintVpToken();

        var first = await _handler.HandleAsync(new SubmitPresentationCommand(request.Id, token));
        first.IsValid.Should().BeTrue();

        // Act — replay
        var replay = await _handler.HandleAsync(new SubmitPresentationCommand(request.Id, token));

        // Assert
        replay.IsValid.Should().BeFalse();
        replay.Reason.Should().Contain("already been fulfilled");
        _verifyHandlerMock.Verify(x => x.HandleAsync(
            It.IsAny<VerifyPresentationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UnknownRequest_ReturnsInvalid()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresentationRequest?)null);

        var result = await _handler.HandleAsync(new SubmitPresentationCommand(Guid.NewGuid(), MintVpToken()));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("not found");
        _verifyHandlerMock.Verify(x => x.HandleAsync(
            It.IsAny<VerifyPresentationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExpiredRequest_ReturnsInvalidAndMarksExpired()
    {
        // Arrange — request lifetime elapsed
        var request = SetupPendingRequest(expiresAt: DateTimeOffset.UtcNow.AddMilliseconds(20));
        await Task.Delay(50);

        // Act
        var result = await _handler.HandleAsync(new SubmitPresentationCommand(request.Id, MintVpToken()));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("expired");
        request.Status.Should().Be(PresentationRequestStatus.Expired);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _verifyHandlerMock.Verify(x => x.HandleAsync(
            It.IsAny<VerifyPresentationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidVp_ReturnsInnerFailureAndStaysPending()
    {
        // Arrange — Phase-1 verification fails (e.g. revoked credential)
        var request = SetupPendingRequest();
        SetupVerificationResult(new PresentationVerificationResultDto
        {
            IsValid = false,
            Reason = "Credential has been revoked."
        });

        // Act
        var result = await _handler.HandleAsync(new SubmitPresentationCommand(request.Id, MintVpToken()));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("revoked");
        request.Status.Should().Be(PresentationRequestStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_NonceMismatch_ReturnsInvalid()
    {
        // Arrange — VP carries a different nonce than the request issued
        var request = SetupPendingRequest();
        SetupVerificationResult(ValidVerification());

        // Act
        var result = await _handler.HandleAsync(
            new SubmitPresentationCommand(request.Id, MintVpToken(nonce: "some-other-nonce")));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("nonce");
        request.Status.Should().Be(PresentationRequestStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_AudienceMismatch_ReturnsInvalid()
    {
        var request = SetupPendingRequest();
        SetupVerificationResult(ValidVerification());

        var result = await _handler.HandleAsync(
            new SubmitPresentationCommand(request.Id, MintVpToken(audience: "someone_else")));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("verifier");
        request.Status.Should().Be(PresentationRequestStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_WrongCredentialType_ReturnsInvalid()
    {
        // Arrange — request wants ProofOfAge, holder presented a DriversLicense
        var request = SetupPendingRequest(requestedType: "ProofOfAge");
        SetupVerificationResult(ValidVerification(credentialType: "DriversLicense"));

        // Act
        var result = await _handler.HandleAsync(new SubmitPresentationCommand(request.Id, MintVpToken()));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("does not satisfy");
        request.Status.Should().Be(PresentationRequestStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_MissingRequestedClaim_ReturnsInvalid()
    {
        // Arrange — request requires dateOfBirth AND fullName; only dateOfBirth disclosed
        var request = SetupPendingRequest(requestedClaimsJson: "[\"dateOfBirth\",\"fullName\"]");
        SetupVerificationResult(ValidVerification(
            disclosedClaims: new Dictionary<string, object> { { "dateOfBirth", "1990-01-01" } }));

        // Act
        var result = await _handler.HandleAsync(new SubmitPresentationCommand(request.Id, MintVpToken()));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("fullName");
        request.Status.Should().Be(PresentationRequestStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_EmptyToken_ReturnsInvalid()
    {
        var result = await _handler.HandleAsync(new SubmitPresentationCommand(Guid.NewGuid(), " "));

        result.IsValid.Should().BeFalse();
        _repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmptyRequestId_ReturnsInvalid()
    {
        var result = await _handler.HandleAsync(new SubmitPresentationCommand(Guid.Empty, MintVpToken()));

        result.IsValid.Should().BeFalse();
    }
}

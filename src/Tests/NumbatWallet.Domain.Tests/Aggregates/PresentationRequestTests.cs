using FluentAssertions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using Xunit;

namespace NumbatWallet.Domain.Tests.Aggregates;

public class PresentationRequestTests
{
    private static PresentationRequest CreateValid(DateTimeOffset? expiresAt = null)
    {
        var result = PresentationRequest.Create(
            "verifier_123",
            "Age verification",
            "ProofOfAge",
            "[\"dateOfBirth\"]",
            "{\"id\":\"def-1\",\"input_descriptors\":[]}",
            "nonce-abc",
            expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30));
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public void Create_WithValidInputs_StartsPendingWithGeneratedId()
    {
        var request = CreateValid();

        request.Id.Should().NotBeEmpty();
        request.Status.Should().Be(PresentationRequestStatus.Pending);
        request.VerifierId.Should().Be("verifier_123");
        request.Purpose.Should().Be("Age verification");
        request.RequestedCredentialType.Should().Be("ProofOfAge");
        request.RequestedClaimsJson.Should().Be("[\"dateOfBirth\"]");
        request.Nonce.Should().Be("nonce-abc");
        request.FulfilledAt.Should().BeNull();
        request.FulfilledByPresentationId.Should().BeNull();
        request.IsExpired.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "purpose", "Type", "[\"a\"]", "{}", "n")]
    [InlineData("v", "", "Type", "[\"a\"]", "{}", "n")]
    [InlineData("v", "purpose", "", "[\"a\"]", "{}", "n")]
    [InlineData("v", "purpose", "Type", "", "{}", "n")]
    [InlineData("v", "purpose", "Type", "[\"a\"]", "", "n")]
    [InlineData("v", "purpose", "Type", "[\"a\"]", "{}", "")]
    public void Create_WithMissingRequiredField_Fails(
        string verifierId, string purpose, string type, string claims, string definition, string nonce)
    {
        var result = PresentationRequest.Create(
            verifierId, purpose, type, claims, definition, nonce,
            DateTimeOffset.UtcNow.AddMinutes(30));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithPastExpiry_Fails()
    {
        var result = PresentationRequest.Create(
            "v", "p", "Type", "[\"a\"]", "{}", "n",
            DateTimeOffset.UtcNow.AddMinutes(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith("PresentationRequest.InvalidExpiry");
    }

    [Fact]
    public void MarkFulfilled_PendingRequest_Succeeds()
    {
        var request = CreateValid();
        var presentationId = Guid.NewGuid();

        var result = request.MarkFulfilled(presentationId);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(PresentationRequestStatus.Fulfilled);
        request.FulfilledAt.Should().NotBeNull();
        request.FulfilledByPresentationId.Should().Be(presentationId);
    }

    [Fact]
    public void MarkFulfilled_Twice_SecondIsRejected()
    {
        // One-shot: a replayed submission must be rejected.
        var request = CreateValid();
        request.MarkFulfilled(Guid.NewGuid()).IsSuccess.Should().BeTrue();
        var firstFulfilledBy = request.FulfilledByPresentationId;

        var replay = request.MarkFulfilled(Guid.NewGuid());

        replay.IsFailure.Should().BeTrue();
        replay.Error.Code.Should().EndWith("PresentationRequest.AlreadyFulfilled");
        request.FulfilledByPresentationId.Should().Be(firstFulfilledBy);
    }

    [Fact]
    public void MarkFulfilled_ExpiredRequest_IsRejected()
    {
        var request = CreateValid(expiresAt: DateTimeOffset.UtcNow.AddMilliseconds(20));
        Thread.Sleep(50);

        var result = request.MarkFulfilled(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith("PresentationRequest.Expired");
    }

    [Fact]
    public void MarkFulfilled_EmptyPresentationId_IsRejected()
    {
        var request = CreateValid();

        var result = request.MarkFulfilled(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        request.Status.Should().Be(PresentationRequestStatus.Pending);
    }

    [Fact]
    public void MarkExpired_PendingRequest_Expires()
    {
        var request = CreateValid();

        request.MarkExpired().IsSuccess.Should().BeTrue();

        request.Status.Should().Be(PresentationRequestStatus.Expired);
    }

    [Fact]
    public void MarkExpired_FulfilledRequest_IsRejected()
    {
        var request = CreateValid();
        request.MarkFulfilled(Guid.NewGuid());

        var result = request.MarkExpired();

        result.IsFailure.Should().BeTrue();
        request.Status.Should().Be(PresentationRequestStatus.Fulfilled);
    }

    [Fact]
    public void SetTenantId_AssignsTenant()
    {
        var request = CreateValid();

        request.SetTenantId("tenant-1");

        request.TenantId.Should().Be("tenant-1");
    }
}

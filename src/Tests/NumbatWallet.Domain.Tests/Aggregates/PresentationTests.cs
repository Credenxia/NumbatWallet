using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Tests.Aggregates;

public class PresentationTests
{
    private static Presentation CreateTestPresentation()
    {
        var result = Presentation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "verifier_123",
            "Age verification",
            """{"dateOfBirth":"1990-01-01"}""",
            DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public void Presentation_Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var verifierId = "verifier_abc";
        var purpose = "Proof of age";
        var disclosedClaimsJson = """{"dateOfBirth":"1990-01-01","fullName":"John Doe"}""";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        // Act
        var result = Presentation.Create(
            credentialId, walletId, verifierId, purpose, disclosedClaimsJson, expiresAt);

        // Assert
        Assert.True(result.IsSuccess);
        var presentation = result.Value;
        Assert.NotEqual(Guid.Empty, presentation.Id);
        Assert.Equal(credentialId, presentation.CredentialId);
        Assert.Equal(walletId, presentation.WalletId);
        Assert.Equal(verifierId, presentation.VerifierId);
        Assert.Equal(purpose, presentation.Purpose);
        Assert.Equal(disclosedClaimsJson, presentation.DisclosedClaimsJson);
        Assert.Equal(PresentationStatus.Pending, presentation.Status);
        Assert.Equal(expiresAt, presentation.ExpiresAt);
        Assert.NotEqual(default, presentation.PresentedAt);
        Assert.Null(presentation.VerifiedAt);
        Assert.Equal(0, presentation.VerificationCount);
        Assert.False(presentation.IsExpired);
    }

    [Fact]
    public void Presentation_Create_WithEmptyCredentialId_ShouldFail()
    {
        var result = Presentation.Create(
            Guid.Empty, Guid.NewGuid(), "verifier", "purpose", "{}",
            DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Presentation_Create_WithEmptyWalletId_ShouldFail()
    {
        var result = Presentation.Create(
            Guid.NewGuid(), Guid.Empty, "verifier", "purpose", "{}",
            DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Presentation_Create_WithEmptyVerifierId_ShouldFail()
    {
        var result = Presentation.Create(
            Guid.NewGuid(), Guid.NewGuid(), "  ", "purpose", "{}",
            DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Presentation_Create_WithEmptyPurpose_ShouldFail()
    {
        var result = Presentation.Create(
            Guid.NewGuid(), Guid.NewGuid(), "verifier", "", "{}",
            DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Presentation_Create_WithNullDisclosedClaims_ShouldFail()
    {
        var result = Presentation.Create(
            Guid.NewGuid(), Guid.NewGuid(), "verifier", "purpose", null!,
            DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Presentation_Create_WithPastExpiry_ShouldFail()
    {
        var result = Presentation.Create(
            Guid.NewGuid(), Guid.NewGuid(), "verifier", "purpose", "{}",
            DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.True(result.IsFailure);
        Assert.EndsWith("Presentation.InvalidExpiry", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void Presentation_SetTenantId_ShouldSetTenant()
    {
        var presentation = CreateTestPresentation();

        presentation.SetTenantId("tenant-1");

        Assert.Equal("tenant-1", presentation.TenantId);
    }

    [Fact]
    public void Presentation_SetTenantId_WithEmptyValue_ShouldThrow()
    {
        var presentation = CreateTestPresentation();

        Assert.Throws<ArgumentException>(() => presentation.SetTenantId(" "));
    }

    [Fact]
    public void Presentation_MarkVerified_ShouldSetVerifiedStateAndCount()
    {
        var presentation = CreateTestPresentation();

        var result = presentation.MarkVerified();

        Assert.True(result.IsSuccess);
        Assert.Equal(PresentationStatus.Verified, presentation.Status);
        Assert.NotNull(presentation.VerifiedAt);
        Assert.Equal(1, presentation.VerificationCount);
    }

    [Fact]
    public void Presentation_MarkVerified_Twice_ShouldAllowReVerifyAndIncrementCount()
    {
        var presentation = CreateTestPresentation();

        var first = presentation.MarkVerified();
        var firstVerifiedAt = presentation.VerifiedAt;
        var second = presentation.MarkVerified();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(PresentationStatus.Verified, presentation.Status);
        Assert.Equal(2, presentation.VerificationCount);
        Assert.True(presentation.VerifiedAt >= firstVerifiedAt);
    }

    [Fact]
    public void Presentation_MarkVerified_WhenRejected_ShouldFail()
    {
        var presentation = CreateTestPresentation();
        presentation.Reject();

        var result = presentation.MarkVerified();

        Assert.True(result.IsFailure);
        Assert.Equal(PresentationStatus.Rejected, presentation.Status);
        Assert.Equal(0, presentation.VerificationCount);
    }

    [Fact]
    public void Presentation_MarkExpired_ShouldSetExpiredStatus()
    {
        var presentation = CreateTestPresentation();

        var result = presentation.MarkExpired();

        Assert.True(result.IsSuccess);
        Assert.Equal(PresentationStatus.Expired, presentation.Status);
    }

    [Fact]
    public void Presentation_MarkVerified_WhenMarkedExpired_ShouldFail()
    {
        var presentation = CreateTestPresentation();
        presentation.MarkExpired();

        var result = presentation.MarkVerified();

        Assert.True(result.IsFailure);
        Assert.EndsWith("Presentation.Expired", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void Presentation_Reject_ShouldSetRejectedStatus()
    {
        var presentation = CreateTestPresentation();

        var result = presentation.Reject();

        Assert.True(result.IsSuccess);
        Assert.Equal(PresentationStatus.Rejected, presentation.Status);
    }

    [Fact]
    public void Presentation_Reject_WhenAlreadyRejected_ShouldFail()
    {
        var presentation = CreateTestPresentation();
        presentation.Reject();

        var result = presentation.Reject();

        Assert.True(result.IsFailure);
    }
}

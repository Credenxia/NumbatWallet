using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Web.Api.Authentication;
using System.Security.Claims;
using Xunit;

namespace NumbatWallet.Web.Api.Tests.Authentication;

public class CredentryPersonEnrichmentTests
{
    private static readonly Guid PersonId = Guid.Parse("d9453dad-0000-0000-0000-00000000abcd");

    private static ClaimsIdentity CreateIdentity(params Claim[] claims) =>
        new(claims, authenticationType: "CredentryJwt");

    private static Mock<IPersonService> PersonServiceReturning(PersonDto? person)
    {
        var mock = new Mock<IPersonService>();
        mock.Setup(s => s.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        return mock;
    }

    [Fact]
    public async Task TryEnrichPersonIdentity_PersonMatchesEmail_AddsPersonIdAndNameIdentifier()
    {
        var identity = CreateIdentity(new Claim("sub", "credentry-user-id"));
        var personService = PersonServiceReturning(new PersonDto { Id = PersonId });

        var result = await CredentryClaimsMapper.TryEnrichPersonIdentityAsync(
            identity, "citizen@example.com", personService.Object, NullLogger.Instance);

        result.Should().BeTrue();
        identity.FindFirst("person_id")!.Value.Should().Be(PersonId.ToString("D"));
        identity.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(PersonId.ToString("D"));
        // HONESTY: the raw Credentry subject must stay untouched.
        identity.FindFirst("sub")!.Value.Should().Be("credentry-user-id");
        personService.Verify(s => s.GetByEmailAsync("citizen@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryEnrichPersonIdentity_NoMatchingPerson_FailsSoftWithoutClaims()
    {
        var identity = CreateIdentity(new Claim("sub", "credentry-user-id"));
        var personService = PersonServiceReturning(null);

        var result = await CredentryClaimsMapper.TryEnrichPersonIdentityAsync(
            identity, "stranger@example.com", personService.Object, NullLogger.Instance);

        result.Should().BeFalse();
        identity.FindFirst("person_id").Should().BeNull();
        identity.FindFirst(ClaimTypes.NameIdentifier).Should().BeNull();
    }

    [Fact]
    public async Task TryEnrichPersonIdentity_LookupThrows_FailsSoftNotAuthGate()
    {
        var identity = CreateIdentity();
        var personService = new Mock<IPersonService>();
        personService.Setup(s => s.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("search token store unavailable"));

        var result = await CredentryClaimsMapper.TryEnrichPersonIdentityAsync(
            identity, "citizen@example.com", personService.Object, NullLogger.Instance);

        result.Should().BeFalse();
        identity.FindFirst("person_id").Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryEnrichPersonIdentity_NoEmailClaim_SkipsLookup(string? email)
    {
        // M2M tokens carry no email — they act like the API-key service principal.
        var identity = CreateIdentity(new Claim("sub", "numbatwallet-m2m"));
        var personService = PersonServiceReturning(new PersonDto { Id = PersonId });

        var result = await CredentryClaimsMapper.TryEnrichPersonIdentityAsync(
            identity, email, personService.Object, NullLogger.Instance);

        result.Should().BeFalse();
        personService.Verify(s => s.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryEnrichPersonIdentity_ExistingNameIdentifier_IsNotOverwritten()
    {
        var identity = CreateIdentity(new Claim(ClaimTypes.NameIdentifier, "pre-existing"));
        var personService = PersonServiceReturning(new PersonDto { Id = PersonId });

        var result = await CredentryClaimsMapper.TryEnrichPersonIdentityAsync(
            identity, "citizen@example.com", personService.Object, NullLogger.Instance);

        result.Should().BeTrue();
        identity.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("pre-existing");
        identity.FindFirst("person_id")!.Value.Should().Be(PersonId.ToString("D"));
    }
}

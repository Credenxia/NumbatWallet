using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Commands.Authentication;
using NumbatWallet.Application.Commands.Authentication.Handlers;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using Xunit;
using PersonAggregate = NumbatWallet.Domain.Aggregates.Person;

namespace NumbatWallet.Application.Tests.Commands.Authentication;

/// <summary>
/// Refresh must re-issue tokens under the SAME claim contract as login
/// (person Guid in NameIdentifier + "sub", tenant_id preserved, roles preserved) —
/// otherwise a refreshed citizen token silently loses wallet/tenant access.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private const string TenantId = "00000000-0000-0000-0000-000000000001";
    private const string ValidRefreshToken = "valid-refresh-token";

    private readonly Mock<IPersonRepository> _personRepository = new();
    private readonly Mock<IRefreshTokenStore> _refreshTokenStore = new();
    private readonly Mock<IAccessTokenSigner> _accessTokenSigner = new();
    private readonly PersonAggregate _citizen;
    private readonly List<Claim> _issuedClaims = new();

    public RefreshTokenCommandHandlerTests()
    {
        _citizen = new PersonAggregate(
            firstName: "Test",
            lastName: "Citizen",
            dateOfBirth: new DateOnly(1990, 1, 1),
            email: "citizen@example.com",
            externalId: "TEST-CITIZEN-001",
            tenantId: TenantId);

        _refreshTokenStore
            .Setup(s => s.Get(ValidRefreshToken))
            .Returns(new RefreshTokenData(_citizen.Id.ToString(), new[] { "Citizen", "User" }));

        _personRepository
            .Setup(r => r.GetByIdAsync(_citizen.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_citizen);

        _accessTokenSigner
            .Setup(s => s.CreateToken(It.IsAny<IEnumerable<Claim>>(), It.IsAny<DateTimeOffset>()))
            .Callback<IEnumerable<Claim>, DateTimeOffset>((claims, _) => _issuedClaims.AddRange(claims))
            .Returns("new-signed-token");
    }

    private RefreshTokenCommandHandler CreateHandler() => new(
        _personRepository.Object,
        _refreshTokenStore.Object,
        _accessTokenSigner.Object,
        Mock.Of<ILogger<RefreshTokenCommandHandler>>());

    [Fact]
    public async Task HandleAsync_ValidToken_ReissuesPersonGuidSubjectClaims()
    {
        await CreateHandler().HandleAsync(new RefreshTokenCommand(ValidRefreshToken));

        _issuedClaims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value
            .Should().Be(_citizen.Id.ToString());
        _issuedClaims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value
            .Should().Be(_citizen.Id.ToString(),
                "a refreshed token must keep the person Guid subject so wallet APIs still resolve the caller");
    }

    [Fact]
    public async Task HandleAsync_ValidToken_PreservesTenantClaim()
    {
        await CreateHandler().HandleAsync(new RefreshTokenCommand(ValidRefreshToken));

        _issuedClaims.SingleOrDefault(c => c.Type == "tenant_id")?.Value.Should().Be(TenantId,
            "losing tenant_id on refresh would break every tenant-scoped query for the citizen");
    }

    [Fact]
    public async Task HandleAsync_ValidToken_PreservesRoles()
    {
        await CreateHandler().HandleAsync(new RefreshTokenCommand(ValidRefreshToken));

        _issuedClaims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().Contain(new[] { "Citizen", "User" });
    }

    [Fact]
    public async Task HandleAsync_ValidToken_RotatesRefreshToken()
    {
        var result = await CreateHandler().HandleAsync(new RefreshTokenCommand(ValidRefreshToken));

        _refreshTokenStore.Verify(s => s.Revoke(ValidRefreshToken), Times.Once);
        _refreshTokenStore.Verify(s => s.Store(
            result.RefreshToken!,
            _citizen.Id.ToString(),
            It.Is<IReadOnlyList<string>>(r => r.Contains("Citizen")),
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_Throws()
    {
        var act = () => CreateHandler().HandleAsync(new RefreshTokenCommand("unknown-token"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}

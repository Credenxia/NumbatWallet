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
/// Login claim contract tests. The wallet APIs (REST GetMyWallets, GraphQL myWallets,
/// IDOR ownership checks) resolve the caller's PERSON by parsing the subject claims as a
/// Guid — so the access token MUST carry the person's Guid in both ClaimTypes.NameIdentifier
/// and the OIDC "sub" claim, the person's tenant in "tenant_id", and the validator-assigned
/// roles. These tests pin that contract.
/// </summary>
public class LoginCommandHandlerTests
{
    private const string TenantId = "00000000-0000-0000-0000-000000000001";
    private const string CitizenEmail = "citizen@example.com";

    private readonly Mock<IPersonRepository> _personRepository = new();
    private readonly Mock<IPasswordValidator> _passwordValidator = new();
    private readonly Mock<IRefreshTokenStore> _refreshTokenStore = new();
    private readonly Mock<IAccessTokenSigner> _accessTokenSigner = new();
    private readonly PersonAggregate _citizen;

    public LoginCommandHandlerTests()
    {
        _citizen = new PersonAggregate(
            firstName: "Test",
            lastName: "Citizen",
            dateOfBirth: new DateOnly(1990, 1, 1),
            email: CitizenEmail,
            externalId: "TEST-CITIZEN-001",
            tenantId: TenantId);

        _personRepository
            .Setup(r => r.GetByEmailAsync(CitizenEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_citizen);

        _passwordValidator.Setup(v => v.SupportsEmail(CitizenEmail)).Returns(true);
        _passwordValidator
            .Setup(v => v.ValidateAsync(CitizenEmail, "correct-password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "Citizen", "User" });
        _passwordValidator
            .Setup(v => v.ValidateAsync(CitizenEmail, It.Is<string>(p => p != "correct-password"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        _accessTokenSigner
            .Setup(s => s.CreateToken(It.IsAny<IEnumerable<Claim>>(), It.IsAny<DateTimeOffset>()))
            .Returns("signed-token");
    }

    private LoginCommandHandler CreateHandler() => new(
        _personRepository.Object,
        new[] { _passwordValidator.Object },
        _refreshTokenStore.Object,
        _accessTokenSigner.Object,
        Mock.Of<ILogger<LoginCommandHandler>>());

    private List<Claim> CaptureIssuedClaims()
    {
        List<Claim> captured = new();
        _accessTokenSigner
            .Setup(s => s.CreateToken(It.IsAny<IEnumerable<Claim>>(), It.IsAny<DateTimeOffset>()))
            .Callback<IEnumerable<Claim>, DateTimeOffset>((claims, _) => captured.AddRange(claims))
            .Returns("signed-token");
        return captured;
    }

    [Fact]
    public async Task HandleAsync_CitizenLogin_NameIdentifierClaimIsThePersonGuid()
    {
        var claims = CaptureIssuedClaims();

        await CreateHandler().HandleAsync(new LoginCommand(CitizenEmail, "correct-password"));

        var nameIdentifier = claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        nameIdentifier.Should().NotBeNull();
        nameIdentifier!.Value.Should().Be(_citizen.Id.ToString(),
            "REST GetMyWallets parses ClaimTypes.NameIdentifier as the caller's person Guid");
    }

    [Fact]
    public async Task HandleAsync_CitizenLogin_SubClaimIsThePersonGuidNotTheEmail()
    {
        var claims = CaptureIssuedClaims();

        await CreateHandler().HandleAsync(new LoginCommand(CitizenEmail, "correct-password"));

        var sub = claims.SingleOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        sub.Should().NotBeNull();
        Guid.TryParse(sub!.Value, out var subGuid).Should().BeTrue(
            "GraphQL myWallets and the wallet/credential ownership handlers resolve the caller's person from 'sub'");
        subGuid.Should().Be(_citizen.Id);
    }

    [Fact]
    public async Task HandleAsync_CitizenLogin_TenantClaimIsThePersonsTenant()
    {
        var claims = CaptureIssuedClaims();

        await CreateHandler().HandleAsync(new LoginCommand(CitizenEmail, "correct-password"));

        claims.SingleOrDefault(c => c.Type == "tenant_id")?.Value.Should().Be(TenantId,
            "CurrentTenantService resolves the tenant from the validated tenant_id claim");
    }

    [Fact]
    public async Task HandleAsync_CitizenLogin_RoleClaimsIncludeValidatorAssignedRoles()
    {
        var claims = CaptureIssuedClaims();

        await CreateHandler().HandleAsync(new LoginCommand(CitizenEmail, "correct-password"));

        claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().Contain(new[] { "Citizen", "User" });
    }

    [Fact]
    public async Task HandleAsync_CitizenLogin_EmailClaimIsPreserved()
    {
        var claims = CaptureIssuedClaims();

        await CreateHandler().HandleAsync(new LoginCommand(CitizenEmail, "correct-password"));

        claims.SingleOrDefault(c => c.Type == ClaimTypes.Email)?.Value.Should().Be(CitizenEmail);
    }

    [Fact]
    public async Task HandleAsync_CitizenLogin_ReturnsBearerResultWithPersonId()
    {
        var result = await CreateHandler().HandleAsync(new LoginCommand(CitizenEmail, "correct-password"));

        result.TokenType.Should().Be("Bearer");
        result.UserId.Should().Be(_citizen.Id.ToString());
        result.Roles.Should().Contain("Citizen");
        result.Claims["TenantId"].Should().Be(TenantId);
        _refreshTokenStore.Verify(s => s.Store(
            result.RefreshToken!,
            _citizen.Id.ToString(),
            It.Is<IReadOnlyList<string>>(r => r.Contains("Citizen")),
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_Throws()
    {
        var act = () => CreateHandler().HandleAsync(new LoginCommand(CitizenEmail, "wrong-password"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        _accessTokenSigner.Verify(
            s => s.CreateToken(It.IsAny<IEnumerable<Claim>>(), It.IsAny<DateTimeOffset>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UnknownPerson_Throws()
    {
        var act = () => CreateHandler().HandleAsync(new LoginCommand("nobody@example.com", "x"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}

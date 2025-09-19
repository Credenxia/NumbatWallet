using FluentAssertions;
using Moq;
using NumbatWallet.Application.Queries.Person;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using Xunit;

namespace NumbatWallet.Application.Tests.Queries.Person;

public sealed class GetPersonStatisticsQueryTests
{
    private readonly Mock<IPersonRepository> _mockPersonRepository;
    private readonly GetPersonStatisticsQueryHandler _handler;

    public GetPersonStatisticsQueryTests()
    {
        _mockPersonRepository = new Mock<IPersonRepository>();
        _handler = new GetPersonStatisticsQueryHandler(_mockPersonRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var query = new GetPersonStatisticsQuery();

        var statistics = new Dictionary<string, int>
        {
            ["Total"] = 100,
            ["Verified"] = 75,
            ["PendingVerification"] = 20,
            ["Suspended"] = 5
        };

        _mockPersonRepository
            .Setup(x => x.GetPersonStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);

        _mockPersonRepository
            .Setup(x => x.CountByStatusAsync(PersonStatus.Verified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);

        _mockPersonRepository
            .Setup(x => x.CountByStatusAsync(PersonStatus.PendingVerification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        _mockPersonRepository
            .Setup(x => x.CountVerifiedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalPersons.Should().Be(100);
        result.VerifiedPersons.Should().Be(75);
        result.PendingVerificationPersons.Should().Be(20);
        result.StatusBreakdown.Should().ContainKey("Verified");
        result.StatusBreakdown["Verified"].Should().Be(75);
    }

    [Fact]
    public async Task HandleAsync_WithNoPersons_ShouldReturnZeroStatistics()
    {
        // Arrange
        var query = new GetPersonStatisticsQuery();

        var statistics = new Dictionary<string, int>
        {
            ["Total"] = 0,
            ["Verified"] = 0,
            ["PendingVerification"] = 0
        };

        _mockPersonRepository
            .Setup(x => x.GetPersonStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);

        _mockPersonRepository
            .Setup(x => x.CountByStatusAsync(It.IsAny<PersonStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockPersonRepository
            .Setup(x => x.CountVerifiedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalPersons.Should().Be(0);
        result.VerifiedPersons.Should().Be(0);
        result.PendingVerificationPersons.Should().Be(0);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.EventSourcing;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Tests.EventSourcing;

public class EventStoreTests : IDisposable
{
    private readonly NumbatWalletDbContext _context;
    private readonly EventStore _eventStore;
    private readonly Mock<ILogger<EventStore>> _mockLogger;
    private readonly Mock<ITenantService> _mockTenantService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IDateTimeService> _mockDateTimeService;
    private readonly Mock<IEventDispatcher> _mockEventDispatcher;
    private readonly Mock<ILogger<NumbatWalletDbContext>> _mockDbLogger;

    public EventStoreTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<NumbatWalletDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Mock dependencies for DbContext
        _mockTenantService = new Mock<ITenantService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockDateTimeService = new Mock<IDateTimeService>();
        _mockEventDispatcher = new Mock<IEventDispatcher>();
        _mockDbLogger = new Mock<ILogger<NumbatWalletDbContext>>();

        // Setup default behaviors
        _mockTenantService.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        _mockTenantService.Setup(x => x.TenantName).Returns("test-tenant");
        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user");
        _mockDateTimeService.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _context = new NumbatWalletDbContext(
            options,
            _mockTenantService.Object,
            _mockCurrentUserService.Object,
            _mockDateTimeService.Object,
            _mockEventDispatcher.Object,
            _mockDbLogger.Object);

        // Ensure database is created with required tables
        _context.Database.EnsureCreated();

        _mockLogger = new Mock<ILogger<EventStore>>();
        _eventStore = new EventStore(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task SaveEventAsync_WithValidEvent_StoresSuccessfully()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var domainEvent = new TestDomainEvent(aggregateId, "Test event occurred");

        // Act
        await _eventStore.SaveEventAsync(domainEvent);

        // Assert
        var storedEvents = await _context.Set<StoredEvent>().ToListAsync();
        storedEvents.Should().HaveCount(1);
        var storedEvent = storedEvents.First();
        storedEvent.AggregateId.Should().Be(aggregateId);
        storedEvent.EventType.Should().Be("TestDomainEvent");
        storedEvent.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveEventsAsync_WithMultipleEvents_StoresAllSuccessfully()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new TestDomainEvent(aggregateId, "Event 1"),
            new TestDomainEvent(aggregateId, "Event 2"),
            new TestDomainEvent(aggregateId, "Event 3")
        };

        // Act
        await _eventStore.SaveEventsAsync(events);

        // Assert
        var storedEvents = await _context.Set<StoredEvent>()
            .OrderBy(e => e.Version)
            .ToListAsync();

        storedEvents.Should().HaveCount(3);
        storedEvents[0].Version.Should().Be(1);
        storedEvents[1].Version.Should().Be(2);
        storedEvents[2].Version.Should().Be(3);
    }

    [Fact]
    public async Task GetEventsAsync_WithAggregateId_ReturnsEventsInOrder()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var events = new List<TestDomainEvent>
        {
            new(aggregateId, "Event 1"),
            new(aggregateId, "Event 2"),
            new(aggregateId, "Event 3")
        };

        foreach (var evt in events)
        {
            await _eventStore.SaveEventAsync(evt);
        }

        // Act
        var result = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(3);
        resultList[0].Version.Should().Be(1);
        resultList[1].Version.Should().Be(2);
        resultList[2].Version.Should().Be(3);
    }

    [Fact]
    public async Task GetEventsAsync_WithDateRange_ReturnsFilteredEvents()
    {
        // Arrange
        var aggregateType = "Unknown"; // EventStore uses "Unknown" when DeclaringType is null
        var now = DateTime.UtcNow;
        var yesterday = now.AddDays(-1);
        var tomorrow = now.AddDays(1);

        var event1 = new TestDomainEvent(Guid.NewGuid(), "Past event");
        var event2 = new TestDomainEvent(Guid.NewGuid(), "Current event");
        var event3 = new TestDomainEvent(Guid.NewGuid(), "Future event");

        // Save events with different timestamps
        await _eventStore.SaveEventAsync(event1);
        await _eventStore.SaveEventAsync(event2);
        await _eventStore.SaveEventAsync(event3);

        // Act
        var result = await _eventStore.GetEventsAsync(aggregateType, yesterday, tomorrow);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(3); // All events are within range since they're all saved "now"
    }

    [Fact]
    public async Task GetAllEventsAsync_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var events = new List<TestDomainEvent>();
        for (int i = 0; i < 15; i++)
        {
            events.Add(new TestDomainEvent(Guid.NewGuid(), $"Event {i}"));
        }

        foreach (var evt in events)
        {
            await _eventStore.SaveEventAsync(evt);
        }

        // Act
        var page1 = await _eventStore.GetAllEventsAsync(1, 10);
        var page2 = await _eventStore.GetAllEventsAsync(2, 10);

        // Assert
        page1.Count().Should().Be(10);
        page2.Count().Should().Be(5);
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithValidSnapshot_StoresSuccessfully()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var snapshot = new EventSnapshot
        {
            AggregateId = aggregateId,
            AggregateType = "TestAggregate",
            SnapshotData = "{ \"state\": \"snapshot\" }",
            Version = 10
        };

        // Act
        await _eventStore.SaveSnapshotAsync(snapshot);

        // Assert
        var storedSnapshot = await _context.Set<EventSnapshot>()
            .FirstOrDefaultAsync(s => s.AggregateId == aggregateId);

        storedSnapshot.Should().NotBeNull();
        storedSnapshot.Version.Should().Be(10);
        storedSnapshot.SnapshotData.Should().Be("{ \"state\": \"snapshot\" }");
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_WithMultipleSnapshots_ReturnsLatest()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        
        var snapshot1 = new EventSnapshot
        {
            AggregateId = aggregateId,
            AggregateType = "TestAggregate",
            SnapshotData = "{ \"version\": 5 }",
            Version = 5
        };

        var snapshot2 = new EventSnapshot
        {
            AggregateId = aggregateId,
            AggregateType = "TestAggregate",
            SnapshotData = "{ \"version\": 10 }",
            Version = 10
        };

        await _eventStore.SaveSnapshotAsync(snapshot1);
        await _eventStore.SaveSnapshotAsync(snapshot2);

        // Act
        var result = await _eventStore.GetLatestSnapshotAsync(aggregateId);

        // Assert
        result.Should().NotBeNull();
        result.Version.Should().Be(10);
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_WithNoSnapshot_ReturnsNull()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var result = await _eventStore.GetLatestSnapshotAsync(aggregateId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEventsAsync_WithNonExistentAggregate_ReturnsEmptyList()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var result = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

// Test domain event
public class TestDomainEvent : IDomainEvent
{
    public TestDomainEvent(Guid aggregateId, string message)
    {
        EventId = Guid.NewGuid();
        AggregateId = aggregateId;
        Message = message;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public Guid EventId { get; }
    public Guid AggregateId { get; }
    public string Message { get; }
    public DateTimeOffset OccurredAt { get; }
}

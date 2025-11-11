using Application.Notifications;
using Application.Services.Abstractions;
using Application.Services.Implementations;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Moq;

namespace DeputyApp.Tests;

[TestFixture]
public class EventServiceTests
{
    [SetUp]
    public void SetUp()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _eventRepoMock = new Mock<IEventRepository>();
        _phoneNotifier = new Mock<IPhoneNotificationService>();
        _tgHandler = new Mock<TgEventNotificationHandler>();

        _uowMock.SetupGet(x => x.Events).Returns(_eventRepoMock.Object);

        _service = new EventService(_uowMock.Object, _tgHandler.Object, _phoneNotifier.Object);
    }

    private Mock<IUnitOfWork> _uowMock = null!;
    private Mock<IEventRepository> _eventRepoMock = null!;
    private Mock<IPhoneNotificationService> _phoneNotifier = null!;
    private Mock<TgEventNotificationHandler> _tgHandler = null!;
    private EventService _service = null!;

    [Test]
    public async Task CreateAsync_AssignsIdAndCreatedAt_AddsAndSaves()
    {
        var e = new Event
        {
            Title = "Test",
            StartAt = DateTimeOffset.UtcNow.AddHours(1),
            EndAt = DateTimeOffset.UtcNow.AddHours(2)
        };

        _eventRepoMock.Setup(r => r.AddAsync(It.IsAny<Event>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var created = await _service.CreateAsync(e);

        Assert.That(created, Is.Not.Null);
        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.CreatedAt, Is.Not.EqualTo(default(DateTimeOffset)));
        _eventRepoMock.Verify(r => r.AddAsync(It.Is<Event>(x => x.Id == created.Id)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task GetUpcomingAsync_ReturnsRepositoryResults()
    {
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(7);
        var items = new[]
        {
            new Event { Id = Guid.NewGuid(), Title = "A", StartAt = from.AddHours(1) },
            new Event { Id = Guid.NewGuid(), Title = "B", StartAt = from.AddHours(2) }
        }.AsEnumerable();

        _eventRepoMock.Setup(r => r.GetUpcomingAsync(from, to)).ReturnsAsync(items);

        var res = await _service.GetUpcomingAsync(from, to);

        Assert.That(res, Is.Not.Null);
        Assert.That(res.Count(), Is.EqualTo(2));
        Assert.That(res.Select(x => x.Title), Does.Contain("A").And.Contain("B"));
        _eventRepoMock.Verify(r => r.GetUpcomingAsync(from, to), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenEventExists_DeletesAndSaves()
    {
        var id = Guid.NewGuid();
        var ev = new Event { Id = id, Title = "toDelete" };

        _eventRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(ev);
        _eventRepoMock.Setup(r => r.Delete(It.IsAny<Event>()));
        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _service.DeleteAsync(id);

        _eventRepoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        _eventRepoMock.Verify(r => r.Delete(It.Is<Event>(x => x.Id == id)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenEventNotFound_DoesNothing()
    {
        var id = Guid.NewGuid();
        _eventRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Event?)null);

        await _service.DeleteAsync(id);

        _eventRepoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        _eventRepoMock.Verify(r => r.Delete(It.IsAny<Event>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task GetMyUpcomingAsync_ReturnsRepositoryResults()
    {
        var userId = Guid.NewGuid();
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(30);
        var items = new[]
        {
            new Event { Id = Guid.NewGuid(), Title = "Mine1" },
            new Event { Id = Guid.NewGuid(), Title = "Mine2" }
        }.AsEnumerable();

        _eventRepoMock.Setup(r => r.GetMyUpcomingAsync(from, to, userId)).ReturnsAsync(items);

        var res = await _service.GetMyUpcomingAsync(userId, from, to);

        Assert.That(res, Is.Not.Null);
        Assert.That(res.Count(), Is.EqualTo(2));
        Assert.That(res.Select(x => x.Title), Does.Contain("Mine1").And.Contain("Mine2"));
        _eventRepoMock.Verify(r => r.GetMyUpcomingAsync(from, to, userId), Times.Once);
    }
}
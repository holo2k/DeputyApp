//using Application.Notifications;
//using Application.Services.Implementations;
//using DeputyApp.DAL.UnitOfWork;
//using Domain.Entities;
//using Infrastructure.DAL.Repository.Abstractions;
//using Moq;
//using System.Linq.Expressions;
//using Task = System.Threading.Tasks.Task;

//namespace DeputyApp.Tests;

//[TestFixture]
//public class PostServiceTests
//{
//    [SetUp]
//    public void SetUp()
//    {
//        _uowMock = new Mock<IUnitOfWork>();
//        _postRepoMock = new Mock<IPostRepository>();
//        _tgHandler = new Mock<TgEventNotificationHandler>();
//        _uowMock.SetupGet(x => x.Posts).Returns(_postRepoMock.Object);
//        _service = new PostService(_uowMock.Object, _tgHandler.Object);
//    }

//    private Mock<IUnitOfWork> _uowMock = null!;
//    private Mock<IPostRepository> _postRepoMock = null!;
//    private PostService _service = null!;
//    private Mock<TgEventNotificationHandler> _tgHandler = null!;

//    [Test]
//    public async Task GetByIdAsync_WhenExists_ReturnsPost()
//    {
//        var id = Guid.NewGuid();
//        var post = new Post { Id = id, Title = "P" };

//        _postRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<Expression<Func<Post, object>>[]>()))
//            .ReturnsAsync(post);

//        var res = await _service.GetByIdAsync(id);

//        Assert.That(res, Is.Not.Null);
//        Assert.That(res!.Id, Is.EqualTo(id));
//        _postRepoMock.Verify(r => r.GetByIdAsync(id, It.IsAny<Expression<Func<Post, object>>[]>()), Times.Once);
//    }

//    [Test]
//    public async Task GetPublishedAsync_ReturnsPublishedPosts()
//    {
//        var items = new[]
//        {
//            new Post { Id = Guid.NewGuid(), Title = "A", PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-10) },
//            new Post { Id = Guid.NewGuid(), Title = "B", PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-5) }
//        }.AsEnumerable();

//        _postRepoMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Post, bool>>>(), It.IsAny<int?>(),
//                It.IsAny<int?>(), It.IsAny<Expression<Func<Post, object>>[]>()))
//            .ReturnsAsync(items);

//        var res = await _service.GetPublishedAsync();

//        Assert.That(res, Is.Not.Null);
//        Assert.That(res.Count(), Is.EqualTo(2));
//        Assert.That(res.Select(x => x.Title), Does.Contain("A").And.Contain("B"));
//        _postRepoMock.Verify(
//            r => r.ListAsync(It.IsAny<Expression<Func<Post, bool>>>(), 0, 20,
//                It.IsAny<Expression<Func<Post, object>>[]>()), Times.Once);
//    }

//    [Test]
//    public async Task CreateAsync_SetsFields_AddsAndSaves()
//    {
//        var p = new Post { Title = "New", Summary = "S", Body = "B" };

//        _postRepoMock.Setup(r => r.AddAsync(It.IsAny<Post>())).Returns(Task.CompletedTask);
//        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

//        var created = await _service.CreateAsync(p);

//        Assert.That(created, Is.Not.Null);
//        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
//        Assert.That(created.CreatedAt, Is.Not.EqualTo(default(DateTimeOffset)));
//        Assert.That(created.PublishedAt, Is.Null);
//        _postRepoMock.Verify(r => r.AddAsync(It.Is<Post>(x => x.Id == created.Id)), Times.Once);
//        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
//    }

//    [Test]
//    public async Task PublishAsync_WhenPostExists_SetsPublishedAt_UpdatesAndSaves()
//    {
//        var id = Guid.NewGuid();
//        var existing = new Post { Id = id, Title = "P" };

//        _postRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
//        _postRepoMock.Setup(r => r.Update(It.IsAny<Post>()));
//        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

//        await _service.PublishAsync(id);

//        _postRepoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
//        _postRepoMock.Verify(r => r.Update(It.Is<Post>(x => x.Id == id && x.PublishedAt != null)), Times.Once);
//        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
//    }

//    [Test]
//    public void PublishAsync_WhenNotFound_ThrowsKeyNotFoundException()
//    {
//        var id = Guid.NewGuid();
//        _postRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Post?)null);

//        Assert.That(async () => await _service.PublishAsync(id), Throws.TypeOf<KeyNotFoundException>());
//        _postRepoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
//        _postRepoMock.Verify(r => r.Update(It.IsAny<Post>()), Times.Never);
//        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
//    }

//    [Test]
//    public async Task DeleteAsync_WhenPostExists_DeletesAndSaves()
//    {
//        var id = Guid.NewGuid();
//        var p = new Post { Id = id };

//        _postRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(p);
//        _postRepoMock.Setup(r => r.Delete(It.IsAny<Post>()));
//        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

//        await _service.DeleteAsync(id);

//        _postRepoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
//        _postRepoMock.Verify(r => r.Delete(It.Is<Post>(x => x.Id == id)), Times.Once);
//        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
//    }

//    [Test]
//    public async Task DeleteAsync_WhenNotFound_DoesNothing()
//    {
//        var id = Guid.NewGuid();
//        _postRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Post?)null);

//        await _service.DeleteAsync(id);

//        _postRepoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
//        _postRepoMock.Verify(r => r.Delete(It.IsAny<Post>()), Times.Never);
//        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
//    }
//}
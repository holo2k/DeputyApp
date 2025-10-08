using Application.Services.Abstractions;
using Application.Services.Implementations;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;
using Moq;

namespace DeputyApp.Tests;

[TestFixture]
public class DocumentServiceTests
{
    [SetUp]
    public void SetUp()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _docRepoMock = new Mock<IDocumentRepository>();
        _storageMock = new Mock<IFileStorage>();

        _uowMock.SetupGet(x => x.Documents).Returns(_docRepoMock.Object);

        _service = new DocumentService(_storageMock.Object, _uowMock.Object);
    }

    private Mock<IUnitOfWork> _uowMock = null!;
    private Mock<IDocumentRepository> _docRepoMock = null!;
    private Mock<IFileStorage> _storageMock = null!;
    private DocumentService _service = null!;

    [Test]
    public async Task UploadAsync_UploadsToStorage_AddsDocumentAndSaves()
    {
        var fileName = "test.pdf";
        var contentBytes = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(contentBytes);
        var contentType = "application/pdf";
        var expectedUrl = "deputy-files/test.pdf";

        _storageMock.Setup(s => s.UploadAsync(fileName, It.IsAny<Stream>(), contentType))
            .ReturnsAsync(expectedUrl);

        _uowMock.Setup(u => u.Documents.AddAsync(It.IsAny<Document>()))
            .Returns(Task.CompletedTask);

        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _service.UploadAsync(fileName, stream, contentType, null, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.FileName, Is.EqualTo(fileName));
        Assert.That(result.Url, Is.EqualTo(expectedUrl));
        Assert.That(result.ContentType, Is.EqualTo(contentType));
        Assert.That(result.Size, Is.EqualTo(stream.Length));

        _storageMock.Verify(s => s.UploadAsync(fileName, It.IsAny<Stream>(), contentType), Times.Once);
        _docRepoMock.Verify(r => r.AddAsync(It.Is<Document>(d => d.FileName == fileName && d.Url == expectedUrl)),
            Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenDocumentExists_DeletesFromStorageAndRemovesAndSaves()
    {
        var id = Guid.NewGuid();
        var doc = new Document { Id = id, Url = "deputy-files/x.pdf" };

        _docRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(doc);
        _storageMock.Setup(s => s.DeleteAsync(doc.Url)).Returns(Task.CompletedTask);
        _docRepoMock.Setup(r => r.Delete(It.IsAny<Document>()));

        _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _service.DeleteAsync(id);

        _docRepoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        _storageMock.Verify(s => s.DeleteAsync(doc.Url), Times.Once);
        _docRepoMock.Verify(r => r.Delete(It.Is<Document>(d => d.Id == id)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenDocumentNotFound_DoesNothing()
    {
        var id = Guid.NewGuid();
        _docRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Document?)null);

        await _service.DeleteAsync(id);

        _docRepoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        _storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
        _docRepoMock.Verify(r => r.Delete(It.IsAny<Document>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task GetByCatalogAsync_ReturnsRepositoryResults()
    {
        var catalogId = Guid.NewGuid();
        var docs = new[]
        {
            new Document { Id = Guid.NewGuid(), FileName = "a.pdf" },
            new Document { Id = Guid.NewGuid(), FileName = "b.pdf" }
        }.AsEnumerable();

        _docRepoMock.Setup(r => r.GetByCatalogAsync(catalogId)).ReturnsAsync(docs);

        var result = await _service.GetByCatalogAsync(catalogId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Select(d => d.FileName), Does.Contain("a.pdf").And.Contain("b.pdf"));

        _docRepoMock.Verify(r => r.GetByCatalogAsync(catalogId), Times.Once);
    }
}
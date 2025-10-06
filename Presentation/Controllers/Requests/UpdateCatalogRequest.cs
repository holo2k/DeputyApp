namespace Presentation.Controllers.Requests;

public record UpdateCatalogRequest(string NewName, Guid? NewParentCatalogId);
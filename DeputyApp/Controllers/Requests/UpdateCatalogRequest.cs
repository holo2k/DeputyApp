namespace DeputyApp.Controllers.Requests;

public record UpdateCatalogRequest(string NewName, Guid? NewParentCatalogId);
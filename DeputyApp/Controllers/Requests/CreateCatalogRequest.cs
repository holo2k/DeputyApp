namespace DeputyApp.Controllers.Requests;

public record CreateCatalogRequest(string Name, Guid? ParentCatalogId);
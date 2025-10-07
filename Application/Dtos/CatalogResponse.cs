namespace Application.Dtos;

public record CatalogResponse(Guid Id, string Name, Guid? ParentCatalogId);
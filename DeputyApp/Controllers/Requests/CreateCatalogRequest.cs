namespace DeputyApp.Controllers.Requests;

/// <summary>Запрос на создание каталога.</summary>
public record CreateCatalogRequest(string Name, Guid? ParentCatalogId);
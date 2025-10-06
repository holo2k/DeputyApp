namespace DeputyApp.Controllers.Requests;

/// <summary>Запрос на обновление каталога.</summary>
public record UpdateCatalogRequest(string NewName, Guid? NewParentCatalogId);
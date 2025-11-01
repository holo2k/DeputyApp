using Domain.Enums;

namespace Application.Dtos;

/// <summary>
///     Запрос на загрузку файла в указанный каталог.
/// </summary>
public class UploadFileRequest
{
    /// <summary>
    ///     Идентификатор каталога, в который загружается файл.
    /// </summary>
    public Guid CatalogId { get; set; }

    /// <summary>
    ///     Статус документа (необязательно).
    /// </summary>
    public DocumentStatus? DocumentStatus { get; set; } = null;

    /// <summary>
    ///     Дата начала обработки документа (необязательно).
    /// </summary>
    public DateTimeOffset? StartDate { get; set; } = null;

    /// <summary>
    ///     Предполагаемая дата окончания обработки документа (необязательно).
    /// </summary>
    public DateTimeOffset? EndDate { get; set; } = null;
}
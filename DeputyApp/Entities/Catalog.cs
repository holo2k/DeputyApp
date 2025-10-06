namespace DeputyApp.Entities;

public class Catalog
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCatalogId { get; set; }
    public Catalog? ParentCatalog { get; set; }
    public ICollection<Catalog> Children { get; set; } = new List<Catalog>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
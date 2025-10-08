using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DAL;

public class AppDbContext : DbContext
{
    private DbContextOptions<AppDbContext> _options;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        _options = options;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Catalog> Catalogs => Set<Catalog>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).IsRequired();
            b.Property(x => x.PasswordHash).IsRequired();
        });


        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Name).IsUnique();
        });


        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            b.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        });


        modelBuilder.Entity<Post>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.CreatedBy).WithMany(u => u.Posts).HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasMany(x => x.Attachments).WithOne(d => d.Post).HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        modelBuilder.Entity<Event>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.Organizer).WithMany(u => u.EventsOrganized).HasForeignKey(x => x.OrganizerId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.StartAt);
        });


        modelBuilder.Entity<Catalog>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.ParentCatalog).WithMany(x => x.Children).HasForeignKey(x => x.ParentCatalogId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<Document>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.UploadedBy).WithMany(u => u.Documents).HasForeignKey(x => x.UploadedById)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Catalog).WithMany(c => c.Documents).HasForeignKey(x => x.CatalogId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.CatalogId);
        });


        modelBuilder.Entity<AnalyticsEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.EventType);
            b.HasIndex(x => x.Timestamp);
        });


        modelBuilder.Entity<Feedback>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            b.Property(x => x.Message).IsRequired();
        });
    }
}
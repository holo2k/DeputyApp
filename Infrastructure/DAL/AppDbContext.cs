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
    public DbSet<Chats> Chats => Set<Chats>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Catalog> Catalogs => Set<Catalog>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================
        // Chats
        // ============================
        modelBuilder.Entity<Chats>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ChatId).IsUnique();
        });

        // ============================
        // User
        // ============================
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).IsRequired();
            b.Property(x => x.PasswordHash).IsRequired();

            // Самоссылка (Deputy)
            b.HasOne(x => x.Deputy)
                .WithMany()
                .HasForeignKey("DeputyId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // User → TaskEntity (автор)
            b.HasMany(u => u.Tasks)
                .WithOne()
                .HasForeignKey(t => t.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-many User ⇄ TaskEntity (исполнители)
            b.HasMany(u => u.Tasks)
                .WithMany(t => t.Users)
                .UsingEntity(j => j.ToTable("UserTasks"));
        });

        // ============================
        // Role
        // ============================
        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Name).IsUnique();
        });

        // ============================
        // UserRole
        // ============================
        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.RoleId });

            b.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            b.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
        });

        // ============================
        // Post
        // ============================
        modelBuilder.Entity<Post>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasOne(x => x.CreatedBy)
                .WithMany(u => u.Posts)
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasMany(x => x.Attachments)
                .WithOne(d => d.Post)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ============================
        // Event
        // ============================
        modelBuilder.Entity<Event>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Organizer)
                .WithMany(u => u.EventsOrganized)
                .HasForeignKey(x => x.OrganizerId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.StartAt);
            b.Property(x => x.Type).HasConversion<int>().IsRequired();
            b.HasMany(e => e.Attachments).WithOne(a => a.Event).HasForeignKey(a => a.EventId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(e => e.Participants).WithOne(pe => pe.Event).HasForeignKey(pe => pe.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        // ============================
        // Catalog
        // ============================
        modelBuilder.Entity<Catalog>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasOne(x => x.ParentCatalog)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentCatalogId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // Document
        // ============================
        modelBuilder.Entity<Document>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasOne(x => x.UploadedBy)
                .WithMany(u => u.Documents)
                .HasForeignKey(x => x.UploadedById)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.Catalog)
                .WithMany(c => c.Documents)
                .HasForeignKey(x => x.CatalogId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.CatalogId);
        });

        // ============================
        // AnalyticsEvent
        // ============================
        modelBuilder.Entity<AnalyticsEvent>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.EventType);
            b.HasIndex(x => x.Timestamp);
        });

        // ============================
        // Feedback
        // ============================
        modelBuilder.Entity<Feedback>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.Property(x => x.Message).IsRequired();
        });

        // ============================
        // TaskEntity
        // ============================
        modelBuilder.Entity<TaskEntity>(b =>
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.Title).IsRequired();
            b.Property(x => x.Description);
            b.Property(x => x.Priority).IsRequired();

            // Many-to-many: TaskEntity ⇄ User
            b.HasMany(t => t.Users)
                .WithMany(u => u.Tasks)
                .UsingEntity(j => j.ToTable("UserTasks"));
        });

        modelBuilder.Entity<EventAttachment>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(a => a.Document).WithMany().HasForeignKey(a => a.DocumentId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(a => a.EventId);
        });

        modelBuilder.Entity<UserEvent>(b =>
        {
            b.HasKey(ue => new { ue.UserId, ue.EventId });
            b.HasOne(ue => ue.User).WithMany(u => u.Events).HasForeignKey(ue => ue.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(ue => ue.Event).WithMany(e => e.Participants).HasForeignKey(ue => ue.EventId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(ue => ue.ExcuseDocument).WithMany().HasForeignKey(ue => ue.ExcuseDocumentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(ue => ue.Status).HasConversion<int>().IsRequired();
        });
    }
}

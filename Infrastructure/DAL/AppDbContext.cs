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
    public DbSet<Status> Statuses => Set<Status>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Chats>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ChatId).IsUnique();
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).IsRequired();
            b.Property(x => x.PasswordHash).IsRequired();

            b.HasOne(x => x.Deputy).WithMany().HasForeignKey("DeputyId").IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict); 

            b.HasMany(u => u.Tasks).WithMany(t => t.Users).UsingEntity(j => j.ToTable("UserTasks")); // Юзер ↔ таск
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId); // Юзер → роль
            b.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId); // Роль → юзер
        });

        modelBuilder.Entity<Post>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.CreatedBy).WithMany(u => u.Posts).HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.SetNull); // Пост → создатель
            b.HasMany(x => x.Attachments).WithOne(d => d.Post).HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.SetNull); // Пост → файлы
        });

        modelBuilder.Entity<Event>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.Organizer).WithMany(u => u.EventsOrganized).HasForeignKey(x => x.OrganizerId)
                .OnDelete(DeleteBehavior.SetNull); // Событие → организатор
            b.HasIndex(x => x.StartAt);
            b.Property(x => x.Type).HasConversion<int>().IsRequired();
            b.HasMany(e => e.Attachments).WithOne(a => a.Event).HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.Cascade); // Событие → файлы
            b.HasMany(e => e.Participants).WithOne(pe => pe.Event).HasForeignKey(pe => pe.EventId)
                .OnDelete(DeleteBehavior.Cascade); // Событие → участники
        });

        modelBuilder.Entity<Catalog>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.ParentCatalog).WithMany(x => x.Children).HasForeignKey(x => x.ParentCatalogId)
                .OnDelete(DeleteBehavior.Restrict); // Каталог → родитель
        });

        modelBuilder.Entity<Document>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.UploadedBy).WithMany(u => u.Documents).HasForeignKey(x => x.UploadedById)
                .OnDelete(DeleteBehavior.SetNull); // Документ → юзер
            b.HasOne(x => x.Catalog).WithMany(c => c.Documents).HasForeignKey(x => x.CatalogId)
                .OnDelete(DeleteBehavior.SetNull); // Документ → каталог
            b.HasIndex(x => x.CatalogId);
        });

        modelBuilder.Entity<AnalyticsEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Событие → юзер
            b.HasIndex(x => x.EventType);
            b.HasIndex(x => x.Timestamp);
        });

        modelBuilder.Entity<Feedback>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Фидбек → юзер
            b.Property(x => x.Message).IsRequired();
        });

        modelBuilder.Entity<TaskEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired();
            b.Property(x => x.Description);
            b.Property(x => x.Priority).IsRequired();
            b.HasMany(t => t.Users)
                .WithMany(u => u.Tasks)
                .UsingEntity(j => j.ToTable("UserTasks")); // Таск ↔ юзер
            b.HasOne(t => t.Status)
                .WithMany(s => s.TaskEntities)
                .HasForeignKey(t => t.StatusId);
        });

        modelBuilder.Entity<Status>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().IsUnicode();
            b.Property(x => x.IsDefault).IsRequired();
            b.HasMany(s => s.TaskEntities).WithOne(t => t.Status);
        });

        modelBuilder.Entity<EventAttachment>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(a => a.Document).WithMany().HasForeignKey(a => a.DocumentId)
                .OnDelete(DeleteBehavior.Restrict); // Вложение → документ
            b.HasIndex(a => a.EventId);
        });

        modelBuilder.Entity<UserEvent>(b =>
        {
            b.HasKey(ue => new { ue.UserId, ue.EventId });
            b.HasOne(ue => ue.User).WithMany(u => u.Events).HasForeignKey(ue => ue.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Участник → юзер
            b.HasOne(ue => ue.Event).WithMany(e => e.Participants).HasForeignKey(ue => ue.EventId)
                .OnDelete(DeleteBehavior.Cascade); // Участник → событие
            b.HasOne(ue => ue.ExcuseDocument).WithMany().HasForeignKey(ue => ue.ExcuseDocumentId)
                .OnDelete(DeleteBehavior.Restrict); // Участник → документ оправдания
            b.Property(ue => ue.Status).HasConversion<int>().IsRequired();
        });
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Status>())
        {
            if (entry.State == EntityState.Modified && entry.OriginalValues.GetValue<bool>("IsDefault"))
            {
                throw new InvalidOperationException("Нельзя изменять дефолтные статусы.");
            }
            if (entry.State == EntityState.Deleted && entry.OriginalValues.GetValue<bool>("IsDefault"))
            {
                throw new InvalidOperationException("Нельзя удалять дефолтные статусы.");
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
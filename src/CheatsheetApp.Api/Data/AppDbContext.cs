using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Cheatsheet> Cheatsheets => Set<Cheatsheet>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Username).IsUnique();
            b.Property(u => u.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Name).HasMaxLength(100);
            b.Property(c => c.Slug).HasMaxLength(120);
        });

        modelBuilder.Entity<Cheatsheet>(b =>
        {
            b.HasIndex(c => new { c.CategoryId, c.Slug }).IsUnique();
            b.Property(c => c.Title).HasMaxLength(200);
            b.Property(c => c.Slug).HasMaxLength(240);
            b.Property(c => c.ContentType).HasMaxLength(10);
            b.HasOne(c => c.Category).WithMany(cat => cat.Cheatsheets)
                .HasForeignKey(c => c.CategoryId).OnDelete(DeleteBehavior.Restrict);
            // Title weighted 'A', content 'B' so title matches outrank body matches in ts_rank.
            b.Property(c => c.SearchVector)
                .HasColumnType("tsvector")
                .HasComputedColumnSql(
                    "setweight(to_tsvector('english', coalesce(title, '')), 'A') || " +
                    "setweight(to_tsvector('english', coalesce(content, '')), 'B')",
                    stored: true);
            b.HasIndex(c => c.SearchVector).HasMethod("GIN");
        });

        modelBuilder.Entity<Tag>(b =>
        {
            b.HasIndex(t => t.Slug).IsUnique();
            b.Property(t => t.Name).HasMaxLength(60);
            b.Property(t => t.Slug).HasMaxLength(80);
            b.HasMany(t => t.Cheatsheets).WithMany(c => c.Tags);
        });
    }
}

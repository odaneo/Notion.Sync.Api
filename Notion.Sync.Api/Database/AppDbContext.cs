using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Models;

namespace Notion.Sync.Api.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<SubTag> SubTags { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }
        public DbSet<ArticleSubTag> ArticleSubTags { get; set; }
        public DbSet<SyncRun> SyncRuns { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity
                    .HasMany(e => e.ArticleTags)
                    .WithOne(e => e.Tag)
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<SubTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity
                    .HasMany(e => e.ArticleSubTags)
                    .WithOne(e => e.SubTag)
                    .HasForeignKey(e => e.SubTagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity
                    .HasMany(e => e.ArticleSubTags)
                    .WithOne(e => e.Article)
                    .HasForeignKey(e => e.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity
                    .HasMany(e => e.ArticleTags)
                    .WithOne(e => e.Article)
                    .HasForeignKey(e => e.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ArticleTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ArticleId, e.TagId }).IsUnique();
            });
            modelBuilder.Entity<ArticleSubTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ArticleId, e.SubTagId }).IsUnique();
            });
            modelBuilder.Entity<SyncRun>().ToTable("SyncRuns", table =>
                table.HasCheckConstraint("CK_SyncRuns_CacheAfterDatabase",
                    "\"CacheInvalidatedAt\" IS NULL OR \"DatabaseAppliedAt\" IS NOT NULL"));
        }
    }
}

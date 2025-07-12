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
        public DbSet<NotionArticle> NotionArticles { get; set; }
        public DbSet<NotionArticleTag> NotionArticleTags { get; set; }
        public DbSet<NotionArticleSubTag> NotionArticleSubTags { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity
                    .HasMany(e => e.SubTags)
                    .WithOne(e => e.Tag)
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity
                    .HasMany(e => e.NotionArticleTags)
                    .WithOne(e => e.Tag)
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<SubTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity
                    .HasMany(e => e.NotionArticleSubTags)
                    .WithOne(e => e.SubTag)
                    .HasForeignKey(e => e.SubTagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<NotionArticle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity
                    .HasMany(e => e.NotionArticleSubTags)
                    .WithOne(e => e.NotionArticle)
                    .HasForeignKey(e => e.NotionArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity
                    .HasMany(e => e.NotionArticleTags)
                    .WithOne(e => e.NotionArticle)
                    .HasForeignKey(e => e.NotionArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<NotionArticleTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.NotionArticleId, e.TagId }).IsUnique();
            });
            modelBuilder.Entity<NotionArticleSubTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.NotionArticleId, e.SubTagId }).IsUnique();
            });
        }
    }
}

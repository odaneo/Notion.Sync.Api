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
            });
            modelBuilder.Entity<SubTag>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }
}

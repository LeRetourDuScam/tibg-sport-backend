using Microsoft.EntityFrameworkCore;
using TIBG.Models;

namespace TIBG.ENTITIES
{
    public class FytAiDbContext : DbContext
    {
        public FytAiDbContext(DbContextOptions<FytAiDbContext> options) : base(options)
        {
        }

        public DbSet<UserFeedback> Feedbacks { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });
        }
    }
}

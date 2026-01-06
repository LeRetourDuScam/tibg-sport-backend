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
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserFeedback>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}

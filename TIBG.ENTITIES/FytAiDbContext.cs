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
    }
}

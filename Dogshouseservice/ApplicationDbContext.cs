using Dogshouseservice.Models;
using Microsoft.EntityFrameworkCore;

namespace Dogshouseservice
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<DogModel> Dogs { get; set; }
    }
}

using Dogshouseservice.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Dogshouseservice
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Dog> Dogs { get; set; }
    }
}

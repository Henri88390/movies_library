using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace MoviesApi.Models;

public class MoviesContext(DbContextOptions<MoviesContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Movie> MoviesItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Additional configuration can be added here
        builder.Entity<Movie>().HasIndex(m => m.Name);
        builder.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
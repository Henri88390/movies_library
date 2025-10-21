using Microsoft.EntityFrameworkCore;

namespace MoviesApi.Models;

public class MoviesContext(DbContextOptions<MoviesContext> options) : DbContext(options)
{
    public DbSet<Movie> MoviesItems { get; set; } = null!;
}
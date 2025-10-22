using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MoviesApi.Models;
using MoviesApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Movies Library API",
        Version = "v1",
        Description = "A simple API for managing a movie library"
    });
});

// Add file upload service
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddDbContext<MoviesContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Seed the database with initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MoviesContext>();

    // Ensure database is created
    context.Database.EnsureCreated();

    // Seed data if the database is empty
    if (!context.MoviesItems.Any())
    {
        var movies = new[]
        {
            new Movie
            {
                Name = "The Shawshank Redemption",
                Realisator = "Frank Darabont",
                Rating = 9,
                Duration = TimeSpan.FromMinutes(142),
                ImagePath = "https://m.media-amazon.com/images/M/MV5BNDE3ODcxYzMtY2YzZC00NmNlLWJiNDMtZDViZWM2MzIxZDYwXkEyXkFqcGdeQXVyNjAwNDUxODI@._V1_SX300.jpg"
            },
            new Movie
            {
                Name = "The Godfather",
                Realisator = "Francis Ford Coppola",
                Rating = 9,
                Duration = TimeSpan.FromMinutes(175),
                ImagePath = "https://image.tmdb.org/t/p/w500/3bhkrj58Vtu7enYsRolD1fZdja1.jpg"
            },
            new Movie
            {
                Name = "The Dark Knight",
                Realisator = "Christopher Nolan",
                Rating = 9,
                Duration = TimeSpan.FromMinutes(152),
                ImagePath = "https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_SX300.jpg"
            },
            new Movie
            {
                Name = "Pulp Fiction",
                Realisator = "Quentin Tarantino",
                Rating = 8,
                Duration = TimeSpan.FromMinutes(154),
                ImagePath = "https://m.media-amazon.com/images/M/MV5BNGNhMDIzZTUtNTBlZi00MTRlLWFjM2ItYzViMjE3YzI5MjljXkEyXkFqcGdeQXVyNzkwMjQ5NzM@._V1_SX300.jpg"
            },
            new Movie
            {
                Name = "Forrest Gump",
                Realisator = "Robert Zemeckis",
                Rating = 8,
                Duration = TimeSpan.FromMinutes(142),
                ImagePath = "https://m.media-amazon.com/images/M/MV5BNWIwODRlZTUtY2U3ZS00Yzg1LWJhNzYtMmZiYmEyNmU1NjMzXkEyXkFqcGdeQXVyMTQxNzMzNDI@._V1_SX300.jpg"
            },
            new Movie
            {
                Name = "Inception",
                Realisator = "Christopher Nolan",
                Rating = 8,
                Duration = TimeSpan.FromMinutes(148),
                ImagePath = "https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_SX300.jpg"
            },
            new Movie
            {
                Name = "The Matrix",
                Realisator = "The Wachowskis",
                Rating = 8,
                Duration = TimeSpan.FromMinutes(136),
                ImagePath = "https://m.media-amazon.com/images/M/MV5BNzQzOTk3OTAtNDQ0Zi00ZTVkLWI0MTEtMDllZjNkYzNjNTc4L2ltYWdlXkEyXkFqcGdeQXVyNjU0OTQ0OTY@._V1_SX300.jpg"
            },
            new Movie
            {
                Name = "Goodfellas",
                Realisator = "Martin Scorsese",
                Rating = 8,
                Duration = TimeSpan.FromMinutes(146),
                ImagePath = "https://m.media-amazon.com/images/M/MV5BY2NkZjEzMDgtN2RjYy00YzM1LWI4ZmQtMjIwYjFjNmI3ZGEwXkEyXkFqcGdeQXVyNzkwMjQ5NzM@._V1_SX300.jpg"
            }
        };

        context.MoviesItems.AddRange(movies);
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable default static file serving for wwwroot
app.UseStaticFiles();

// Configure static file serving for uploads directory in wwwroot
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPath = Path.Combine(wwwrootPath, "uploads");

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    app.Logger.LogInformation("Created uploads directory: {UploadsPath}", uploadsPath);
}

app.Logger.LogInformation("Static file serving configured for uploads at: {UploadsPath}", uploadsPath);

// Enable CORS
app.UseCors("AllowAngularApp");

app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the Program class accessible for testing
public partial class Program { }

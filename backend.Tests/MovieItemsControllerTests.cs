using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoviesApi.Models;
using Xunit;

namespace backend.Tests;

public class MovieItemsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MovieItemsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the original DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MoviesContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add a test database
                services.AddDbContext<MoviesContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
            });
        });

        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetMovies_ReturnsEmptyList_WhenNoMoviesExist()
    {
        // Arrange
        await ClearDatabase();

        // Act
        var response = await _client.GetAsync("/api/movies/movies");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var movies = JsonSerializer.Deserialize<List<Movie>>(content, _jsonOptions);

        Assert.NotNull(movies);
        Assert.Empty(movies);
    }

    [Fact]
    public async Task PostMovie_CreatesMovie_WithValidData()
    {
        // Arrange
        await ClearDatabase();
        var movieData = new
        {
            name = "New Movie",
            realisator = "Test Director",
            rating = 9,
            duration = "02:30:00"
        };

        var json = JsonSerializer.Serialize(movieData, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/movies", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdMovie = JsonSerializer.Deserialize<Movie>(responseContent, _jsonOptions);

        Assert.NotNull(createdMovie);
        Assert.Equal("New Movie", createdMovie.Name);
        Assert.Equal("Test Director", createdMovie.Realisator);
        Assert.Equal(9, createdMovie.Rating);
    }

    [Fact]
    public async Task PostMovie_ReturnsBadRequest_WithInvalidRating()
    {
        // Arrange
        await ClearDatabase();
        var movieData = new
        {
            name = "Test Movie",
            rating = 15, // Invalid: rating > 10
            duration = "02:00:00"
        };

        var json = JsonSerializer.Serialize(movieData, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/movies", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMovie_ReturnsMovie_WhenMovieExists()
    {
        // Arrange
        await ClearDatabase();
        var movieId = await SeedSingleMovie();

        // Act
        var response = await _client.GetAsync($"/api/movies/{movieId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var movie = JsonSerializer.Deserialize<Movie>(content, _jsonOptions);

        Assert.NotNull(movie);
        Assert.Equal("Test Movie", movie.Name);
        Assert.Equal(8, movie.Rating);
    }

    [Fact]
    public async Task GetMovie_ReturnsNotFound_WhenMovieDoesNotExist()
    {
        // Arrange
        await ClearDatabase();

        // Act
        var response = await _client.GetAsync("/api/movies/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutMovie_UpdatesMovie_WithValidData()
    {
        // Arrange
        await ClearDatabase();
        var movieId = await SeedSingleMovie();

        var updatedMovie = new Movie
        {
            Id = movieId,
            Name = "Updated Movie",
            Realisator = "Updated Director",
            Rating = 10,
            Duration = TimeSpan.FromHours(3)
        };

        var json = JsonSerializer.Serialize(updatedMovie, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/movies/{movieId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PatchMovie_UpdatesPartialFields_WithValidData()
    {
        // Arrange
        await ClearDatabase();
        var movieId = await SeedSingleMovie();

        var updateData = new
        {
            rating = 10 // Only update rating
        };

        var json = JsonSerializer.Serialize(updateData, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PatchAsync($"/api/movies/{movieId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMovie_RemovesMovie_WhenMovieExists()
    {
        // Arrange
        await ClearDatabase();
        var movieId = await SeedSingleMovie();

        // Act
        var response = await _client.DeleteAsync($"/api/movies/{movieId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/movies/{movieId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task ClearDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MoviesContext>();

        context.MoviesItems.RemoveRange(context.MoviesItems);
        await context.SaveChangesAsync();
    }

    private async Task<long> SeedSingleMovie()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MoviesContext>();

        var movie = new Movie
        {
            Name = "Test Movie",
            Realisator = "Test Director",
            Rating = 8,
            Duration = TimeSpan.FromHours(2)
        };

        context.MoviesItems.Add(movie);
        await context.SaveChangesAsync();

        return movie.Id;
    }
}

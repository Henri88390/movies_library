using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoviesApi.Models;
using Xunit;

namespace backend.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Create a new service collection and only copy non-EF services
            var newServices = new ServiceCollection();

            foreach (var service in services)
            {
                // Skip all EF Core and SQLite related services
                if (service.ServiceType.Namespace?.Contains("EntityFrameworkCore") == true ||
                    service.ServiceType.FullName?.Contains("Sqlite") == true ||
                    service.ServiceType == typeof(MoviesContext) ||
                    service.ServiceType == typeof(DbContextOptions<MoviesContext>) ||
                    service.ServiceType == typeof(DbContextOptions))
                    continue;

                newServices.Add(service);
            }

            // Add fresh in-memory database with consistent name
            newServices.AddDbContext<MoviesContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            services.Clear();
            foreach (var service in newServices)
            {
                services.Add(service);
            }
        });

        builder.UseEnvironment("Testing");
    }
}

public class MovieItemsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public MovieItemsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
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
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("New Movie"), "name");
        formData.Add(new StringContent("Test Director"), "realisator");
        formData.Add(new StringContent("9"), "rating");
        formData.Add(new StringContent("02:30:00"), "duration");

        // Act
        var response = await _client.PostAsync("/api/movies/movies", formData);

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
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("Test Movie"), "name");
        formData.Add(new StringContent("15"), "rating"); // Invalid: rating > 10
        formData.Add(new StringContent("02:00:00"), "duration");

        // Act
        var response = await _client.PostAsync("/api/movies/movies", formData);

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

        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("Updated Movie"), "name");
        formData.Add(new StringContent("Updated Director"), "realisator");
        formData.Add(new StringContent("10"), "rating");
        formData.Add(new StringContent("03:00:00"), "duration");

        // Act
        var response = await _client.PutAsync($"/api/movies/{movieId}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

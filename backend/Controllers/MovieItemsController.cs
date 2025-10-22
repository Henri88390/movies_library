using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.Models;
using MoviesApi.Services;

namespace backend.Controllers
{
    [Route("api/movies")]
    [ApiController]
    public class MovieItemsController : ControllerBase
    {
        private readonly MoviesContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<MovieItemsController> _logger;

        public MovieItemsController(MoviesContext context, IFileUploadService fileUploadService, ILogger<MovieItemsController> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // GET: api/MovieItems
        [HttpGet("movies")]
        public async Task<ActionResult<IEnumerable<MovieGetDTO>>> GetMoviesItems()
        {
            return await _context.MoviesItems.Select(x => x.MovieItemToDTO()).ToListAsync();
        }

        // GET: api/MovieItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Movie>> GetMovie(long id)
        {
            var movie = await _context.MoviesItems.FindAsync(id);

            if (movie == null)
            {
                return NotFound();
            }

            return movie;
        }

        // PUT: api/MovieItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ActionResult<MovieGetDTO>> PutMovie(long id, MoviePutDto movieDto)
        {
            var existingMovie = await _context.MoviesItems.FindAsync(id);
            if (existingMovie == null)
            {
                return NotFound();
            }

            // Update the existing movie with DTO data
            if (movieDto.Name != null)
            {
                existingMovie.Name = movieDto.Name;
            }
            if (movieDto.Realisator != null)
            {
                existingMovie.Realisator = movieDto.Realisator;
            }
            if (movieDto.Rating.HasValue)
            {
                existingMovie.Rating = movieDto.Rating.Value;
            }
            if (movieDto.Duration.HasValue)
            {
                existingMovie.Duration = movieDto.Duration.Value;
            }
            if (movieDto.ImagePath != null)
            {
                // Delete old image if it exists
                if (!string.IsNullOrEmpty(existingMovie.ImagePath))
                {
                    _fileUploadService.DeleteImage(existingMovie.ImagePath);
                }
                existingMovie.ImagePath = movieDto.ImagePath;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MovieExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Return the updated movie as DTO
            return existingMovie.MovieItemToDTO();
        }

        // PATCH: api/MovieItems/5
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchMovie(long id, MoviePatchDto movieUpdate)
        {
            var existingMovie = await _context.MoviesItems.FindAsync(id);
            if (existingMovie == null)
            {
                return NotFound();
            }

            // Only update fields that are provided (not null)
            if (movieUpdate.Name != null)
            {
                existingMovie.Name = movieUpdate.Name;
            }

            if (movieUpdate.Realisator != null)
            {
                existingMovie.Realisator = movieUpdate.Realisator;
            }

            if (movieUpdate.Rating.HasValue)
            {
                existingMovie.Rating = movieUpdate.Rating.Value;
            }

            if (movieUpdate.Duration.HasValue)
            {
                existingMovie.Duration = movieUpdate.Duration.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MovieExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/movies/movies
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("movies")]
        public async Task<ActionResult<MovieGetDTO>> PostMovie([FromForm] string name,
            [FromForm] string realisator,
            [FromForm] int rating,
            [FromForm] string? duration = null,
            [FromForm] IFormFile? image = null)
        {
            _logger.LogInformation("Starting movie creation: Name={MovieName}, Director={Director}, Rating={Rating}, Duration={Duration}, HasImage={HasImage}", 
                name, realisator, rating, duration, image != null);

            try
            {
                // Parse duration if provided
                TimeSpan parsedDuration = TimeSpan.Zero;
                if (!string.IsNullOrEmpty(duration))
                {
                    _logger.LogDebug("Parsing duration: {Duration}", duration);
                    if (!TimeSpan.TryParse(duration, out parsedDuration))
                    {
                        _logger.LogWarning("Invalid duration format provided: {Duration}", duration);
                        return BadRequest("Invalid duration format. Use HH:MM:SS format.");
                    }
                }

                // Handle image upload
                string? imagePath = null;
                if (image != null)
                {
                    _logger.LogDebug("Processing image upload: FileName={FileName}, Size={Size}, ContentType={ContentType}", 
                        image.FileName, image.Length, image.ContentType);
                    imagePath = await _fileUploadService.SaveImageAsync(image);
                    _logger.LogDebug("Image saved successfully: {ImagePath}", imagePath);
                }

                _logger.LogDebug("Creating movie entity with parsed duration: {ParsedDuration}", parsedDuration);
                var movie = new Movie
                {
                    Name = name,
                    Realisator = realisator,
                    Rating = rating,
                    Duration = parsedDuration,
                    ImagePath = imagePath
                };

                _logger.LogDebug("Adding movie to database context");
                _context.MoviesItems.Add(movie);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Movie created successfully: Id={MovieId}, Name={MovieName}", movie.Id, movie.Name);
                return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie.MovieItemToDTO());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating movie: {MovieName}, {Director}, {Rating}", name, realisator, rating);
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating movie: {MovieName}, {Director}, {Rating}. InnerException: {InnerException}", 
                    name, realisator, rating, ex.InnerException?.Message);
                return StatusCode(500, "Database error occurred while creating the movie.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating movie: {MovieName}, {Director}, {Rating}. Exception Type: {ExceptionType}, Message: {Message}", 
                    name, realisator, rating, ex.GetType().Name, ex.Message);
                return StatusCode(500, "An unexpected error occurred while creating the movie.");
            }
        }

        // DELETE: api/MovieItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovie(long id)
        {
            var movie = await _context.MoviesItems.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            // Delete associated image file if it exists
            if (!string.IsNullOrEmpty(movie.ImagePath))
            {
                _fileUploadService.DeleteImage(movie.ImagePath);
            }

            _context.MoviesItems.Remove(movie);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MovieExists(long id)
        {
            return _context.MoviesItems.Any(e => e.Id == id);
        }
    }


}

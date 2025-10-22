using System;
using System.Collections.Generic;
using System.IO;
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
        public async Task<ActionResult<MovieGetDTO>> PutMovie(long id,
            [FromForm] string? name = null,
            [FromForm] string? realisator = null,
            [FromForm] int? rating = null,
            [FromForm] string? duration = null,
            [FromForm] IFormFile? image = null,
            [FromForm] bool? removeImage = false)
        {
            _logger.LogInformation("Starting movie update: Id={MovieId}, Name={MovieName}, HasImage={HasImage}, RemoveImage={RemoveImage}",
                id, name, image != null, removeImage);

            try
            {
                var existingMovie = await _context.MoviesItems.FindAsync(id);
                if (existingMovie == null)
                {
                    return NotFound();
                }

                // Update the existing movie with form data
                if (!string.IsNullOrEmpty(name))
                {
                    existingMovie.Name = name;
                }
                if (!string.IsNullOrEmpty(realisator))
                {
                    existingMovie.Realisator = realisator;
                }
                if (rating.HasValue)
                {
                    existingMovie.Rating = rating.Value;
                }
                if (!string.IsNullOrEmpty(duration))
                {
                    if (TimeSpan.TryParse(duration, out TimeSpan parsedDuration))
                    {
                        // Validate duration doesn't exceed 10 hours
                        if (parsedDuration > TimeSpan.FromHours(10))
                        {
                            return BadRequest(new { error = "Duration too long", message = "Duration cannot exceed 10 hours" });
                        }
                        existingMovie.Duration = parsedDuration;
                    }
                    else
                    {
                        return BadRequest("Invalid duration format. Use HH:MM:SS format.");
                    }
                }

                // Handle image updates
                if (removeImage == true)
                {
                    // Remove existing image
                    if (!string.IsNullOrEmpty(existingMovie.ImagePath))
                    {
                        _logger.LogDebug("Removing existing image: {ImagePath}", existingMovie.ImagePath);
                        _fileUploadService.DeleteImage(existingMovie.ImagePath);
                        existingMovie.ImagePath = null;
                    }
                }
                else if (image != null)
                {
                    // Replace with new image
                    _logger.LogDebug("Updating image: FileName={FileName}, Size={Size}", image.FileName, image.Length);

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(existingMovie.ImagePath))
                    {
                        _fileUploadService.DeleteImage(existingMovie.ImagePath);
                    }

                    // Save new image
                    string? imagePath = await _fileUploadService.SaveImageAsync(image);
                    existingMovie.ImagePath = imagePath;
                    _logger.LogDebug("New image saved: {ImagePath}", imagePath);
                }

                _logger.LogDebug("Updating movie in database");
                await _context.SaveChangesAsync();

                _logger.LogInformation("Movie updated successfully: Id={MovieId}, Name={MovieName}", existingMovie.Id, existingMovie.Name);
                return Ok(existingMovie.MovieItemToDTO());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating movie: Id={MovieId}. Operation: {Operation}", id, ex.Message);

                // Check specific file upload errors
                if (ex.Message.Contains("File size exceeds"))
                {
                    return BadRequest(new { error = "File too large", message = "Image file must be smaller than 5MB" });
                }
                else if (ex.Message.Contains("File type") && ex.Message.Contains("not allowed"))
                {
                    return BadRequest(new { error = "Invalid file type", message = "Only image files (JPG, PNG, GIF, WebP) are allowed" });
                }
                else if (ex.Message.Contains("Failed to save image"))
                {
                    return StatusCode(500, new { error = "File upload failed", message = "Failed to save the uploaded image. Please try again." });
                }
                else
                {
                    return BadRequest(new { error = "Invalid operation", message = ex.Message });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied while updating movie: Id={MovieId}", id);
                return StatusCode(500, new { error = "File system error", message = "Unable to access file storage. Please contact support." });
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Upload directory not found while updating movie: Id={MovieId}", id);
                return StatusCode(500, new { error = "Storage configuration error", message = "File storage is not properly configured. Please contact support." });
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error while updating movie: Id={MovieId}", id);
                return StatusCode(500, new { error = "File system error", message = "A file system error occurred. Please try again or contact support." });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while updating movie: Id={MovieId}", id);

                // Check if the movie still exists
                if (!MovieExists(id))
                {
                    return NotFound(new { error = "Movie not found", message = "The movie was deleted while you were editing it." });
                }
                else
                {
                    return Conflict(new { error = "Concurrency conflict", message = "The movie was modified by another user. Please refresh and try again." });
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating movie: Id={MovieId}. InnerException: {InnerException}",
                    id, ex.InnerException?.Message);

                // Check for specific database constraint violations
                if (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
                {
                    return Conflict(new { error = "Duplicate data", message = "A movie with this information already exists." });
                }
                else if (ex.InnerException?.Message?.Contains("CHECK constraint") == true)
                {
                    return BadRequest(new { error = "Invalid data", message = "The provided data violates database constraints." });
                }
                else if (ex.InnerException?.Message?.Contains("FOREIGN KEY constraint") == true)
                {
                    return BadRequest(new { error = "Reference error", message = "The movie references data that no longer exists." });
                }
                else
                {
                    return StatusCode(500, new { error = "Database error", message = "A database error occurred while updating the movie." });
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while updating movie: Id={MovieId}", id);
                return BadRequest(new { error = "Invalid input", message = ex.Message });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout while updating movie: Id={MovieId}", id);
                return StatusCode(408, new { error = "Request timeout", message = "The operation took too long to complete. Please try again." });
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogCritical(ex, "Out of memory while updating movie: Id={MovieId}", id);
                return StatusCode(507, new { error = "Server overloaded", message = "The server is currently overloaded. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating movie: Id={MovieId}. Exception Type: {ExceptionType}, Message: {Message}",
                    id, ex.GetType().Name, ex.Message);
                return StatusCode(500, new { error = "Unexpected error", message = "An unexpected error occurred while updating the movie. Please try again or contact support." });
            }
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

                    // Validate duration doesn't exceed 10 hours
                    if (parsedDuration > TimeSpan.FromHours(10))
                    {
                        _logger.LogWarning("Duration exceeds maximum: {Duration}", parsedDuration);
                        return BadRequest(new { error = "Duration too long", message = "Duration cannot exceed 10 hours" });
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.Models;

namespace backend.Controllers
{
    [Route("api/movies")]
    [ApiController]
    public class MovieItemsController : ControllerBase
    {
        private readonly MoviesContext _context;

        public MovieItemsController(MoviesContext context)
        {
            _context = context;
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
        public async Task<ActionResult<MovieGetDTO>> PostMovie(MoviePostDto movieDto)
        {
            var movie = new Movie
            {
                Name = movieDto.Name,
                Realisator = movieDto.Realisator,
                Rating = movieDto.Rating,
                Duration = movieDto.Duration ?? TimeSpan.Zero
            };

            _context.MoviesItems.Add(movie);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie.MovieItemToDTO());
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

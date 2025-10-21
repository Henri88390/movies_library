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
    [Route("api/[controller]")]
    [ApiController]
    public class MovieItemsController : ControllerBase
    {
        private readonly MoviesContext _context;

        public MovieItemsController(MoviesContext context)
        {
            _context = context;
        }

        // GET: api/MovieItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Movie>>> GetMoviesItems()
        {
            return await _context.MoviesItems.ToListAsync();
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
        public async Task<IActionResult> PutMovie(long id, Movie movie)
        {
            if (id != movie.Id)
            {
                return BadRequest();
            }

            _context.Entry(movie).State = EntityState.Modified;

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

        // POST: api/MovieItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Movie>> PostMovie(Movie movie)
        {
            _context.MoviesItems.Add(movie);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie);
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

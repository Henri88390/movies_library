
using System.ComponentModel.DataAnnotations;

namespace MoviesApi.Models
{
    public class Movie
    {
        public long Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Movie name must be between 1 and 200 characters")]
        public required string Name { get; set; }

        [StringLength(100, ErrorMessage = "Director name cannot exceed 100 characters")]
        public string? Realisator { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        public required int Rating { get; set; }

        [Required]
        public required TimeSpan Duration { get; set; }

        [StringLength(500, ErrorMessage = "Image path cannot exceed 500 characters")]
        public string? ImagePath { get; set; }

        public MovieGetDTO MovieItemToDTO() =>
       new()
       {
           Id = this.Id,
           Name = this.Name,
           Rating = this.Rating,
           Realisator = this.Realisator,
           Duration = this.Duration,
           ImagePath = this.ImagePath
       };
    }
}
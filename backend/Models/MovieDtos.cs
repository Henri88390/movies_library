using System.ComponentModel.DataAnnotations;

namespace MoviesApi.Models
{
    public class MovieGetDTO
    {
        public required long Id { get; set; }
        public string? Name { get; set; }
        public string? Realisator { get; set; }
        public int? Rating { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? ImagePath { get; set; }
    }

    public class MoviePutDto
    {
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Movie name must be between 1 and 200 characters")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Director name cannot exceed 100 characters")]
        public string? Realisator { get; set; }

        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        public int? Rating { get; set; }

        [MaxDuration(10)]
        public TimeSpan? Duration { get; set; }

        [StringLength(500, ErrorMessage = "Image path cannot exceed 500 characters")]
        public string? ImagePath { get; set; }
    }
    public class MoviePostDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Movie name must be between 1 and 200 characters")]
        public required string Name { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Director name cannot exceed 100 characters")]
        public required string Realisator { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        public required int Rating { get; set; }

        [MaxDuration(10)]
        public TimeSpan? Duration { get; set; }

        [StringLength(500, ErrorMessage = "Image path cannot exceed 500 characters")]
        public string? ImagePath { get; set; }
    }

    public class MoviePatchDto
    {
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Movie name must be between 1 and 200 characters")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Director name cannot exceed 100 characters")]
        public string? Realisator { get; set; }

        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        public int? Rating { get; set; }

        [MaxDuration(10)]
        public TimeSpan? Duration { get; set; }

        [StringLength(500, ErrorMessage = "Image path cannot exceed 500 characters")]
        public string? ImagePath { get; set; }
    }
}
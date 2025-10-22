using System.ComponentModel.DataAnnotations;

namespace MoviesApi.Models
{
    public class MaxDurationAttribute : ValidationAttribute
    {
        private readonly TimeSpan _maxDuration;

        public MaxDurationAttribute(int maxHours)
        {
            _maxDuration = TimeSpan.FromHours(maxHours);
            ErrorMessage = $"Duration cannot exceed {maxHours} hours";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                // Duration is optional, so null is valid
                return ValidationResult.Success;
            }

            if (value is TimeSpan duration)
            {
                if (duration > _maxDuration)
                {
                    return new ValidationResult(ErrorMessage);
                }
                return ValidationResult.Success;
            }

            // If it's not a TimeSpan, let other validators handle it
            return ValidationResult.Success;
        }
    }
}
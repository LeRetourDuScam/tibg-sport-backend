using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// Represents an exercise with detailed information
    /// </summary>
    public class Exercise
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Duration { get; set; } = string.Empty;

        [Required]
        public string Repetitions { get; set; } = string.Empty;

        [Required]
        [Url]
        public string VideoUrl { get; set; } = string.Empty;
    }
}

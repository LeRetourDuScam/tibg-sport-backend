using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// Represents an alternative sport recommendation
    /// </summary>
    public class SportAlternative
    {
        [Required]
        public string Sport { get; set; } = string.Empty;

        [Required]
        [Range(0, 100)]
        public int Score { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty ;
    }
}

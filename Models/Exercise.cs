using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapstoneProjectPUSH_API.Models
{
    public class Exercise
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsTimed { get; set; } = false;

        public int Difficulty { get; set; } = 1;

        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        // Filled in by the API from the Categories table (not stored)
        [NotMapped]
        public string CategoryTitle { get; set; } = string.Empty;
    }
}

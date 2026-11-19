using System.ComponentModel.DataAnnotations;

namespace CapstoneProjectPUSH_API.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Citation { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string CitationUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime DateTimePublished { get; set; } = DateTime.UtcNow;
    }
}

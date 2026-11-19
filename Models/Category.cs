using System.ComponentModel.DataAnnotations;

namespace CapstoneProjectPUSH_API.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;
    }
}

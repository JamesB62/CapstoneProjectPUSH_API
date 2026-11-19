using System.ComponentModel.DataAnnotations;

namespace CapstoneProjectPUSH_API.Models
{
    public class Gym
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
    }
}

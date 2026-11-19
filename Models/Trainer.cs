using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapstoneProjectPUSH_API.Models
{
    public class Trainer
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(60)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool Available { get; set; } = true;

        [MaxLength(500)]
        public string ProfileImageUrl { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [NotMapped]
        public string CategoryTitle { get; set; } = string.Empty;

        public int GymId { get; set; }

        [NotMapped]
        public string GymName { get; set; } = string.Empty;

        [NotMapped]
        public string GymAddress { get; set; } = string.Empty;

        [NotMapped]
        public string GymCity { get; set; } = string.Empty;

        [NotMapped]
        public bool IsAtFavouriteGym { get; set; }
    }
}

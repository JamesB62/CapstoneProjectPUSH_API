using System.ComponentModel.DataAnnotations.Schema;

namespace CapstoneProjectPUSH_API.Models
{
    public class WeightLog
    {
        public int Id { get; set; }

        public double WeightKg { get; set; } = 0;

        public DateTime DateTimeLogged { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }

        [NotMapped]
        public string UserDisplayName { get; set; } = string.Empty;
    }
}

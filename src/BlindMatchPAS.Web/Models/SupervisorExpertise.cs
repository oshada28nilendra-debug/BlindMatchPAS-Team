using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlindMatchPAS.Web.Models
{
    public class SupervisorExpertise
    {
        [Required]
        public string SupervisorId { get; set; } = string.Empty;

        [Required]
        public int ResearchAreaId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(SupervisorId))]
        public virtual ApplicationUser? Supervisor { get; set; }

        [ForeignKey(nameof(ResearchAreaId))]
        public virtual ResearchArea? ResearchArea { get; set; }
    }
}

using BlindMatchPAS.Web.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlindMatchPAS.Web.Models
{
    public class Match
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public string SupervisorId { get; set; } = string.Empty;

        [Display(Name = "Match Status")]
        public MatchStatus Status { get; set; } = MatchStatus.Interested;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? RevealedAt { get; set; }

        [StringLength(1000)]
        [Display(Name = "Supervisor Notes")]
        public string? SupervisorNotes { get; set; }

        // Navigation
        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        [ForeignKey(nameof(SupervisorId))]
        public virtual ApplicationUser? Supervisor { get; set; }
    }
}

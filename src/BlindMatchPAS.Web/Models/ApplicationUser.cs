using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Department / Faculty")]
        [StringLength(200)]
        public string? Department { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        public virtual ICollection<Match> SupervisedMatches { get; set; } = new List<Match>();
        public virtual ICollection<SupervisorExpertise> Expertises { get; set; } = new List<SupervisorExpertise>();
    }
}

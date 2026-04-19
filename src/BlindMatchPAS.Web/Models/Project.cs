using BlindMatchPAS.Web.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlindMatchPAS.Web.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project title is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        [Display(Name = "Project Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Abstract is required.")]
        [StringLength(2000, MinimumLength = 50, ErrorMessage = "Abstract must be between 50 and 2000 characters.")]
        [Display(Name = "Abstract")]
        public string Abstract { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tech stack is required.")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Tech stack must be between 2 and 500 characters.")]
        [Display(Name = "Technology Stack")]
        public string TechStack { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a research area.")]
        [Display(Name = "Research Area")]
        public int ResearchAreaId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(ResearchAreaId))]
        public virtual ResearchArea? ResearchArea { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual ApplicationUser? Student { get; set; }

        public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}

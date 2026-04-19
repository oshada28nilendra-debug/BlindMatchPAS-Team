using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.Models
{
    public class ResearchArea
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Research area name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 150 characters.")]
        [Display(Name = "Research Area")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        public virtual ICollection<SupervisorExpertise> SupervisorExpertises { get; set; } = new List<SupervisorExpertise>();
    }
}

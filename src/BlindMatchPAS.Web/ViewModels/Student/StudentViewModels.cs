using BlindMatchPAS.Web.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.ViewModels.Student
{
    public class ProjectSubmitViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project title is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        [Display(Name = "Project Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Abstract is required.")]
        [StringLength(2000, MinimumLength = 50, ErrorMessage = "Abstract must be between 50 and 2000 characters.")]
        [Display(Name = "Abstract")]
        [DataType(DataType.MultilineText)]
        public string Abstract { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tech stack is required.")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Tech stack must be between 2 and 500 characters.")]
        [Display(Name = "Technology Stack")]
        public string TechStack { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a research area.")]
        [Display(Name = "Research Area")]
        public int ResearchAreaId { get; set; }

        // Populated from DB for the dropdown
        public List<Models.ResearchArea> ResearchAreas { get; set; } = new();
    }

    public class ProjectListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ResearchAreaName { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasActiveMatch { get; set; }
        public string? SupervisorName { get; set; }
        public string? SupervisorEmail { get; set; }
        public string? SupervisorDepartment { get; set; }
        public bool IsRevealed { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public List<ProjectListItemViewModel> Projects { get; set; } = new();
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int MatchedCount { get; set; }
    }
}

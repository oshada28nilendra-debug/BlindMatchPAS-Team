using BlindMatchPAS.Web.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.ViewModels.Supervisor
{
    public class AnonymousProjectViewModel
    {
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string TechStack { get; set; } = string.Empty;
        public string ResearchAreaName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public bool AlreadyExpressedInterest { get; set; }
        public int MatchId { get; set; }
        public MatchStatus? CurrentMatchStatus { get; set; }
    }

    public class SupervisorDashboardViewModel
    {
        public List<AnonymousProjectViewModel> BrowseableProjects { get; set; } = new();
        public List<MatchListItemViewModel> MyMatches { get; set; } = new();
        public List<Models.ResearchArea> AllResearchAreas { get; set; } = new();
        public List<int> SelectedAreaIds { get; set; } = new();
        public int InterestedCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int RevealedCount { get; set; }
    }

    public class MatchListItemViewModel
    {
        public int MatchId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string ResearchAreaName { get; set; } = string.Empty;
        public MatchStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        // Revealed only after Confirmed
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
        public bool IsRevealed { get; set; }
        public string? Notes { get; set; }
    }

    public class ExpressInterestViewModel
    {
        [Required]
        public int ProjectId { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notes / Comments")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }
    }

    public class SupervisorExpertiseViewModel
    {
        public List<Models.ResearchArea> AllResearchAreas { get; set; } = new();
        public List<int> SelectedResearchAreaIds { get; set; } = new();
    }
}

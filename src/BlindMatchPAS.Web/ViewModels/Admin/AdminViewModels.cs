using BlindMatchPAS.Web.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Web.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalSupervisors { get; set; }
        public int TotalProjects { get; set; }
        public int PendingProjects { get; set; }
        public int UnderReviewProjects { get; set; }
        public int MatchedProjects { get; set; }
        public int TotalMatches { get; set; }
        public int ConfirmedMatches { get; set; }
        public List<MatchSummaryViewModel> RecentMatches { get; set; } = new();
    }

    public class MatchSummaryViewModel
    {
        public int MatchId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public string SupervisorEmail { get; set; } = string.Empty;
        public string ResearchAreaName { get; set; } = string.Empty;
        public MatchStatus MatchStatus { get; set; }
        public ProjectStatus ProjectStatus { get; set; }
        public DateTime MatchCreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }

    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Department { get; set; }
    }

    public class ReassignProjectViewModel
    {
        public int MatchId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;

        // Bound from the select element — validated manually in the controller
        public string NewSupervisorId { get; set; } = string.Empty;

        // NOT bound from POST body — repopulated server-side on every request
        // No validation attributes: prevents cascading ModelState failures
        [ValidateNever]
        public List<Models.ApplicationUser> AvailableSupervisors { get; set; } = new();
    }
}

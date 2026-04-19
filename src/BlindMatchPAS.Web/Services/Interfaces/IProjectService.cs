using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;

namespace BlindMatchPAS.Web.Services.Interfaces
{
    public interface IProjectService
    {
        Task<Project> CreateProjectAsync(string studentId, string title, string abstractText, string techStack, int researchAreaId);
        Task<Project?> GetProjectByIdAsync(int projectId);
        Task<IEnumerable<Project>> GetProjectsByStudentAsync(string studentId);
        Task<IEnumerable<Project>> GetAllProjectsAsync();
        Task<IEnumerable<Project>> GetProjectsByStatusAsync(ProjectStatus status);
        Task<IEnumerable<Project>> GetAnonymousProjectsForSupervisorAsync(string supervisorId, List<int>? areaFilter = null);
        Task<bool> UpdateProjectAsync(int projectId, string studentId, string title, string abstractText, string techStack, int researchAreaId);
        Task<bool> WithdrawProjectAsync(int projectId, string studentId);
        Task<bool> UpdateProjectStatusAsync(int projectId, ProjectStatus status);
    }
}

using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;
using BlindMatchPAS.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Web.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Project> CreateProjectAsync(string studentId, string title, string abstractText,
            string techStack, int researchAreaId)
        {
            var project = new Project
            {
                StudentId = studentId,
                Title = title,
                Abstract = abstractText,
                TechStack = techStack,
                ResearchAreaId = researchAreaId,
                Status = ProjectStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectId} created by student {StudentId}", project.Id, studentId);
            return project;
        }

        public async Task<Project?> GetProjectByIdAsync(int projectId)
        {
            return await _context.Projects
                .Include(p => p.ResearchArea)
                .Include(p => p.Student)
                .Include(p => p.Matches)
                    .ThenInclude(m => m.Supervisor)
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<IEnumerable<Project>> GetProjectsByStudentAsync(string studentId)
        {
            return await _context.Projects
                .Include(p => p.ResearchArea)
                .Include(p => p.Matches)
                    .ThenInclude(m => m.Supervisor)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.ResearchArea)
                .Include(p => p.Student)
                .Include(p => p.Matches)
                    .ThenInclude(m => m.Supervisor)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsByStatusAsync(ProjectStatus status)
        {
            return await _context.Projects
                .Include(p => p.ResearchArea)
                .Include(p => p.Student)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetAnonymousProjectsForSupervisorAsync(
            string supervisorId, List<int>? areaFilter = null)
        {
            // Blind matching: return projects the supervisor has NOT yet matched with,
            // that are Pending or UnderReview, filtered by research area if specified.
            var matchedProjectIds = await _context.Matches
                .Where(m => m.SupervisorId == supervisorId)
                .Select(m => m.ProjectId)
                .ToListAsync();

            var query = _context.Projects
                .Include(p => p.ResearchArea)
                .Where(p => p.Status == ProjectStatus.Pending || p.Status == ProjectStatus.UnderReview)
                .Where(p => !matchedProjectIds.Contains(p.Id));

            if (areaFilter != null && areaFilter.Any())
            {
                query = query.Where(p => areaFilter.Contains(p.ResearchAreaId));
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<bool> UpdateProjectAsync(int projectId, string studentId, string title,
            string abstractText, string techStack, int researchAreaId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.StudentId != studentId)
                return false;

            // Only allow editing if still Pending
            if (project.Status != ProjectStatus.Pending)
                return false;

            project.Title = title;
            project.Abstract = abstractText;
            project.TechStack = techStack;
            project.ResearchAreaId = researchAreaId;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> WithdrawProjectAsync(int projectId, string studentId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.StudentId != studentId)
                return false;

            if (project.Status == ProjectStatus.Matched)
                return false; // Cannot withdraw a matched project

            project.Status = ProjectStatus.Withdrawn;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectId} withdrawn by student {StudentId}", projectId, studentId);
            return true;
        }

        public async Task<bool> UpdateProjectStatusAsync(int projectId, ProjectStatus status)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return false;

            project.Status = status;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

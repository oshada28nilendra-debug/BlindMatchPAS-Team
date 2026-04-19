using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;
using BlindMatchPAS.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Web.Services
{
    public class MatchService : IMatchService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MatchService> _logger;

        public MatchService(ApplicationDbContext context, ILogger<MatchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Match?> ExpressInterestAsync(string supervisorId, int projectId, string? notes = null)
        {
            // Guard: ensure project exists and is open
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.Status == ProjectStatus.Matched || project.Status == ProjectStatus.Withdrawn)
            {
                _logger.LogWarning("Supervisor {SupervisorId} tried to express interest in ineligible project {ProjectId}", supervisorId, projectId);
                return null;
            }

            // Guard: no duplicate interest
            var exists = await _context.Matches
                .AnyAsync(m => m.SupervisorId == supervisorId && m.ProjectId == projectId);
            if (exists) return null;

            var match = new Match
            {
                ProjectId = projectId,
                SupervisorId = supervisorId,
                Status = MatchStatus.Interested,
                SupervisorNotes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Matches.Add(match);

            // Update project status to UnderReview when first supervisor expresses interest
            if (project.Status == ProjectStatus.Pending)
            {
                project.Status = ProjectStatus.UnderReview;
                project.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Supervisor {SupervisorId} expressed interest in project {ProjectId}", supervisorId, projectId);
            return match;
        }

        public async Task<Match?> ConfirmMatchAsync(int matchId, string supervisorId)
        {
            var match = await _context.Matches
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == matchId && m.SupervisorId == supervisorId);

            if (match == null || match.Status != MatchStatus.Interested)
                return null;

            // Transition: Interested → Confirmed → Revealed
            match.Status = MatchStatus.Confirmed;
            match.ConfirmedAt = DateTime.UtcNow;

            // Immediately reveal identity to both parties
            match.Status = MatchStatus.Revealed;
            match.RevealedAt = DateTime.UtcNow;

            // Update project status to Matched
            if (match.Project != null)
            {
                match.Project.Status = ProjectStatus.Matched;
                match.Project.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Match {MatchId} confirmed and identities revealed", matchId);
            return match;
        }

        public async Task<Match?> GetMatchByIdAsync(int matchId)
        {
            return await _context.Matches
                .Include(m => m.Project)
                    .ThenInclude(p => p!.Student)
                .Include(m => m.Project)
                    .ThenInclude(p => p!.ResearchArea)
                .Include(m => m.Supervisor)
                .FirstOrDefaultAsync(m => m.Id == matchId);
        }

        public async Task<IEnumerable<Match>> GetMatchesForSupervisorAsync(string supervisorId)
        {
            return await _context.Matches
                .Include(m => m.Project)
                    .ThenInclude(p => p!.ResearchArea)
                .Include(m => m.Project)
                    .ThenInclude(p => p!.Student)
                .Where(m => m.SupervisorId == supervisorId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Match>> GetMatchesForProjectAsync(int projectId)
        {
            return await _context.Matches
                .Include(m => m.Supervisor)
                .Where(m => m.ProjectId == projectId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Match>> GetAllMatchesAsync()
        {
            return await _context.Matches
                .Include(m => m.Project)
                    .ThenInclude(p => p!.Student)
                .Include(m => m.Project)
                    .ThenInclude(p => p!.ResearchArea)
                .Include(m => m.Supervisor)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ReassignMatchAsync(int matchId, string newSupervisorId, string adminId)
        {
            var match = await _context.Matches
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null) return false;

            var newSupervisor = await _context.Users.FindAsync(newSupervisorId);
            if (newSupervisor == null) return false;

            // Check if new supervisor already has a match with this project
            var conflict = await _context.Matches
                .AnyAsync(m => m.ProjectId == match.ProjectId && m.SupervisorId == newSupervisorId && m.Id != matchId);
            if (conflict) return false;

            match.SupervisorId = newSupervisorId;
            match.Status = MatchStatus.Interested; // Reset to Interested for re-confirmation
            match.ConfirmedAt = null;
            match.RevealedAt = null;

            if (match.Project != null && match.Project.Status == ProjectStatus.Matched)
            {
                match.Project.Status = ProjectStatus.UnderReview;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} reassigned match {MatchId} to supervisor {SupervisorId}", adminId, matchId, newSupervisorId);
            return true;
        }

        public async Task<bool> HasSupervisorExpressedInterestAsync(string supervisorId, int projectId)
        {
            return await _context.Matches
                .AnyAsync(m => m.SupervisorId == supervisorId && m.ProjectId == projectId);
        }
    }
}

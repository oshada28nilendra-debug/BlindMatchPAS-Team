using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;
using BlindMatchPAS.Web.Services.Interfaces;
using BlindMatchPAS.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Web.Controllers
{
    [Authorize(Roles = $"{Roles.ModuleLeader},{Roles.SystemAdmin}")]
    public class AdminController : Controller
    {
        private readonly IMatchService _matchService;
        private readonly IProjectService _projectService;
        private readonly IResearchAreaService _researchAreaService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _db;

        public AdminController(
            IMatchService matchService,
            IProjectService projectService,
            IResearchAreaService researchAreaService,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger,
            ApplicationDbContext db)
        {
            _matchService = matchService;
            _projectService = projectService;
            _researchAreaService = researchAreaService;
            _userManager = userManager;
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Dashboard()
        {
            var allProjects = (await _projectService.GetAllProjectsAsync()).ToList();
            var allMatches = (await _matchService.GetAllMatchesAsync()).ToList();
            var allUsers = _userManager.Users.ToList();

            var students = new List<ApplicationUser>();
            var supervisors = new List<ApplicationUser>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(Roles.Student)) students.Add(user);
                if (roles.Contains(Roles.Supervisor)) supervisors.Add(user);
            }

            var recentMatches = allMatches.Take(10).Select(m => new MatchSummaryViewModel
            {
                MatchId = m.Id,
                ProjectId = m.ProjectId,
                ProjectTitle = m.Project?.Title ?? "—",
                StudentName = m.Project?.Student?.FullName ?? "—",
                StudentEmail = m.Project?.Student?.Email ?? "—",
                SupervisorName = m.Supervisor?.FullName ?? "—",
                SupervisorEmail = m.Supervisor?.Email ?? "—",
                ResearchAreaName = m.Project?.ResearchArea?.Name ?? "—",
                MatchStatus = m.Status,
                ProjectStatus = m.Project?.Status ?? ProjectStatus.Pending,
                MatchCreatedAt = m.CreatedAt,
                ConfirmedAt = m.ConfirmedAt
            }).ToList();

            var vm = new AdminDashboardViewModel
            {
                TotalStudents = students.Count,
                TotalSupervisors = supervisors.Count,
                TotalProjects = allProjects.Count,
                PendingProjects = allProjects.Count(p => p.Status == ProjectStatus.Pending),
                UnderReviewProjects = allProjects.Count(p => p.Status == ProjectStatus.UnderReview),
                MatchedProjects = allProjects.Count(p => p.Status == ProjectStatus.Matched),
                TotalMatches = allMatches.Count,
                ConfirmedMatches = allMatches.Count(m => m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Revealed),
                RecentMatches = recentMatches
            };

            return View(vm);
        }

        // ── Research Areas ──────────────────────────────────────
        public async Task<IActionResult> ResearchAreas()
        {
            var areas = await _researchAreaService.GetAllAsync();
            return View(areas);
        }

        [HttpGet]
        public IActionResult CreateResearchArea() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResearchArea(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("name", "Name is required.");
                return View();
            }

            await _researchAreaService.CreateAsync(name.Trim(), description?.Trim());
            TempData["Success"] = $"Research area '{name}' created.";
            return RedirectToAction(nameof(ResearchAreas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleResearchArea(int id, bool isActive)
        {
            var area = await _researchAreaService.GetByIdAsync(id);
            if (area == null) return NotFound();

            await _researchAreaService.UpdateAsync(id, area.Name, area.Description, isActive);
            TempData["Success"] = $"Research area '{area.Name}' {(isActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(ResearchAreas));
        }

        // ── Users ──────────────────────────────────────────────
        public async Task<IActionResult> Users()
        {
            var allUsers = _userManager.Users.ToList();
            var userVms = new List<UserListViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userVms.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "—",
                    Role = string.Join(", ", roles),
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Department = user.Department
                });
            }

            return View(userVms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(string userId, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsActive = isActive;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"User '{user.FullName}' {(isActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Users));
        }

        // ── Matches / Reassignment ─────────────────────────────
        public async Task<IActionResult> AllMatches()
        {
            var matches = await _matchService.GetAllMatchesAsync();
            var vms = matches.Select(m => new MatchSummaryViewModel
            {
                MatchId = m.Id,
                ProjectId = m.ProjectId,
                ProjectTitle = m.Project?.Title ?? "—",
                StudentName = m.Project?.Student?.FullName ?? "—",
                StudentEmail = m.Project?.Student?.Email ?? "—",
                SupervisorName = m.Supervisor?.FullName ?? "—",
                SupervisorEmail = m.Supervisor?.Email ?? "—",
                ResearchAreaName = m.Project?.ResearchArea?.Name ?? "—",
                MatchStatus = m.Status,
                ProjectStatus = m.Project?.Status ?? ProjectStatus.Pending,
                MatchCreatedAt = m.CreatedAt,
                ConfirmedAt = m.ConfirmedAt
            }).ToList();

            return View(vms);
        }

        [HttpGet]
        public async Task<IActionResult> Reassign(int matchId)
        {
            var match = await _matchService.GetMatchByIdAsync(matchId);
            if (match == null) return NotFound();

            var allSupervisors = new List<ApplicationUser>();
            foreach (var user in _userManager.Users.ToList())
            {
                if (await _userManager.IsInRoleAsync(user, Roles.Supervisor))
                    allSupervisors.Add(user);
            }

            var vm = new ReassignProjectViewModel
            {
                MatchId = matchId,
                ProjectId = match.ProjectId,
                ProjectTitle = match.Project?.Title ?? "—",
                AvailableSupervisors = allSupervisors
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(ReassignProjectViewModel model)
        {
            Console.WriteLine($"[Reassign POST] MatchId={model.MatchId} NewSupervisorId='{model.NewSupervisorId}'");
            _logger.LogInformation("[Reassign POST] MatchId={MatchId} NewSupervisorId='{NewSupervisorId}'",
                model.MatchId, model.NewSupervisorId);

            // ── 1. Manual validation only (bypass ModelState completely) ─────────
            if (model.MatchId <= 0)
            {
                TempData["Error"] = "Invalid match ID.";
                return RedirectToAction(nameof(AllMatches));
            }

            if (string.IsNullOrWhiteSpace(model.NewSupervisorId))
            {
                TempData["Error"] = "Please select a supervisor before confirming the reassignment.";
                // Re-load the page properly via GET
                return RedirectToAction(nameof(Reassign), new { matchId = model.MatchId });
            }

            // ── 2. Direct EF Core — find and update ───────────────────────────────
            try
            {
                var match = await _db.Matches
                    .Include(m => m.Project)
                    .FirstOrDefaultAsync(m => m.Id == model.MatchId);

                if (match == null)
                {
                    Console.WriteLine($"[Reassign POST] Match {model.MatchId} not found in database.");
                    TempData["Error"] = $"Match #{model.MatchId} was not found.";
                    return RedirectToAction(nameof(AllMatches));
                }

                Console.WriteLine($"[Reassign POST] Found match. Current SupervisorId='{match.SupervisorId}' Status={match.Status}");

                // Verify the new supervisor exists
                var newSupervisor = await _db.Users.FindAsync(model.NewSupervisorId);
                if (newSupervisor == null)
                {
                    Console.WriteLine($"[Reassign POST] Supervisor '{model.NewSupervisorId}' not found.");
                    TempData["Error"] = "Selected supervisor account was not found.";
                    return RedirectToAction(nameof(Reassign), new { matchId = model.MatchId });
                }

                // Check for duplicate match (same supervisor already matched to same project)
                var conflict = await _db.Matches
                    .AnyAsync(m => m.ProjectId == match.ProjectId
                               && m.SupervisorId == model.NewSupervisorId
                               && m.Id != model.MatchId);

                if (conflict)
                {
                    Console.WriteLine($"[Reassign POST] Conflict: supervisor already has a match for project {match.ProjectId}.");
                    TempData["Error"] = $"Supervisor '{newSupervisor.FullName}' already has a match for this project. Please choose a different supervisor.";
                    return RedirectToAction(nameof(Reassign), new { matchId = model.MatchId });
                }

                // ── Apply the reassignment ────────────────────────────────────────
                match.SupervisorId = model.NewSupervisorId;
                match.Status = MatchStatus.Interested;
                match.ConfirmedAt = null;
                match.RevealedAt = null;

                if (match.Project != null && match.Project.Status == ProjectStatus.Matched)
                {
                    match.Project.Status = ProjectStatus.UnderReview;
                    match.Project.UpdatedAt = DateTime.UtcNow;
                }

                var rows = await _db.SaveChangesAsync();
                Console.WriteLine($"[Reassign POST] SaveChangesAsync complete. {rows} row(s) affected.");
                _logger.LogInformation("Match {MatchId} reassigned to supervisor {SupervisorId}. {Rows} rows saved.",
                    model.MatchId, model.NewSupervisorId, rows);

                TempData["Success"] = $"Match #{model.MatchId} has been successfully reassigned to {newSupervisor.FullName}. They will need to confirm the match.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Reassign POST] EXCEPTION: {ex}");
                _logger.LogError(ex, "Error reassigning match {MatchId} to supervisor {SupervisorId}",
                    model.MatchId, model.NewSupervisorId);
                TempData["Error"] = $"An error occurred during reassignment: {ex.Message}";
            }

            return RedirectToAction(nameof(AllMatches));
        }
    }
}

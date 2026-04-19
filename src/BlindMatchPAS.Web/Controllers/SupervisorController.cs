using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;
using BlindMatchPAS.Web.Services.Interfaces;
using BlindMatchPAS.Web.ViewModels.Supervisor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Web.Controllers
{
    [Authorize(Roles = Roles.Supervisor)]
    public class SupervisorController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IMatchService _matchService;
        private readonly IResearchAreaService _researchAreaService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SupervisorController> _logger;
        private readonly ApplicationDbContext _db;

        public SupervisorController(
            IProjectService projectService,
            IMatchService matchService,
            IResearchAreaService researchAreaService,
            UserManager<ApplicationUser> userManager,
            ILogger<SupervisorController> logger,
            ApplicationDbContext db)
        {
            _projectService = projectService;
            _matchService = matchService;
            _researchAreaService = researchAreaService;
            _userManager = userManager;
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Dashboard([FromQuery] List<int>? areas = null)
        {
            var supervisorId = _userManager.GetUserId(User)!;

            var allAreas = await _researchAreaService.GetActiveAsync();
            var supervisorAreas = await _researchAreaService.GetSupervisorExpertiseAreasAsync(supervisorId);
            var selectedIds = areas != null && areas.Any() ? areas : supervisorAreas.Select(a => a.Id).ToList();

            var projects = await _projectService.GetAnonymousProjectsForSupervisorAsync(supervisorId, selectedIds.Any() ? selectedIds : null);
            var myMatches = await _matchService.GetMatchesForSupervisorAsync(supervisorId);

            var browseable = projects.Select(p => new AnonymousProjectViewModel
            {
                ProjectId = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                TechStack = p.TechStack,
                ResearchAreaName = p.ResearchArea?.Name ?? "—",
                SubmittedAt = p.CreatedAt,
                AlreadyExpressedInterest = false // excluded by query
            }).ToList();

            var matchVms = myMatches.Select(m => new MatchListItemViewModel
            {
                MatchId = m.Id,
                ProjectId = m.ProjectId,
                ProjectTitle = m.Project?.Title ?? "—",
                ResearchAreaName = m.Project?.ResearchArea?.Name ?? "—",
                Status = m.Status,
                CreatedAt = m.CreatedAt,
                ConfirmedAt = m.ConfirmedAt,
                IsRevealed = m.Status == MatchStatus.Revealed,
                // Only expose student identity after reveal
                StudentName = m.Status == MatchStatus.Revealed ? m.Project?.Student?.FullName : null,
                StudentEmail = m.Status == MatchStatus.Revealed ? m.Project?.Student?.Email : null,
                Notes = m.SupervisorNotes
            }).ToList();

            var vm = new SupervisorDashboardViewModel
            {
                BrowseableProjects = browseable,
                MyMatches = matchVms,
                AllResearchAreas = allAreas.ToList(),
                SelectedAreaIds = selectedIds,
                InterestedCount = matchVms.Count(m => m.Status == MatchStatus.Interested),
                ConfirmedCount = matchVms.Count(m => m.Status == MatchStatus.Confirmed),
                RevealedCount = matchVms.Count(m => m.Status == MatchStatus.Revealed)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpressInterest(ExpressInterestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction(nameof(Dashboard));
            }

            var supervisorId = _userManager.GetUserId(User)!;
            var match = await _matchService.ExpressInterestAsync(supervisorId, model.ProjectId, model.Notes);

            TempData[match != null ? "Success" : "Error"] = match != null
                ? "Interest expressed! The student remains anonymous until you confirm the match."
                : "Unable to express interest. The project may no longer be available.";

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMatch(int matchId)
        {
            Console.WriteLine($"[ConfirmMatch POST] Received matchId={matchId}");
            _logger.LogInformation("[ConfirmMatch POST] matchId={MatchId}", matchId);

            // ── 1. Basic input guards ──────────────────────────────────────────
            if (matchId <= 0)
            {
                Console.WriteLine("[ConfirmMatch POST] Invalid matchId (<= 0).");
                TempData["Error"] = "Invalid match ID.";
                return RedirectToAction(nameof(Dashboard));
            }

            var supervisorId = _userManager.GetUserId(User);
            Console.WriteLine($"[ConfirmMatch POST] Resolved supervisorId='{supervisorId}'");

            if (string.IsNullOrEmpty(supervisorId))
            {
                Console.WriteLine("[ConfirmMatch POST] SupervisorId is null/empty — user not authenticated?");
                TempData["Error"] = "Unable to identify current user.";
                return RedirectToAction(nameof(Dashboard));
            }

            // ── 2. Direct EF Core — find the match ────────────────────────────
            try
            {
                // Find by matchId only first (wider query for better diagnostics)
                var match = await _db.Matches
                    .Include(m => m.Project)
                    .FirstOrDefaultAsync(m => m.Id == matchId);

                if (match == null)
                {
                    Console.WriteLine($"[ConfirmMatch POST] No match found with Id={matchId}.");
                    TempData["Error"] = $"Match #{matchId} was not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                Console.WriteLine($"[ConfirmMatch POST] Match found. SupervisorId='{match.SupervisorId}' Status={match.Status}");

                // Ownership check
                if (match.SupervisorId != supervisorId)
                {
                    Console.WriteLine($"[ConfirmMatch POST] Ownership mismatch! Match.SupervisorId='{match.SupervisorId}' vs current='{supervisorId}'");
                    TempData["Error"] = "You are not the supervisor assigned to this match.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // State check
                if (match.Status != MatchStatus.Interested)
                {
                    Console.WriteLine($"[ConfirmMatch POST] Match is not in Interested state (current: {match.Status}).");
                    TempData["Error"] = $"Match cannot be confirmed — it is currently in '{match.Status}' state.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // ── 3. Apply changes ──────────────────────────────────────────
                match.Status = MatchStatus.Revealed;   // Confirmed + Identity Revealed in one step
                match.ConfirmedAt = DateTime.UtcNow;
                match.RevealedAt = DateTime.UtcNow;

                if (match.Project != null)
                {
                    match.Project.Status = ProjectStatus.Matched;
                    match.Project.UpdatedAt = DateTime.UtcNow;
                }

                var rows = await _db.SaveChangesAsync();
                Console.WriteLine($"[ConfirmMatch POST] SaveChangesAsync complete. {rows} row(s) saved.");
                _logger.LogInformation("Match {MatchId} confirmed and revealed by supervisor {SupervisorId}. {Rows} rows saved.",
                    matchId, supervisorId, rows);

                TempData["Success"] = "Match confirmed! The student's identity has been revealed — check the match card below.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfirmMatch POST] EXCEPTION: {ex}");
                _logger.LogError(ex, "Error confirming match {MatchId} for supervisor {SupervisorId}", matchId, supervisorId);
                TempData["Error"] = $"An error occurred while confirming the match: {ex.Message}";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> ManageExpertise()
        {
            var supervisorId = _userManager.GetUserId(User)!;
            var allAreas = await _researchAreaService.GetActiveAsync();
            var myAreas = await _researchAreaService.GetSupervisorExpertiseAreasAsync(supervisorId);

            var vm = new SupervisorExpertiseViewModel
            {
                AllResearchAreas = allAreas.ToList(),
                SelectedResearchAreaIds = myAreas.Select(a => a.Id).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageExpertise(SupervisorExpertiseViewModel model)
        {
            var supervisorId = _userManager.GetUserId(User)!;
            await _researchAreaService.UpdateSupervisorExpertiseAsync(supervisorId, model.SelectedResearchAreaIds ?? new List<int>());

            TempData["Success"] = "Research area preferences updated!";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> MatchDetails(int id)
        {
            var supervisorId = _userManager.GetUserId(User)!;
            var match = await _matchService.GetMatchByIdAsync(id);

            if (match == null || match.SupervisorId != supervisorId)
                return NotFound();

            return View(match);
        }
    }
}

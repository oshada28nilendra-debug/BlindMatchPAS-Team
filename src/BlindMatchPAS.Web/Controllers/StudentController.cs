using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;
using BlindMatchPAS.Web.Services.Interfaces;
using BlindMatchPAS.Web.ViewModels.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlindMatchPAS.Web.Controllers
{
    [Authorize(Roles = Roles.Student)]
    public class StudentController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IResearchAreaService _researchAreaService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StudentController> _logger;

        public StudentController(
            IProjectService projectService,
            IResearchAreaService researchAreaService,
            UserManager<ApplicationUser> userManager,
            ILogger<StudentController> logger)
        {
            _projectService = projectService;
            _researchAreaService = researchAreaService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User)!;
            var projects = await _projectService.GetProjectsByStudentAsync(userId);

            var items = projects.Select(p => new ProjectListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                ResearchAreaName = p.ResearchArea?.Name ?? "—",
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                HasActiveMatch = p.Matches.Any(m => m.Status == MatchStatus.Revealed),
                IsRevealed = p.Matches.Any(m => m.Status == MatchStatus.Revealed),
                SupervisorName = p.Matches.FirstOrDefault(m => m.Status == MatchStatus.Revealed)?.Supervisor?.FullName,
                SupervisorEmail = p.Matches.FirstOrDefault(m => m.Status == MatchStatus.Revealed)?.Supervisor?.Email,
                SupervisorDepartment = p.Matches.FirstOrDefault(m => m.Status == MatchStatus.Revealed)?.Supervisor?.Department
            }).ToList();

            var vm = new StudentDashboardViewModel
            {
                Projects = items,
                PendingCount = items.Count(i => i.Status == ProjectStatus.Pending),
                UnderReviewCount = items.Count(i => i.Status == ProjectStatus.UnderReview),
                MatchedCount = items.Count(i => i.Status == ProjectStatus.Matched)
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Submit()
        {
            var vm = new ProjectSubmitViewModel
            {
                ResearchAreas = (await _researchAreaService.GetActiveAsync()).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ProjectSubmitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ResearchAreas = (await _researchAreaService.GetActiveAsync()).ToList();
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;
            await _projectService.CreateProjectAsync(userId, model.Title, model.Abstract, model.TechStack, model.ResearchAreaId);

            TempData["Success"] = "Your project has been submitted successfully!";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var project = await _projectService.GetProjectByIdAsync(id);

            if (project == null || project.StudentId != userId)
                return NotFound();

            if (project.Status != ProjectStatus.Pending)
            {
                TempData["Error"] = "You can only edit projects that are still pending.";
                return RedirectToAction(nameof(Dashboard));
            }

            var vm = new ProjectSubmitViewModel
            {
                Id = project.Id,
                Title = project.Title,
                Abstract = project.Abstract,
                TechStack = project.TechStack,
                ResearchAreaId = project.ResearchAreaId,
                ResearchAreas = (await _researchAreaService.GetActiveAsync()).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectSubmitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ResearchAreas = (await _researchAreaService.GetActiveAsync()).ToList();
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;
            var success = await _projectService.UpdateProjectAsync(id, userId, model.Title, model.Abstract, model.TechStack, model.ResearchAreaId);

            if (!success)
            {
                TempData["Error"] = "Unable to update project. It may no longer be editable.";
                return RedirectToAction(nameof(Dashboard));
            }

            TempData["Success"] = "Project updated successfully!";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var success = await _projectService.WithdrawProjectAsync(id, userId);

            TempData[success ? "Success" : "Error"] = success
                ? "Project withdrawn successfully."
                : "Unable to withdraw this project.";

            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> ProjectDetails(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var project = await _projectService.GetProjectByIdAsync(id);

            if (project == null || project.StudentId != userId)
                return NotFound();

            return View(project);
        }
    }
}

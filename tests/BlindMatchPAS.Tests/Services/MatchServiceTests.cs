using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;
using BlindMatchPAS.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlindMatchPAS.Tests.Services
{
    public class MatchServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MatchService _matchService;
        private readonly Mock<ILogger<MatchService>> _loggerMock;
        private readonly string _supervisorId = "sup-001";
        private readonly string _studentId = "stu-001";

        public MatchServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<MatchService>>();
            _matchService = new MatchService(_context, _loggerMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var supervisor = new ApplicationUser { Id = _supervisorId, UserName = "sup@test.com", FullName = "Test Supervisor" };
            var student = new ApplicationUser { Id = _studentId, UserName = "stu@test.com", FullName = "Test Student" };
            var area = new ResearchArea { Id = 1, Name = "AI", IsActive = true, CreatedAt = DateTime.UtcNow };

            _context.Users.AddRange(supervisor, student);
            _context.ResearchAreas.Add(area);
            _context.SaveChanges();
        }

        private Project CreateProject(ProjectStatus status = ProjectStatus.Pending)
        {
            var project = new Project
            {
                Title = "Test Project",
                Abstract = "A test abstract with more than fifty characters to pass validation.",
                TechStack = "C#, EF Core",
                ResearchAreaId = 1,
                StudentId = _studentId,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
            _context.Projects.Add(project);
            _context.SaveChanges();
            return project;
        }

        // ── Express Interest ────────────────────────────────────────

        [Fact]
        public async Task ExpressInterest_OnPendingProject_CreatesMatchAndSetsUnderReview()
        {
            var project = CreateProject(ProjectStatus.Pending);

            var match = await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            Assert.NotNull(match);
            Assert.Equal(MatchStatus.Interested, match.Status);

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.UnderReview, updatedProject!.Status);
        }

        [Fact]
        public async Task ExpressInterest_AlreadyMatched_ReturnsNull()
        {
            var project = CreateProject(ProjectStatus.Matched);

            var result = await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task ExpressInterest_Duplicate_ReturnsNull()
        {
            var project = CreateProject(ProjectStatus.Pending);
            await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            // Second attempt by the same supervisor
            var duplicate = await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            Assert.Null(duplicate);
        }

        [Fact]
        public async Task ExpressInterest_WithdrawnProject_ReturnsNull()
        {
            var project = CreateProject(ProjectStatus.Withdrawn);

            var result = await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task ExpressInterest_SecondSupervisor_DoesNotChangeStatusToUnderReview()
        {
            var project = CreateProject(ProjectStatus.Pending);
            var sup2 = new ApplicationUser { Id = "sup-002", UserName = "sup2@test.com", FullName = "Supervisor 2" };
            _context.Users.Add(sup2);
            _context.SaveChanges();

            await _matchService.ExpressInterestAsync(_supervisorId, project.Id);
            await _matchService.ExpressInterestAsync("sup-002", project.Id);

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.UnderReview, updatedProject!.Status);
        }

        // ── Confirm Match / Reveal ──────────────────────────────────

        [Fact]
        public async Task ConfirmMatch_InterestedMatch_TransitionsToRevealedAndMatchesProject()
        {
            var project = CreateProject(ProjectStatus.Pending);
            var match = await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            var confirmed = await _matchService.ConfirmMatchAsync(match!.Id, _supervisorId);

            Assert.NotNull(confirmed);
            Assert.Equal(MatchStatus.Revealed, confirmed.Status);
            Assert.NotNull(confirmed.RevealedAt);

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.Matched, updatedProject!.Status);
        }

        [Fact]
        public async Task ConfirmMatch_WrongSupervisor_ReturnsNull()
        {
            var project = CreateProject(ProjectStatus.Pending);
            var match = await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            var result = await _matchService.ConfirmMatchAsync(match!.Id, "wrong-supervisor-id");

            Assert.Null(result);
        }

        [Fact]
        public async Task ConfirmMatch_AlreadyRevealed_ReturnsNull()
        {
            var project = CreateProject(ProjectStatus.Pending);
            var match = await _matchService.ExpressInterestAsync(_supervisorId, project.Id);
            await _matchService.ConfirmMatchAsync(match!.Id, _supervisorId);

            // Try confirming again — should fail since status is already Revealed
            var result = await _matchService.ConfirmMatchAsync(match.Id, _supervisorId);

            Assert.Null(result);
        }

        // ── HasSupervisorExpressedInterest ──────────────────────────

        [Fact]
        public async Task HasSupervisorExpressedInterest_AfterInterest_ReturnsTrue()
        {
            var project = CreateProject();
            await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            var result = await _matchService.HasSupervisorExpressedInterestAsync(_supervisorId, project.Id);

            Assert.True(result);
        }

        [Fact]
        public async Task HasSupervisorExpressedInterest_BeforeInterest_ReturnsFalse()
        {
            var project = CreateProject();

            var result = await _matchService.HasSupervisorExpressedInterestAsync(_supervisorId, project.Id);

            Assert.False(result);
        }

        // ── Anonymous Project Query ─────────────────────────────────

        [Fact]
        public async Task GetAnonymousProjects_ExcludesAlreadyMatchedProjectsBySupervisor()
        {
            var projectService = new ProjectService(_context, new Mock<ILogger<ProjectService>>().Object);
            var project = CreateProject(ProjectStatus.Pending);
            await _matchService.ExpressInterestAsync(_supervisorId, project.Id);

            var results = await projectService.GetAnonymousProjectsForSupervisorAsync(_supervisorId);

            Assert.DoesNotContain(results, p => p.Id == project.Id);
        }

        public void Dispose() => _context.Dispose();
    }
}

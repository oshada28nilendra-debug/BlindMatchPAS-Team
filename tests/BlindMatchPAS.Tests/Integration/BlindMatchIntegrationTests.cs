using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;
using BlindMatchPAS.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BlindMatchPAS.Tests.Integration
{
    /// <summary>
    /// End-to-end integration tests that exercise the full service stack
    /// against an in-memory SQL-compatible EF Core provider.
    /// These simulate complete user-journey scenarios.
    /// </summary>
    public class BlindMatchIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectService _projectService;
        private readonly MatchService _matchService;
        private readonly ResearchAreaService _researchAreaService;

        // Test actors
        private const string StudentId = "stu-int-001";
        private const string SupervisorId = "sup-int-001";
        private const string Supervisor2Id = "sup-int-002";

        public BlindMatchIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _projectService = new ProjectService(_context, new Mock<ILogger<ProjectService>>().Object);
            _matchService = new MatchService(_context, new Mock<ILogger<MatchService>>().Object);
            _researchAreaService = new ResearchAreaService(_context, new Mock<ILogger<ResearchAreaService>>().Object);

            SeedActors();
        }

        private void SeedActors()
        {
            _context.Users.AddRange(
                new ApplicationUser { Id = StudentId, UserName = "student@test.com", FullName = "Test Student" },
                new ApplicationUser { Id = SupervisorId, UserName = "supervisor@test.com", FullName = "Test Supervisor" },
                new ApplicationUser { Id = Supervisor2Id, UserName = "supervisor2@test.com", FullName = "Second Supervisor" }
            );
            _context.ResearchAreas.Add(new ResearchArea { Id = 1, Name = "AI", IsActive = true, CreatedAt = DateTime.UtcNow });
            _context.SaveChanges();
        }

        // ── Journey 1: Happy Path ─────────────────────────────────

        [Fact]
        public async Task Journey_StudentSubmits_SupervisorConfirms_BothRevealed()
        {
            // Step 1 — Student submits project
            var project = await _projectService.CreateProjectAsync(
                StudentId, "AI Research", "A comprehensive abstract that exceeds the minimum fifty character limit.", "Python", 1);

            Assert.Equal(ProjectStatus.Pending, project.Status);

            // Step 2 — Supervisor browses anonymously — project IS returned, title visible
            var browseable = (await _projectService.GetAnonymousProjectsForSupervisorAsync(SupervisorId)).ToList();
            Assert.Single(browseable);
            Assert.Equal("AI Research", browseable[0].Title); // Title visible
            // (Student identity is hidden in the VIEW layer, not stripped at query level)

            // Step 3 — Supervisor expresses interest → project becomes UnderReview
            var match = await _matchService.ExpressInterestAsync(SupervisorId, project.Id, "Very interesting topic");
            Assert.NotNull(match);
            Assert.Equal(MatchStatus.Interested, match.Status);

            var projectAfterInterest = await _projectService.GetProjectByIdAsync(project.Id);
            Assert.Equal(ProjectStatus.UnderReview, projectAfterInterest!.Status);

            // Step 4 — Project no longer appears in anonymous browse for SAME supervisor
            var browseAfter = (await _projectService.GetAnonymousProjectsForSupervisorAsync(SupervisorId)).ToList();
            Assert.Empty(browseAfter);

            // Step 5 — Supervisor confirms match → Revealed status, project = Matched
            var confirmed = await _matchService.ConfirmMatchAsync(match.Id, SupervisorId);
            Assert.NotNull(confirmed);
            Assert.Equal(MatchStatus.Revealed, confirmed.Status);
            Assert.NotNull(confirmed.RevealedAt);

            var matchWithDetails = await _matchService.GetMatchByIdAsync(match.Id);
            Assert.Equal(ProjectStatus.Matched, matchWithDetails!.Project!.Status);

            // Step 6 — Student can now see supervisor details
            var studentProject = await _projectService.GetProjectByIdAsync(project.Id);
            var revealedMatch = studentProject!.Matches.FirstOrDefault(m => m.Status == MatchStatus.Revealed);
            Assert.NotNull(revealedMatch);
        }

        // ── Journey 2: Multiple Supervisors ──────────────────────

        [Fact]
        public async Task Journey_MultipleInterests_OnlyOneConfirmed()
        {
            var project = await _projectService.CreateProjectAsync(
                StudentId, "Cloud Computing", "Extended abstract about distributed systems and microservices architecture.", "Go", 1);

            var match1 = await _matchService.ExpressInterestAsync(SupervisorId, project.Id);
            var match2 = await _matchService.ExpressInterestAsync(Supervisor2Id, project.Id);

            Assert.NotNull(match1);
            Assert.NotNull(match2);

            // First supervisor confirms
            await _matchService.ConfirmMatchAsync(match1!.Id, SupervisorId);

            var finalProject = await _projectService.GetProjectByIdAsync(project.Id);
            Assert.Equal(ProjectStatus.Matched, finalProject!.Status);

            // Second supervisor's match remains Interested (not affected)
            var match2Updated = await _matchService.GetMatchByIdAsync(match2!.Id);
            Assert.Equal(MatchStatus.Interested, match2Updated!.Status);
        }

        // ── Journey 3: Student Withdraws ─────────────────────────

        [Fact]
        public async Task Journey_StudentWithdraws_BeforeInterest()
        {
            var project = await _projectService.CreateProjectAsync(
                StudentId, "Blockchain", "This abstract describes blockchain technology and smart contracts in detail.", "Solidity", 1);

            var result = await _projectService.WithdrawProjectAsync(project.Id, StudentId);

            Assert.True(result);
            var updated = await _projectService.GetProjectByIdAsync(project.Id);
            Assert.Equal(ProjectStatus.Withdrawn, updated!.Status);

            // Supervisor cannot express interest in withdrawn project
            var match = await _matchService.ExpressInterestAsync(SupervisorId, project.Id);
            Assert.Null(match);
        }

        // ── Journey 4: Research Area Filter ──────────────────────

        [Fact]
        public async Task Journey_SupervisorFilters_ByExpertiseArea()
        {
            var area2 = await _researchAreaService.CreateAsync("Cybersecurity");

            var p1 = await _projectService.CreateProjectAsync(
                StudentId, "AI Project", "Abstract for AI project that meets the minimum length requirement easily.", "Python", 1);
            var p2 = await _projectService.CreateProjectAsync(
                StudentId, "Security Project", "Abstract about network security that is long enough to meet requirements.", "C", area2.Id);

            // Supervisor sets expertise to AI only
            await _researchAreaService.UpdateSupervisorExpertiseAsync(SupervisorId, new List<int> { 1 });

            // Filter by AI area
            var filtered = (await _projectService.GetAnonymousProjectsForSupervisorAsync(SupervisorId, new List<int> { 1 })).ToList();

            Assert.Single(filtered);
            Assert.Equal(p1.Id, filtered[0].Id);
        }

        // ── Journey 5: DB Persistence Round-Trip ─────────────────

        [Fact]
        public async Task Integration_ProjectAndMatch_PersistCorrectlyOnDbReload()
        {
            var project = await _projectService.CreateProjectAsync(
                StudentId, "Persistence Test", "Testing persistence of project data across multiple context operations.", "C#", 1);

            await _matchService.ExpressInterestAsync(SupervisorId, project.Id);

            // Reload from DB
            var reloaded = await _context.Projects
                .Include(p => p.Matches)
                .Include(p => p.ResearchArea)
                .FirstOrDefaultAsync(p => p.Id == project.Id);

            Assert.NotNull(reloaded);
            Assert.Single(reloaded!.Matches);
            Assert.Equal(MatchStatus.Interested, reloaded.Matches.First().Status);
            Assert.Equal("AI", reloaded.ResearchArea!.Name);
        }

        // ── Journey 6: Guard Conditions ──────────────────────────

        [Fact]
        public async Task Guard_CannotWithdrawMatchedProject()
        {
            var project = await _projectService.CreateProjectAsync(
                StudentId, "Matched Project", "This is a project with sufficient abstract length to be valid.", "Java", 1);

            var match = await _matchService.ExpressInterestAsync(SupervisorId, project.Id);
            await _matchService.ConfirmMatchAsync(match!.Id, SupervisorId);

            var withdraw = await _projectService.WithdrawProjectAsync(project.Id, StudentId);

            Assert.False(withdraw);
        }

        [Fact]
        public async Task Guard_CannotEditUnderReviewProject()
        {
            var project = await _projectService.CreateProjectAsync(
                StudentId, "Under Review", "Abstract that is long enough for validation purposes in the test.", "Ruby", 1);

            await _matchService.ExpressInterestAsync(SupervisorId, project.Id);

            // Project is now UnderReview — edit should fail
            var edited = await _projectService.UpdateProjectAsync(
                project.Id, StudentId, "New Title", "New abstract that is long enough for validation pass.", "Python", 1);

            Assert.False(edited);
        }

        public void Dispose() => _context.Dispose();
    }
}

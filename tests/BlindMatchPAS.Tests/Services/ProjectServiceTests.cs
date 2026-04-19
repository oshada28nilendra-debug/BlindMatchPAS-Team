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
    public class ProjectServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectService _projectService;
        private readonly string _studentId = "stu-001";

        public ProjectServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            var loggerMock = new Mock<ILogger<ProjectService>>();
            _projectService = new ProjectService(_context, loggerMock.Object);

            var user = new ApplicationUser { Id = _studentId, UserName = "stu@test.com", FullName = "Test Student" };
            var area = new ResearchArea { Id = 1, Name = "AI", IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.Users.AddRange(user);
            _context.ResearchAreas.Add(area);
            _context.SaveChanges();
        }

        // ── Create ──────────────────────────────────────────────────

        [Fact]
        public async Task CreateProject_ValidData_ReturnsProjectWithPendingStatus()
        {
            var project = await _projectService.CreateProjectAsync(
                _studentId, "Test Title", "Abstract of sufficient length to meet the 50 char minimum.", "C#", 1);

            Assert.NotNull(project);
            Assert.Equal(ProjectStatus.Pending, project.Status);
            Assert.Equal(_studentId, project.StudentId);
        }

        [Fact]
        public async Task CreateProject_PersistsToDatabase()
        {
            await _projectService.CreateProjectAsync(
                _studentId, "Persisted Project", "Abstract long enough to pass validation requirements.", "Python", 1);

            var count = await _context.Projects.CountAsync();
            Assert.Equal(1, count);
        }

        // ── Get by Student ──────────────────────────────────────────

        [Fact]
        public async Task GetProjectsByStudent_ReturnsOnlyStudentsProjects()
        {
            var other = new ApplicationUser { Id = "other-stu", UserName = "other@test.com", FullName = "Other" };
            _context.Users.Add(other);
            _context.SaveChanges();

            await _projectService.CreateProjectAsync(_studentId, "Mine", "Abstract with adequate length for the project.", "C#", 1);
            await _projectService.CreateProjectAsync("other-stu", "Theirs", "Another abstract with adequate length too.", "Java", 1);

            var mine = await _projectService.GetProjectsByStudentAsync(_studentId);

            Assert.Single(mine);
            Assert.All(mine, p => Assert.Equal(_studentId, p.StudentId));
        }

        // ── Update ──────────────────────────────────────────────────

        [Fact]
        public async Task UpdateProject_PendingProject_Succeeds()
        {
            var project = await _projectService.CreateProjectAsync(
                _studentId, "Old Title", "Original abstract of adequate length to pass the validator.", "PHP", 1);

            var result = await _projectService.UpdateProjectAsync(
                project.Id, _studentId, "New Title", "Updated abstract that still passes the minimum length check.", "Go", 1);

            Assert.True(result);
            var updated = await _context.Projects.FindAsync(project.Id);
            Assert.Equal("New Title", updated!.Title);
        }

        [Fact]
        public async Task UpdateProject_NonPendingProject_Fails()
        {
            var project = await _projectService.CreateProjectAsync(
                _studentId, "Title", "Abstract that is long enough to meet the fifty-char minimum.", "C#", 1);
            await _projectService.UpdateProjectStatusAsync(project.Id, ProjectStatus.UnderReview);

            var result = await _projectService.UpdateProjectAsync(
                project.Id, _studentId, "Changed Title", "Changed abstract string that is long enough for validation.", "Go", 1);

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateProject_WrongStudent_Fails()
        {
            var project = await _projectService.CreateProjectAsync(
                _studentId, "Title", "Abstract that is long enough to meet length requirements here.", "C#", 1);

            var result = await _projectService.UpdateProjectAsync(
                project.Id, "wrong-student", "Hacker Title", "Hacker abstract that is still long enough for rules.", "Go", 1);

            Assert.False(result);
        }

        // ── Withdraw ────────────────────────────────────────────────

        [Fact]
        public async Task WithdrawProject_PendingProject_SetsWithdrawnStatus()
        {
            var project = await _projectService.CreateProjectAsync(
                _studentId, "Withdraw Me", "Abstract of suitable length for the project withdrawal test.", "JS", 1);

            var result = await _projectService.WithdrawProjectAsync(project.Id, _studentId);

            Assert.True(result);
            var updated = await _context.Projects.FindAsync(project.Id);
            Assert.Equal(ProjectStatus.Withdrawn, updated!.Status);
        }

        [Fact]
        public async Task WithdrawProject_MatchedProject_Fails()
        {
            var project = await _projectService.CreateProjectAsync(
                _studentId, "Matched", "Abstract of sufficient length to validate the project creation.", "C#", 1);
            await _projectService.UpdateProjectStatusAsync(project.Id, ProjectStatus.Matched);

            var result = await _projectService.WithdrawProjectAsync(project.Id, _studentId);

            Assert.False(result);
        }

        [Fact]
        public async Task WithdrawProject_WrongStudent_Fails()
        {
            var project = await _projectService.CreateProjectAsync(
                _studentId, "Not Yours", "Abstract length is adequate for the project creation requirements.", "C#", 1);

            var result = await _projectService.WithdrawProjectAsync(project.Id, "attacker-id");

            Assert.False(result);
        }

        // ── Status Transitions ──────────────────────────────────────

        [Theory]
        [InlineData(ProjectStatus.Pending)]
        [InlineData(ProjectStatus.UnderReview)]
        [InlineData(ProjectStatus.Matched)]
        [InlineData(ProjectStatus.Withdrawn)]
        public async Task UpdateProjectStatus_AllValidStatuses_Succeeds(ProjectStatus status)
        {
            var project = await _projectService.CreateProjectAsync(
                _studentId, "Status Test", "Abstract that satisfies the minimum length of fifty characters.", "C#", 1);

            var result = await _projectService.UpdateProjectStatusAsync(project.Id, status);

            Assert.True(result);
            var updated = await _context.Projects.FindAsync(project.Id);
            Assert.Equal(status, updated!.Status);
        }

        // ── Anonymous Browsing ──────────────────────────────────────

        [Fact]
        public async Task GetAnonymousProjects_FiltersByResearchArea()
        {
            var area2 = new ResearchArea { Id = 2, Name = "Cybersecurity", IsActive = true, CreatedAt = DateTime.UtcNow };
            _context.ResearchAreas.Add(area2);
            _context.SaveChanges();

            await _projectService.CreateProjectAsync(_studentId, "AI Project", "A sufficiently long abstract for the AI project test case here.", "Python", 1);
            await _projectService.CreateProjectAsync(_studentId, "Cyber Project", "A sufficiently long abstract for the cyber project test case.", "C", 2);

            var supId = "sup-filter";
            _context.Users.Add(new ApplicationUser { Id = supId, UserName = "sup@test.com", FullName = "Supervisor" });
            _context.SaveChanges();

            var results = (await _projectService.GetAnonymousProjectsForSupervisorAsync(supId, new List<int> { 1 })).ToList();

            Assert.Single(results);
            Assert.All(results, p => Assert.Equal(1, p.ResearchAreaId));
        }

        public void Dispose() => _context.Dispose();
    }
}

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
    /// <summary>
    /// Integration tests for ResearchAreaService — full DB round-trips with InMemory provider.
    /// </summary>
    public class ResearchAreaServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ResearchAreaService _service;

        public ResearchAreaServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new ResearchAreaService(_context, new Mock<ILogger<ResearchAreaService>>().Object);
        }

        [Fact]
        public async Task CreateAsync_ValidName_PersistsAndReturnsArea()
        {
            var area = await _service.CreateAsync("Machine Learning", "Deep learning and neural networks");

            Assert.NotNull(area);
            Assert.Equal("Machine Learning", area.Name);
            Assert.True(area.IsActive);
            Assert.Equal(1, await _context.ResearchAreas.CountAsync());
        }

        [Fact]
        public async Task GetActiveAsync_ReturnsOnlyActiveAreas()
        {
            await _service.CreateAsync("Active Area");
            var inactive = await _service.CreateAsync("Inactive Area");
            await _service.UpdateAsync(inactive.Id, inactive.Name, null, false);

            var active = await _service.GetActiveAsync();

            Assert.Single(active);
            Assert.All(active, a => Assert.True(a.IsActive));
        }

        [Fact]
        public async Task UpdateAsync_ExistingArea_UpdatesAllFields()
        {
            var area = await _service.CreateAsync("Old Name", "Old desc");

            var result = await _service.UpdateAsync(area.Id, "New Name", "New desc", false);

            Assert.True(result);
            var updated = await _context.ResearchAreas.FindAsync(area.Id);
            Assert.Equal("New Name", updated!.Name);
            Assert.Equal("New desc", updated.Description);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task UpdateAsync_NonExistentId_ReturnsFalse()
        {
            var result = await _service.UpdateAsync(9999, "Name", null, true);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_AreaNotInUse_HardDeletes()
        {
            var area = await _service.CreateAsync("Unused Area");

            var result = await _service.DeleteAsync(area.Id);

            Assert.True(result);
            Assert.Equal(0, await _context.ResearchAreas.CountAsync());
        }

        [Fact]
        public async Task DeleteAsync_AreaInUse_SoftDeletes()
        {
            var area = await _service.CreateAsync("Used Area");
            var student = new ApplicationUser { Id = "stu-1", UserName = "s@test.com", FullName = "Student" };
            _context.Users.Add(student);
            _context.Projects.Add(new Project
            {
                Title = "Test",
                Abstract = "Abstract that is long enough to pass the minimum length requirement.",
                TechStack = "C#",
                ResearchAreaId = area.Id,
                StudentId = "stu-1",
                Status = ProjectStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteAsync(area.Id);

            Assert.True(result);
            var remaining = await _context.ResearchAreas.FindAsync(area.Id);
            Assert.NotNull(remaining);        // still exists
            Assert.False(remaining!.IsActive); // but deactivated
        }

        [Fact]
        public async Task UpdateSupervisorExpertise_ReplacesExistingEntries()
        {
            var area1 = await _service.CreateAsync("AI");
            var area2 = await _service.CreateAsync("Cybersecurity");
            var area3 = await _service.CreateAsync("IoT");
            var supervisor = new ApplicationUser { Id = "sup-1", UserName = "sup@test.com", FullName = "Supervisor" };
            _context.Users.Add(supervisor);
            await _context.SaveChangesAsync();

            // Set initial expertise
            await _service.UpdateSupervisorExpertiseAsync("sup-1", new List<int> { area1.Id, area2.Id });

            // Replace with different set
            await _service.UpdateSupervisorExpertiseAsync("sup-1", new List<int> { area3.Id });

            var expertise = (await _service.GetSupervisorExpertiseAreasAsync("sup-1")).ToList();
            Assert.Single(expertise);
            Assert.Equal("IoT", expertise[0].Name);
        }

        [Fact]
        public async Task GetSupervisorExpertiseAreas_NoExpertise_ReturnsEmpty()
        {
            var result = await _service.GetSupervisorExpertiseAreasAsync("sup-nobody");
            Assert.Empty(result);
        }

        public void Dispose() => _context.Dispose();
    }
}

using BlindMatchPAS.Web.Data;
using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Web.Services
{
    public class ResearchAreaService : IResearchAreaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResearchAreaService> _logger;

        public ResearchAreaService(ApplicationDbContext context, ILogger<ResearchAreaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ResearchArea>> GetAllAsync()
        {
            return await _context.ResearchAreas.OrderBy(r => r.Name).ToListAsync();
        }

        public async Task<IEnumerable<ResearchArea>> GetActiveAsync()
        {
            return await _context.ResearchAreas.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        }

        public async Task<ResearchArea?> GetByIdAsync(int id)
        {
            return await _context.ResearchAreas.FindAsync(id);
        }

        public async Task<ResearchArea> CreateAsync(string name, string? description = null)
        {
            var area = new ResearchArea
            {
                Name = name,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.ResearchAreas.Add(area);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Research area '{Name}' created", name);
            return area;
        }

        public async Task<bool> UpdateAsync(int id, string name, string? description, bool isActive)
        {
            var area = await _context.ResearchAreas.FindAsync(id);
            if (area == null) return false;

            area.Name = name;
            area.Description = description;
            area.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var area = await _context.ResearchAreas.FindAsync(id);
            if (area == null) return false;

            // Check if in use
            var inUse = await _context.Projects.AnyAsync(p => p.ResearchAreaId == id);
            if (inUse)
            {
                area.IsActive = false; // Soft delete
            }
            else
            {
                _context.ResearchAreas.Remove(area);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ResearchArea>> GetSupervisorExpertiseAreasAsync(string supervisorId)
        {
            return await _context.SupervisorExpertises
                .Where(se => se.SupervisorId == supervisorId)
                .Include(se => se.ResearchArea)
                .Select(se => se.ResearchArea!)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<bool> UpdateSupervisorExpertiseAsync(string supervisorId, List<int> researchAreaIds)
        {
            var existing = await _context.SupervisorExpertises
                .Where(se => se.SupervisorId == supervisorId)
                .ToListAsync();

            _context.SupervisorExpertises.RemoveRange(existing);

            var newEntries = researchAreaIds.Select(areaId => new SupervisorExpertise
            {
                SupervisorId = supervisorId,
                ResearchAreaId = areaId,
                AddedAt = DateTime.UtcNow
            });

            _context.SupervisorExpertises.AddRange(newEntries);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

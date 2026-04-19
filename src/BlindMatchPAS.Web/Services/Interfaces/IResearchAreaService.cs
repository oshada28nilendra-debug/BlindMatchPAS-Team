using BlindMatchPAS.Web.Models;

namespace BlindMatchPAS.Web.Services.Interfaces
{
    public interface IResearchAreaService
    {
        Task<IEnumerable<ResearchArea>> GetAllAsync();
        Task<IEnumerable<ResearchArea>> GetActiveAsync();
        Task<ResearchArea?> GetByIdAsync(int id);
        Task<ResearchArea> CreateAsync(string name, string? description = null);
        Task<bool> UpdateAsync(int id, string name, string? description, bool isActive);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<ResearchArea>> GetSupervisorExpertiseAreasAsync(string supervisorId);
        Task<bool> UpdateSupervisorExpertiseAsync(string supervisorId, List<int> researchAreaIds);
    }
}

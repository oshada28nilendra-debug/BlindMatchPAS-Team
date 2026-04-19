using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;

namespace BlindMatchPAS.Web.Services.Interfaces
{
    public interface IMatchService
    {
        Task<Match?> ExpressInterestAsync(string supervisorId, int projectId, string? notes = null);
        Task<Match?> ConfirmMatchAsync(int matchId, string supervisorId);
        Task<Match?> GetMatchByIdAsync(int matchId);
        Task<IEnumerable<Match>> GetMatchesForSupervisorAsync(string supervisorId);
        Task<IEnumerable<Match>> GetMatchesForProjectAsync(int projectId);
        Task<IEnumerable<Match>> GetAllMatchesAsync();
        Task<bool> ReassignMatchAsync(int matchId, string newSupervisorId, string adminId);
        Task<bool> HasSupervisorExpressedInterestAsync(string supervisorId, int projectId);
    }
}

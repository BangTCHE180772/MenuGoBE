using System.Threading.Tasks;
using MenuGoBE.Dtos.Dashboard;

namespace MenuGoBE.Interface.Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync(string range, string? startDateStr, string? endDateStr, long? branchId);
    }
}

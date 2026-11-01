using FruitSeller.Application.Dtos;

namespace FruitSeller.Application.Services;

public interface IReportService
{
    Task<ReportDto> GetReportAsync(DateOnly? fromDate, DateOnly? toDate);
    Task<DashboardDto> GetDashboardAsync();
}

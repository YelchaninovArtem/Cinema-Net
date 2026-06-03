namespace Cinema.Application.Reports;

public sealed record SalesReportItem(
    string Date,
    int TotalBookings,
    decimal TotalRevenue
);

public sealed record OccupancyReportItem(
    string HallName,
    string MovieTitle,
    string Date,
    int OccupiedSeats,
    int TotalSeats,
    double OccupancyPercent
);

public interface IReportService
{
    Task<IReadOnlyList<SalesReportItem>> GetSalesReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<IReadOnlyList<OccupancyReportItem>> GetOccupancyReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
}
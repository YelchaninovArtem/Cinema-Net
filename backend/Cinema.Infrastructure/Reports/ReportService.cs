using Cinema.Application.Reports;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Reports;

public sealed class ReportService : IReportService
{
    private readonly CinemaDbContext _db;

    public ReportService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalesReportItem>> GetSalesReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var results = await _db.Tickets
            .Where(t => t.Status == TicketStatus.Paid
                     && t.CreatedUtc >= start
                     && t.CreatedUtc < end)
            .GroupBy(t => t.CreatedUtc.Date)
            .Select(g => new { Date = g.Key, Count = g.Count(), Total = g.Sum(t => t.FinalAmount) })
            .OrderBy(r => r.Date)
            .ToListAsync(ct);

        return results
            .Select(r => new SalesReportItem(r.Date.ToString("yyyy-MM-dd"), r.Count, r.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<OccupancyReportItem>> GetOccupancyReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        // Count paid tickets per showtime
        var ticketCounts = await _db.Tickets
            .Where(t => t.Status == TicketStatus.Paid
                      && t.Showtime.StartUtc >= start
                      && t.Showtime.StartUtc < end)
            .GroupBy(t => t.ShowtimeId)
            .Select(g => new { ShowtimeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShowtimeId, x => x.Count, ct);

        var showtimes = await _db.Showtimes
            .Include(s => s.Hall)
            .Include(s => s.Movie)
            .Where(s => s.StartUtc >= start && s.StartUtc < end)
            .ToListAsync(ct);

        var result = showtimes.Select(st =>
        {
            var totalSeats = st.Hall.Rows * st.Hall.Cols;
            var occupiedSeats = ticketCounts.TryGetValue(st.Id, out var count) ? count : 0;
            return new OccupancyReportItem(
                st.Hall.Name,
                st.Movie!.Title,
                st.StartUtc.ToString("yyyy-MM-dd"),
                occupiedSeats,
                totalSeats,
                totalSeats > 0 ? Math.Round((double)occupiedSeats / totalSeats * 100, 1) : 0);
        }).ToList();

        return result.OrderBy(r => r.Date).ThenBy(r => r.HallName).ToList();
    }
}
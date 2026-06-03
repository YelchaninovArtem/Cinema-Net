using Cinema.Application.Reviews;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Identity;
using Cinema.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Reviews;

public sealed class ReviewService : IReviewService
{
    private readonly CinemaDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ReviewService(CinemaDbContext db, UserManager<ApplicationUser> users)
    {
        _db    = db;
        _users = users;
    }

    public async Task<MovieReviewsDto> GetMovieReviewsAsync(int movieId, CancellationToken ct = default)
    {
        var reviews = await _db.Reviews
            .Where(r => r.MovieId == movieId)
            .OrderByDescending(r => r.CreatedUtc)
            .ToListAsync(ct);

        var dtos = await ToDtosAsync(reviews, ct);

        double? avg = reviews.Count > 0
            ? Math.Round(reviews.Average(r => r.Rating), 1)
            : null;

        return new MovieReviewsDto(dtos, avg, reviews.Count);
    }

    public async Task<bool> CanReviewAsync(string userId, int movieId, CancellationToken ct = default)
    {
        var reviewAvailableUtc = DateTime.UtcNow;

        // Користувач може залишити відгук лише після фактичного відвідування:
        // квиток використаний, а сеанс завершився щонайменше хвилину тому.
        return await _db.Tickets
            .AnyAsync(t => t.UserId == userId
                        && t.Status == TicketStatus.Used
                        && t.Showtime.MovieId == movieId
                        && t.Showtime.StartUtc.AddMinutes(t.Showtime.Movie.DurationMinutes + 1) <= reviewAvailableUtc, ct);
    }

    public async Task<ReviewDto?> GetUserReviewAsync(string userId, int movieId, CancellationToken ct = default)
    {
        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId, ct);

        return review is null ? null : await ToDtoAsync(review, ct);
    }

    public async Task<IReadOnlyList<UserReviewDto>> GetUserReviewsAsync(string userId, CancellationToken ct = default) =>
        await _db.Reviews
            .Include(r => r.Movie)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedUtc)
            .Select(r => new UserReviewDto(
                r.Id,
                r.MovieId,
                r.Movie.Title,
                r.Rating,
                r.Comment,
                DateTimeUtc.Mark(r.CreatedUtc)))
            .ToListAsync(ct);

    public async Task<ReviewDto> SubmitAsync(
        string userId, SubmitReviewRequest req, CancellationToken ct = default)
    {
        // перевіряємо наявність використаного квитка на вже завершений сеанс
        var canReview = await CanReviewAsync(userId, req.MovieId, ct);
        if (!canReview)
            throw new DomainException("You can only review movies you have watched.");

        // перевіряємо дублювання
        var existing = await _db.Reviews
            .AnyAsync(r => r.UserId == userId && r.MovieId == req.MovieId, ct);
        if (existing)
            throw new DomainException("You have already submitted a review for this movie.");

        var review = new Review(userId, req.MovieId, req.Rating, req.Comment);
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(review, ct);
    }

    public async Task<ReviewDto> UpdateAsync(
        string userId, int reviewId, UpdateReviewRequest req, CancellationToken ct = default)
    {
        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        review.Update(req.Rating, req.Comment);
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(review, ct);
    }

    public async Task DeleteAsync(string userId, int reviewId, bool isAdmin = false, CancellationToken ct = default)
    {
        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId && (isAdmin || r.UserId == userId), ct)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminReviewDto>> GetAllForAdminAsync(CancellationToken ct = default)
    {
        var reviews = await _db.Reviews
            .Include(r => r.Movie)
            .OrderByDescending(r => r.CreatedUtc)
            .ToListAsync(ct);

        var result = new List<AdminReviewDto>();
        foreach (var r in reviews)
        {
            var user = await _users.FindByIdAsync(r.UserId);
            result.Add(new AdminReviewDto(r.Id, r.MovieId, user?.UserName ?? "User", r.Movie.Title, r.Rating, r.Comment, r.CreatedUtc));
        }
        return result;
    }

    // ── Хелпери ─────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<ReviewDto>> ToDtosAsync(
        IEnumerable<Review> reviews, CancellationToken ct)
    {
        var result = new List<ReviewDto>();
        foreach (var r in reviews)
            result.Add(await ToDtoAsync(r, ct));
        return result;
    }

    private async Task<ReviewDto> ToDtoAsync(Review r, CancellationToken ct)
    {
        var user     = await _users.FindByIdAsync(r.UserId);
        var userName = user?.UserName ?? "User";
        return new ReviewDto(r.Id, r.UserId, userName, r.Rating, r.Comment, r.IsApproved, r.CreatedUtc);
    }
}

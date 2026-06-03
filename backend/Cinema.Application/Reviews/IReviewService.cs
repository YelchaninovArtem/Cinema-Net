namespace Cinema.Application.Reviews;

public interface IReviewService
{
    /// <summary>Отримати всі відгуки фільму + середній рейтинг.</summary>
    Task<MovieReviewsDto> GetMovieReviewsAsync(int movieId, CancellationToken ct = default);

    /// <summary>Перевіряє, чи є в користувача використаний квиток для цього фільму.</summary>
    Task<bool> CanReviewAsync(string userId, int movieId, CancellationToken ct = default);

    /// <summary>Перевіряє, чи вже залишив користувач відгук.</summary>
    Task<ReviewDto?> GetUserReviewAsync(string userId, int movieId, CancellationToken ct = default);

    /// <summary>Усі відгуки поточного користувача.</summary>
    Task<IReadOnlyList<UserReviewDto>> GetUserReviewsAsync(string userId, CancellationToken ct = default);

    /// <summary>Надіслати новий відгук (тільки якщо є Used-квиток і ще не залишав).</summary>
    Task<ReviewDto> SubmitAsync(string userId, SubmitReviewRequest request, CancellationToken ct = default);

    /// <summary>Редагувати власний відгук.</summary>
    Task<ReviewDto> UpdateAsync(string userId, int reviewId, UpdateReviewRequest request, CancellationToken ct = default);

    /// <summary>Видалити власний відгук або будь-який відгук для адміна.</summary>
    Task DeleteAsync(string userId, int reviewId, bool isAdmin = false, CancellationToken ct = default);

    /// <summary>Усі відгуки для адмін-панелі.</summary>
    Task<IReadOnlyList<AdminReviewDto>> GetAllForAdminAsync(CancellationToken ct = default);
}

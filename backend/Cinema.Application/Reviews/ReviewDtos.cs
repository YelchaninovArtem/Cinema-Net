namespace Cinema.Application.Reviews;

public sealed record ReviewDto(
    int      Id,
    string   UserId,
    string   UserName,
    int      Rating,
    string   Comment,
    bool     IsApproved,
    DateTime CreatedUtc);

public sealed record MovieReviewsDto(
    IReadOnlyList<ReviewDto> Reviews,
    double?                  AverageRating,
    int                      TotalReviews);

public sealed record SubmitReviewRequest(int MovieId, int Rating, string Comment);

public sealed record UpdateReviewRequest(int Rating, string Comment);

public sealed record AdminReviewDto(
    int      Id,
    int      MovieId,
    string   UserName,
    string   MovieTitle,
    int      Rating,
    string   Comment,
    DateTime CreatedUtc);

public sealed record UserReviewDto(
    int      Id,
    int      MovieId,
    string   MovieTitle,
    int      Rating,
    string   Comment,
    DateTime CreatedUtc);

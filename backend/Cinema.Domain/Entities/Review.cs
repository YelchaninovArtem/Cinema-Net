using Cinema.Domain.Common;

namespace Cinema.Domain.Entities;

public sealed class Review
{
    private Review() { }

    public Review(string userId, int movieId, int rating, string comment)
    {
        if (rating is < 1 or > 10)
            throw new DomainException("Rating must be between 1 and 10.");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("Review comment is required.");

        UserId     = userId;
        MovieId    = movieId;
        Rating     = rating;
        Comment    = comment.Trim();
        IsApproved = true;
        CreatedUtc = DateTime.UtcNow;
    }

    public int      Id         { get; private set; }
    public string   UserId     { get; private set; } = default!;
    public int      MovieId    { get; private set; }
    public Movie    Movie      { get; private set; } = default!;
    public int      Rating     { get; private set; }  // 1–10
    public string   Comment    { get; private set; } = default!;
    public bool     IsApproved { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public void Update(int rating, string comment)
    {
        if (rating is < 1 or > 10)
            throw new DomainException("Rating must be between 1 and 10.");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("Review comment is required.");
        Rating     = rating;
        Comment    = comment.Trim();
        IsApproved = true;
    }
}

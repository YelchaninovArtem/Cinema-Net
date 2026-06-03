namespace Cinema.Infrastructure.Identity;

public sealed class RefreshToken
{
    private RefreshToken() { }

    public RefreshToken(string token, string userId, DateTime expiresUtc)
    {
        Token = token;
        UserId = userId;
        ExpiresUtc = expiresUtc;
    }

    public int Id { get; private set; }
    public string Token { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public ApplicationUser User { get; private set; } = default!;
    public DateTime ExpiresUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke() => IsRevoked = true;
}

namespace Cinema.Application.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string Role);

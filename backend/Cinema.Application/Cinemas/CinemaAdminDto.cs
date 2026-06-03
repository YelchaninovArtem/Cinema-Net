namespace Cinema.Application.Cinemas;

public sealed record CinemaAdminDto(int Id, string Name, string City, string Address, string TimezoneId);

public sealed record CreateCinemaRequest(string Name, string City, string Address);

public sealed record UpdateCinemaRequest(string Name, string City, string Address);

namespace Cinema.Infrastructure.Email;

public sealed class EmailOptions
{
    public string? Host        { get; init; }
    public int     Port        { get; init; } = 587;
    public string? User        { get; init; }
    public string? Password    { get; init; }
    public string  From        { get; init; } = "no-reply@cinema.local";
    public string  SenderName  { get; init; } = "Мережа кінотеатрів";
}

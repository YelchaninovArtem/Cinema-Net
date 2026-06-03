namespace Cinema.Domain.Entities;

public sealed class Favorite
{
    public string UserId { get; set; } = default!;
    public int MovieId  { get; set; }

    // навігаційні властивості
    public Movie Movie { get; set; } = default!;
}

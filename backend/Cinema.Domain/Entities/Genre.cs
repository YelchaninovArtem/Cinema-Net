using Cinema.Domain.Common;

namespace Cinema.Domain.Entities;

public sealed class Genre
{
    private readonly List<Movie> _movies = [];

    private Genre() { }

    public Genre(string name)
    {
        Rename(name);
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = default!;

    public IReadOnlyCollection<Movie> Movies => _movies.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Genre name is required.");
        Name = name.Trim();
    }
}

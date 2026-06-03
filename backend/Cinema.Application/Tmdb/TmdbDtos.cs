namespace Cinema.Application.Tmdb;

public sealed record TmdbMovieSearchResult(
    int TmdbId,
    string Title,
    int? Year,
    string? PosterUrl,
    string[] Genres);

public sealed record TmdbMovieDetail(
    int TmdbId,
    string Title,
    string Description,
    int DurationMinutes,
    string AgeRating,
    DateTime ReleaseDateUtc,
    string? PosterUrl,
    string? TrailerUrl,
    string[] Genres);

/// <summary>Параметри фільтрації для /discover/movie</summary>
public sealed record TmdbDiscoverFilters(
    int? GenreId,
    string? OriginalLanguage,
    string SortBy,
    int Page);

public sealed record TmdbGenre(int Id, string Name);

/// <summary>Одна сторінка результатів discover</summary>
public sealed record TmdbMoviePageResult(
    IReadOnlyList<TmdbMovieSearchResult> Results,
    int Page,
    int TotalPages);

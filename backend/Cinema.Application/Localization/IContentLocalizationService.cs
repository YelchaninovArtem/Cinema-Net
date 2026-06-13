namespace Cinema.Application.Localization;

public interface IContentLocalizationService
{
    string NormalizeLanguage(string? language);
    IReadOnlyList<string> LocalizeGenres(IEnumerable<string> genres, string? language);
    Task<string> LocalizeMovieDescriptionAsync(string description, string? language, CancellationToken ct = default);
}

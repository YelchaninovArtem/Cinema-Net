namespace Cinema.Application.Cinemas;

public interface ICinemaQueryService
{
    Task<IReadOnlyList<CinemaBranchDto>> GetAllAsync(CancellationToken ct = default);
}

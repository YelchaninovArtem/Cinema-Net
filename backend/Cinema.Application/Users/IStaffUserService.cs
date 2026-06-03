namespace Cinema.Application.Users;

public interface IStaffUserService
{
    Task<IReadOnlyList<StaffUserDto>> GetStaffAsync(CancellationToken ct = default);
    Task<StaffUserDto> CreateAsync(CreateStaffUserRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

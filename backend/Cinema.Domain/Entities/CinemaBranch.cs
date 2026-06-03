using Cinema.Domain.Common;

namespace Cinema.Domain.Entities;

public sealed class CinemaBranch
{
    private readonly List<Hall> _halls = [];

    private CinemaBranch() { }

    public CinemaBranch(string name, string city, string address, string timezoneId)
    {
        Rename(name);
        Relocate(city, address);
        SetTimezone(timezoneId);
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string TimezoneId { get; private set; } = default!;

    public IReadOnlyCollection<Hall> Halls => _halls.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Cinema branch name is required.");
        Name = name.Trim();
    }

    public void Relocate(string city, string address)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City is required.");
        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Address is required.");
        City = city.Trim();
        Address = address.Trim();
    }

    public void SetTimezone(string timezoneId)
    {
        // Перевірка існування таймзони — валідація на боці Application/Infrastructure,
        // у Domain лише гарантуємо, що рядок не порожній.
        if (string.IsNullOrWhiteSpace(timezoneId))
            throw new DomainException("Timezone identifier is required.");
        TimezoneId = timezoneId.Trim();
    }
}

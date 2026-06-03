using Cinema.Domain.Common;

namespace Cinema.Domain.Entities;

public sealed class LoyaltyAccount
{
    private LoyaltyAccount() { }

    public LoyaltyAccount(string userId)
    {
        UserId = userId;
    }

    public string  UserId       { get; private set; } = default!;
    public int     Balance      { get; private set; }  // балів
    public int     TotalEarned  { get; private set; }  // всього зароблено (статистика)

    /// <summary>Нараховує бали після оплати.</summary>
    public void Earn(int points)
    {
        if (points <= 0) throw new DomainException("Points to earn must be positive.");
        Balance     += points;
        TotalEarned += points;
    }

    /// <summary>Списує бали при використанні на знижку.</summary>
    public void Redeem(int points)
    {
        if (points <= 0) throw new DomainException("Points to redeem must be positive.");
        if (points > Balance)
            throw new DomainException($"Insufficient balance: {Balance} < {points}.");
        Balance -= points;
    }

    /// <summary>Повертає раніше списані бали без зміни статистики TotalEarned.</summary>
    public void Restore(int points)
    {
        if (points <= 0) throw new DomainException("Points to restore must be positive.");
        Balance += points;
    }
}

namespace Cinema.Application.Loyalty;

public sealed record LoyaltyBalanceDto(int Balance, int TotalEarned);

public sealed record ApplyLoyaltyRequest(int BookingId, int PointsToRedeem);

public sealed record ApplyLoyaltyResponse(decimal NewTotal, int PointsRedeemed, int RemainingBalance);

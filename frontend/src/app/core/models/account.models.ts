export interface FavoriteSummary {
  movieId: number;
  title: string;
  posterUrl: string | null;
  averageRating: number | null;
}

export interface LoyaltyBalance {
  balance: number;
  totalEarned: number;
}

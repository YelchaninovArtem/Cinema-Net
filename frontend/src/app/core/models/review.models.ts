export interface ReviewDto {
  id: number;
  userId: string;
  userName: string;
  rating: number;
  comment: string;
  isApproved: boolean;
  createdUtc: string;
}

export interface MovieReviewsDto {
  reviews: ReviewDto[];
  averageRating: number | null;
  totalReviews: number;
}

export interface CanReviewDto {
  canReview: boolean;
  myReview: ReviewDto | null;
}

export interface UserReviewDto {
  id: number;
  movieId: number;
  movieTitle: string;
  rating: number;
  comment: string;
  createdUtc: string;
}

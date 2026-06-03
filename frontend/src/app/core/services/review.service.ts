import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CanReviewDto, MovieReviewsDto, ReviewDto, UserReviewDto } from '../models/review.models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/reviews`;

  getMovieReviews(movieId: number): Observable<MovieReviewsDto> {
    return this.http.get<MovieReviewsDto>(`${this.base}/movies/${movieId}`);
  }

  canReview(movieId: number): Observable<CanReviewDto> {
    return this.http.get<CanReviewDto>(`${this.base}/movies/${movieId}/can-review`);
  }

  getMyReviews(): Observable<UserReviewDto[]> {
    return this.http.get<UserReviewDto[]>(`${this.base}/my`);
  }

  submit(movieId: number, rating: number, comment: string): Observable<ReviewDto> {
    return this.http.post<ReviewDto>(this.base, { movieId, rating, comment });
  }

  update(reviewId: number, rating: number, comment: string): Observable<ReviewDto> {
    return this.http.put<ReviewDto>(`${this.base}/${reviewId}`, { rating, comment });
  }

  delete(reviewId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${reviewId}`);
  }
}

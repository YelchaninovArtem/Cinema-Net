import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CinemaAdminDto {
  id: number;
  name: string;
  city: string;
  address: string;
  timezoneId: string;
}

export interface CreateCinemaRequest {
  name: string;
  city: string;
  address: string;
}

export interface UpdateCinemaRequest {
  name: string;
  city: string;
  address: string;
}

export interface HallAdminDto {
  id: number;
  cinemaBranchId: number;
  cinemaName: string;
  hallName: string;
  rows: number;
  cols: number;
  layout: SeatTypeCode[][];
}

export interface CreateHallRequest {
  cinemaBranchId: number;
  name: string;
  rows: number;
  cols: number;
  layout: SeatTypeCode[][];
}

export interface UpdateHallRequest {
  name: string;
  rows: number;
  cols: number;
  layout: SeatTypeCode[][];
}

export type SeatTypeCode = 1 | 2 | 3;

export interface MovieAdminDto {
  id: number;
  title: string;
  description: string;
  durationMinutes: number;
  ageRating: string;
  releaseDateUtc: string;
  posterUrl: string | null;
  trailerUrl: string | null;
  genreIds: number[];
}

export interface CreateMovieRequest {
  title: string;
  description: string;
  durationMinutes: number;
  ageRating: string;
  releaseDateUtc: string;
  posterUrl?: string;
  trailerUrl?: string;
  genreIds: number[];
}

export interface UpdateMovieRequest {
  title: string;
  description: string;
  durationMinutes: number;
  ageRating: string;
  releaseDateUtc: string;
  posterUrl?: string;
  trailerUrl?: string;
  genreIds: number[];
}

export interface ShowtimeAdminDto {
  id: number;
  movieId: number;
  movieTitle: string;
  hallId: number;
  hallName: string;
  cinemaBranchId: number;
  cinemaName: string;
  startUtc: string;
  format: string;
  basePrice: number;
}

export interface CreateShowtimeRequest {
  movieId: number;
  hallId: number;
  startUtc: string;
  format: string;
  basePrice: number;
}

export interface UpdateShowtimeRequest {
  startUtc: string;
  format: string;
  basePrice: number;
}

export interface ShowtimeConflictResult {
  hasConflict: boolean;
  conflictingShowtimeId: number | null;
  conflictingMovieTitle: string | null;
  conflictingStartUtc: string | null;
  conflictingEndUtc: string | null;
}

export interface SalesReportItem {
  date: string;
  totalBookings: number;
  totalRevenue: number;
}

export interface OccupancyReportItem {
  hallName: string;
  movieTitle: string;
  date: string;
  occupiedSeats: number;
  totalSeats: number;
  occupancyPercent: number;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface StaffUserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface AdminReviewDto {
  id: number;
  movieId: number;
  userName: string;
  movieTitle: string;
  rating: number;
  comment: string;
  createdUtc: string;
}

export interface CreateStaffUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: 'Admin' | 'Cashier';
}

export interface PromoCodeDto {
  id: number;
  code: string;
  discountType: 'Percent' | 'Fixed';
  value: number;
  validFrom: string;
  validTo: string;
  usageLimit: number;
  perUserLimit: number;
  isPersonal: boolean;
  ownerUserId: string | null;
  usageCount: number;
}

export interface CreatePromoCodeRequest {
  code: string;
  discountType: 'Percent' | 'Fixed';
  value: number;
  validFrom: string;
  validTo: string;
  usageLimit: number;
  perUserLimit: number;
  isPersonal?: boolean;
  ownerUserId?: string;
}

export interface UpdatePromoCodeRequest {
  code: string;
  discountType: 'Percent' | 'Fixed';
  value: number;
  validFrom: string;
  validTo: string;
  usageLimit: number;
  perUserLimit: number;
  isPersonal?: boolean;
  ownerUserId?: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // ═══════════ CINEMAS ═══════════
  getCinemas(): Observable<CinemaAdminDto[]> {
    return this.http.get<CinemaAdminDto[]>(`${this.base}/admin/cinemas`);
  }

  getCinema(id: number): Observable<CinemaAdminDto> {
    return this.http.get<CinemaAdminDto>(`${this.base}/admin/cinemas/${id}`);
  }

  createCinema(req: CreateCinemaRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/admin/cinemas`, req);
  }

  updateCinema(id: number, req: UpdateCinemaRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/admin/cinemas/${id}`, req);
  }

  deleteCinema(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/cinemas/${id}`);
  }

  // ═══════════ HALLS ═══════════
  getHalls(): Observable<HallAdminDto[]> {
    return this.http.get<HallAdminDto[]>(`${this.base}/admin/halls`);
  }

  getHallsByCinema(cinemaId: number): Observable<HallAdminDto[]> {
    return this.http.get<HallAdminDto[]>(`${this.base}/admin/halls/cinema/${cinemaId}`);
  }

  getHall(id: number): Observable<HallAdminDto> {
    return this.http.get<HallAdminDto>(`${this.base}/admin/halls/${id}`);
  }

  createHall(req: CreateHallRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/admin/halls`, req);
  }

  updateHall(id: number, req: UpdateHallRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/admin/halls/${id}`, req);
  }

  deleteHall(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/halls/${id}`);
  }

  // ═══════════ MOVIES ═══════════
  getMovies(): Observable<MovieAdminDto[]> {
    return this.http.get<MovieAdminDto[]>(`${this.base}/admin/movies`);
  }

  getMovie(id: number): Observable<MovieAdminDto> {
    return this.http.get<MovieAdminDto>(`${this.base}/admin/movies/${id}`);
  }

  createMovie(req: CreateMovieRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/admin/movies`, req);
  }

  updateMovie(id: number, req: UpdateMovieRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/admin/movies/${id}`, req);
  }

  deleteMovie(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/movies/${id}`);
  }

  ensureGenres(names: string[]): Observable<{ id: number; name: string }[]> {
    return this.http.post<{ id: number; name: string }[]>(`${this.base}/genres/ensure`, names);
  }

  // ═══════════ SHOWTIMES ═══════════
  getShowtimes(): Observable<ShowtimeAdminDto[]> {
    return this.http.get<ShowtimeAdminDto[]>(`${this.base}/admin/showtimes`);
  }

  getShowtimesByCinema(cinemaId: number): Observable<ShowtimeAdminDto[]> {
    return this.http.get<ShowtimeAdminDto[]>(`${this.base}/admin/showtimes/cinema/${cinemaId}`);
  }

  getShowtimesByHall(hallId: number): Observable<ShowtimeAdminDto[]> {
    return this.http.get<ShowtimeAdminDto[]>(`${this.base}/admin/showtimes/hall/${hallId}`);
  }

  getShowtime(id: number): Observable<ShowtimeAdminDto> {
    return this.http.get<ShowtimeAdminDto>(`${this.base}/admin/showtimes/${id}`);
  }

  createShowtime(req: CreateShowtimeRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/admin/showtimes`, req);
  }

  updateShowtime(id: number, req: UpdateShowtimeRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/admin/showtimes/${id}`, req);
  }

  deleteShowtime(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/showtimes/${id}`);
  }

  checkShowtimeConflict(
    hallId: number,
    startUtc: string,
    endUtc: string,
    excludeShowtimeId?: number
  ): Observable<ShowtimeConflictResult> {
    let params = new HttpParams()
      .set('hallId', hallId.toString())
      .set('startUtc', startUtc)
      .set('endUtc', endUtc);
    if (excludeShowtimeId) {
      params = params.set('excludeShowtimeId', excludeShowtimeId.toString());
    }
    return this.http.get<ShowtimeConflictResult>(`${this.base}/admin/showtimes/check-conflict`, { params });
  }

  // ═══════════ REPORTS ═══════════
  getSalesReport(startDate: string, endDate: string): Observable<SalesReportItem[]> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<SalesReportItem[]>(`${this.base}/admin/reports/sales`, { params });
  }

  getOccupancyReport(startDate: string, endDate: string): Observable<OccupancyReportItem[]> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<OccupancyReportItem[]>(`${this.base}/admin/reports/occupancy`, { params });
  }

  downloadReport(type: 'sales' | 'occupancy', format: 'pdf' | 'xlsx', startDate: string, endDate: string): Observable<Blob> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get(`${this.base}/admin/reports/${type}/${format}`, {
      params,
      responseType: 'blob',
    });
  }

  // ═══════════ PROMO CODES ═══════════
  getPromoCodes(): Observable<PromoCodeDto[]> {
    return this.http.get<PromoCodeDto[]>(`${this.base}/admin/promo-codes`);
  }

  getPromoCode(id: number): Observable<PromoCodeDto> {
    return this.http.get<PromoCodeDto>(`${this.base}/admin/promo-codes/${id}`);
  }

  createPromoCode(req: CreatePromoCodeRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/admin/promo-codes`, req);
  }

  updatePromoCode(id: number, req: UpdatePromoCodeRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/admin/promo-codes/${id}`, req);
  }

  deletePromoCode(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/promo-codes/${id}`);
  }

  // ═══════════ USERS ═══════════
  getUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${this.base}/admin/users`);
  }

  // ═══════════ STAFF ═══════════
  getStaff(): Observable<StaffUserDto[]> {
    return this.http.get<StaffUserDto[]>(`${this.base}/admin/staff`);
  }

  createStaff(req: CreateStaffUserRequest): Observable<StaffUserDto> {
    return this.http.post<StaffUserDto>(`${this.base}/admin/staff`, req);
  }

  deleteStaff(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/admin/staff/${id}`);
  }

  // ═══════════ REVIEWS ═══════════
  getReviews(): Observable<AdminReviewDto[]> {
    return this.http.get<AdminReviewDto[]>(`${this.base}/reviews/admin`);
  }

  deleteReview(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/reviews/${id}`);
  }
}

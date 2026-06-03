import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MovieDetail, MovieFilters, MovieSummary, Showtime } from '../models/catalog.models';

@Injectable({ providedIn: 'root' })
export class MovieService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getMovies(filters: MovieFilters = {}): Observable<MovieSummary[]> {
    let params = new HttpParams();
    if (filters.title)   params = params.set('title', filters.title);
    if (filters.city)    params = params.set('city', filters.city);
    if (filters.date)    params = params.set('date', filters.date);
    if (filters.format)  params = params.set('format', filters.format);
    if (filters.genreId) params = params.set('genreId', filters.genreId);
    return this.http.get<MovieSummary[]>(`${this.base}/movies`, { params });
  }

  getMovie(id: number): Observable<MovieDetail> {
    return this.http.get<MovieDetail>(`${this.base}/movies/${id}`);
  }

  getShowtimes(filters: { movieId?: number; city?: string; date?: string; format?: string } = {}): Observable<Showtime[]> {
    let params = new HttpParams();
    if (filters.movieId) params = params.set('movieId', filters.movieId);
    if (filters.city)    params = params.set('city', filters.city);
    if (filters.date)    params = params.set('date', filters.date);
    if (filters.format)  params = params.set('format', filters.format);
    return this.http.get<Showtime[]>(`${this.base}/showtimes`, { params });
  }
}

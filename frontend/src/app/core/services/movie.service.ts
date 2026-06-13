import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { MovieDetail, MovieFilters, MovieSummary, Showtime } from '../models/catalog.models';
import { LanguageService } from './language.service';

@Injectable({ providedIn: 'root' })
export class MovieService {
  private readonly http = inject(HttpClient);
  private readonly translate = inject(TranslateService);
  private readonly language = inject(LanguageService);
  private readonly base = environment.apiUrl;

  getMovies(filters: MovieFilters = {}, lang = this.currentLang()): Observable<MovieSummary[]> {
    let params = new HttpParams().set('lang', lang);
    if (filters.title)   params = params.set('title', filters.title);
    if (filters.city)    params = params.set('city', filters.city);
    if (filters.date)    params = params.set('date', filters.date);
    if (filters.format)  params = params.set('format', filters.format);
    if (filters.genreId) params = params.set('genreId', filters.genreId);
    return this.http.get<MovieSummary[]>(`${this.base}/movies`, { params });
  }

  getMovie(id: number, lang = this.currentLang()): Observable<MovieDetail> {
    const params = new HttpParams().set('lang', lang);
    return this.http.get<MovieDetail>(`${this.base}/movies/${id}`, { params });
  }

  getShowtimes(filters: { movieId?: number; city?: string; date?: string; format?: string } = {}): Observable<Showtime[]> {
    let params = new HttpParams();
    if (filters.movieId) params = params.set('movieId', filters.movieId);
    if (filters.city)    params = params.set('city', filters.city);
    if (filters.date)    params = params.set('date', filters.date);
    if (filters.format)  params = params.set('format', filters.format);
    return this.http.get<Showtime[]>(`${this.base}/showtimes`, { params });
  }

  private currentLang(): string {
    return this.language.currentLang || this.translate.currentLang || this.translate.defaultLang || 'en';
  }
}

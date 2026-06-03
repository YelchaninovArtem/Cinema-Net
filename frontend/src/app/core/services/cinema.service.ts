import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CinemaBranch, Genre } from '../models/catalog.models';

@Injectable({ providedIn: 'root' })
export class CinemaService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getCinemas(): Observable<CinemaBranch[]> {
    return this.http.get<CinemaBranch[]>(`${this.base}/cinemas`);
  }

  getGenres(): Observable<Genre[]> {
    return this.http.get<Genre[]>(`${this.base}/genres`);
  }
}

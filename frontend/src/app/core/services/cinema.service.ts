import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { CinemaBranch, Genre } from '../models/catalog.models';
import { LanguageService } from './language.service';

@Injectable({ providedIn: 'root' })
export class CinemaService {
  private readonly http = inject(HttpClient);
  private readonly translate = inject(TranslateService);
  private readonly language = inject(LanguageService);
  private readonly base = environment.apiUrl;

  getCinemas(): Observable<CinemaBranch[]> {
    return this.http.get<CinemaBranch[]>(`${this.base}/cinemas`);
  }

  getGenres(lang = this.currentLang()): Observable<Genre[]> {
    const params = new HttpParams().set('lang', lang);
    return this.http.get<Genre[]>(`${this.base}/genres`, { params });
  }

  private currentLang(): string {
    return this.language.currentLang || this.translate.currentLang || this.translate.defaultLang || 'en';
  }
}

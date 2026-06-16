import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule } from '@ngx-translate/core';
import { HttpClient } from '@angular/common/http';
import { debounceTime, distinctUntilChanged, switchMap, of, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TmdbMovieSearchResult {
  tmdbId: number;
  title: string;
  year: number | null;
  posterUrl: string | null;
  genres: string[];
}

export interface TmdbMovieDetail {
  tmdbId: number;
  title: string;
  description: string;
  durationMinutes: number;
  ageRating: string;
  releaseDateUtc: string;
  posterUrl: string | null;
  trailerUrl: string | null;
  genres: string[];
}

interface TmdbMoviePageResult {
  results: TmdbMovieSearchResult[];
  page: number;
  totalPages: number;
}

interface Genre { id: number; name: string; }

@Component({
  selector: 'app-tmdb-import-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatSelectModule,
    MatTooltipModule,
    TranslateModule,
  ],
  template: `
    <div class="dialog-title-row">
      <h2 mat-dialog-title>{{ 'tmdb.dialogTitle' | translate }}</h2>
      <button
        class="dialog-close"
        type="button"
        (click)="close()"
        [attr.aria-label]="'admin.close' | translate"
        [matTooltip]="'admin.close' | translate">
        <mat-icon>close</mat-icon>
      </button>
    </div>
    <mat-dialog-content class="tmdb-content">
      <mat-tab-group>

        <!-- ── Tab 0: Search ── -->
        <mat-tab [label]="'tmdb.searchTab' | translate">
          <div class="tab-pad">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>{{ 'tmdb.searchPlaceholder' | translate }}</mat-label>
              <input matInput [formControl]="searchCtrl" autocomplete="off">
            </mat-form-field>

            @if (searching) {
              <div class="spinner-row"><mat-spinner diameter="32"></mat-spinner></div>
            }
            @if (importingId !== null) {
              <div class="spinner-row">
                <mat-spinner diameter="32"></mat-spinner>
                <span>{{ 'tmdb.importing' | translate }}</span>
              </div>
            }
            @if (searchError) {
              <p class="tmdb-error">{{ 'tmdb.importError' | translate }}</p>
            }
            @if (!searching && !searchError && searchResults.length === 0 && searched) {
              <p class="tmdb-empty">{{ 'tmdb.noResults' | translate }}</p>
            }
            @if (searchResults.length > 0) {
              <ul class="results-list">
                @for (r of searchResults; track r.tmdbId) {
                  <li class="result-card" (click)="selectMovie(r.tmdbId)" [class.loading]="importingId === r.tmdbId">
                    @if (r.posterUrl) {
                      <img class="poster" [src]="r.posterUrl" [alt]="r.title" width="50" height="75">
                    } @else {
                      <div class="poster-placeholder">🎬</div>
                    }
                    <div class="result-info">
                      <div class="result-title">{{ r.title }} <span class="result-year">{{ r.year }}</span></div>
                      <div class="result-genres">
                        @for (g of r.genres; track g) { <span class="genre-badge">{{ g }}</span> }
                      </div>
                    </div>
                  </li>
                }
              </ul>
            }
          </div>
        </mat-tab>

        <!-- ── Tab 1: Now Playing ── -->
        <mat-tab [label]="'tmdb.nowPlayingTab' | translate">
          <div class="tab-pad">
            <!-- Filters row -->
            <div class="filters-row">
              <mat-form-field appearance="outline" class="filter-field">
                <mat-label>{{ 'tmdb.filterGenre' | translate }}</mat-label>
                <mat-select [(ngModel)]="npGenreId" (ngModelChange)="loadNowPlaying(true)">
                  <mat-option [value]="null">{{ 'tmdb.allGenres' | translate }}</mat-option>
                  @for (g of genres; track g.id) {
                    <mat-option [value]="g.id">{{ g.name }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline" class="filter-field">
                <mat-label>{{ 'tmdb.filterLanguage' | translate }}</mat-label>
                <mat-select [(ngModel)]="npLanguage" (ngModelChange)="loadNowPlaying(true)">
                  <mat-option value="">{{ 'tmdb.allLanguages' | translate }}</mat-option>
                  @for (l of languages; track l.code) {
                    <mat-option [value]="l.code">{{ l.label }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline" class="filter-field">
                <mat-label>{{ 'tmdb.filterSort' | translate }}</mat-label>
                <mat-select [(ngModel)]="npSortBy" (ngModelChange)="loadNowPlaying(true)">
                  <mat-option value="popularity.desc">{{ 'tmdb.sortPopularity' | translate }}</mat-option>
                  <mat-option value="vote_average.desc">{{ 'tmdb.sortRating' | translate }}</mat-option>
                  <mat-option value="primary_release_date.desc">{{ 'tmdb.sortNewest' | translate }}</mat-option>
                </mat-select>
              </mat-form-field>
            </div>

            @if (npLoading && npResults.length === 0) {
              <div class="spinner-row"><mat-spinner diameter="32"></mat-spinner></div>
            }
            @if (npError) {
              <p class="tmdb-error">{{ 'tmdb.importError' | translate }}</p>
            }
            @if (importingId !== null) {
              <div class="spinner-row">
                <mat-spinner diameter="32"></mat-spinner>
                <span>{{ 'tmdb.importing' | translate }}</span>
              </div>
            }

            @if (npResults.length > 0) {
              <ul class="results-list">
                @for (r of npResults; track r.tmdbId) {
                  <li class="result-card" (click)="selectMovie(r.tmdbId)" [class.loading]="importingId === r.tmdbId">
                    @if (r.posterUrl) {
                      <img class="poster" [src]="r.posterUrl" [alt]="r.title" width="50" height="75">
                    } @else {
                      <div class="poster-placeholder">🎬</div>
                    }
                    <div class="result-info">
                      <div class="result-title">{{ r.title }} <span class="result-year">{{ r.year }}</span></div>
                      <div class="result-genres">
                        @for (g of r.genres; track g) { <span class="genre-badge">{{ g }}</span> }
                      </div>
                    </div>
                  </li>
                }
              </ul>

              @if (npPage < npTotalPages) {
                <div class="load-more-row">
                  @if (npLoading) {
                    <mat-spinner diameter="24"></mat-spinner>
                  } @else {
                    <button mat-stroked-button (click)="loadNowPlaying(false)">
                      {{ 'tmdb.loadMore' | translate }}
                    </button>
                  }
                  <span class="page-info">
                    {{ 'tmdb.page' | translate : { page: npPage, total: npTotalPages } }}
                  </span>
                </div>
              }
            }
          </div>
        </mat-tab>

      </mat-tab-group>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'admin.cancel' | translate }}</button>
    </mat-dialog-actions>
  `,
  styles: [`
    /* Dialog title */
    h2[mat-dialog-title],
    ::ng-deep .mat-mdc-dialog-title { color: #f0f0f0 !important; }

    .dialog-title-row {
      position: relative;
    }

    .dialog-title-row h2 {
      margin-right: 48px;
    }

    .dialog-close {
      position: absolute;
      top: 16px;
      right: 16px;
      z-index: 2;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      padding: 0;
      border: 1px solid rgba(148, 163, 184, 0.22);
      border-radius: 10px;
      background: rgba(15, 23, 42, 0.45);
      color: #94a3b8;
      cursor: pointer;
      transition: color 150ms ease, background 150ms ease, border-color 150ms ease, transform 150ms ease, box-shadow 150ms ease;
    }

    .dialog-close mat-icon {
      width: 20px;
      height: 20px;
      font-size: 20px;
    }

    .dialog-close:hover {
      color: #fff;
      background: rgba(99, 102, 241, 0.2);
      border-color: rgba(129, 140, 248, 0.65);
      box-shadow: 0 0 14px rgba(99, 102, 241, 0.2);
      transform: translateY(-1px);
    }

    .dialog-close:focus-visible {
      outline: 2px solid #818cf8;
      outline-offset: 2px;
    }

    /* Dialog shell */
    .tmdb-content {
      min-width: 520px;
      max-height: 580px;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      padding: 0 4px;
    }
    mat-tab-group { flex: 1; overflow: hidden; }
    .tab-pad { padding: 12px 2px 4px; overflow-y: auto; max-height: 460px; }

    /* Search field */
    .full-width { width: 100%; }

    /* Filters */
    .filters-row { display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 8px; }
    .filter-field { flex: 1; min-width: 140px; }

    /* Feedback states */
    .spinner-row { display: flex; align-items: center; gap: 10px; margin: 12px 0; color: #aaa; font-size: 13px; }
    .tmdb-error { color: #ef5350; font-size: 13px; margin: 8px 0; }
    .tmdb-empty { color: #9e9e9e; font-size: 13px; margin: 16px 0; text-align: center; }

    /* Movie cards */
    .results-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
    .result-card {
      display: flex;
      gap: 14px;
      padding: 10px 12px;
      cursor: pointer;
      border-radius: 10px;
      border: 1px solid rgba(255,255,255,0.08);
      background: rgba(255,255,255,0.04);
      transition: background 0.15s, border-color 0.15s;
    }
    .result-card:hover {
      background: rgba(99,102,241,0.15);
      border-color: rgba(99,102,241,0.5);
    }
    .result-card.loading { opacity: 0.45; pointer-events: none; }

    /* Poster */
    .poster { border-radius: 6px; object-fit: cover; flex-shrink: 0; box-shadow: 0 2px 8px rgba(0,0,0,0.4); }
    .poster-placeholder {
      width: 50px; height: 75px;
      background: rgba(255,255,255,0.08);
      border-radius: 6px;
      flex-shrink: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #555;
      font-size: 20px;
    }

    /* Card text */
    .result-info { display: flex; flex-direction: column; gap: 6px; justify-content: center; min-width: 0; }
    .result-title { font-weight: 600; font-size: 14px; color: #f0f0f0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .result-year {
      font-size: 11px;
      font-weight: 500;
      color: #c0c0c0;
      margin-left: 6px;
      background: rgba(255,255,255,0.12);
      padding: 1px 7px;
      border-radius: 4px;
      vertical-align: middle;
    }

    /* Genre badges */
    .result-genres { display: flex; flex-wrap: wrap; gap: 4px; }
    .genre-badge {
      font-size: 10px;
      font-weight: 500;
      padding: 2px 8px;
      border-radius: 12px;
      background: rgba(99,102,241,0.25);
      color: #a5b4fc;
      border: 1px solid rgba(99,102,241,0.35);
      white-space: nowrap;
    }

    /* Load more row */
    .load-more-row { display: flex; align-items: center; gap: 14px; padding: 10px 0 4px; }
    .page-info { font-size: 12px; color: #757575; }
  `],
})
export class TmdbImportDialogComponent implements OnInit {
  private readonly http      = inject(HttpClient);
  private readonly dialogRef = inject(MatDialogRef<TmdbImportDialogComponent, TmdbMovieDetail>);
  private readonly base      = environment.apiUrl;

  // ── Search tab state ──
  searchCtrl    = new FormControl('');
  searchResults: TmdbMovieSearchResult[] = [];
  searching     = false;
  searched      = false;
  searchError   = false;

  // ── Now Playing tab state ──
  npResults:    TmdbMovieSearchResult[] = [];
  npPage        = 0;
  npTotalPages  = 1;
  npLoading     = false;
  npError       = false;
  npGenreId:    number | null = null;
  npLanguage    = '';
  npSortBy      = 'popularity.desc';

  // ── Shared ──
  importingId: number | null = null;
  genres:      Genre[]       = [];

  readonly languages = [
    { code: 'en', label: 'English' },
    { code: 'uk', label: 'Ukrainian' },
    { code: 'fr', label: 'French' },
    { code: 'de', label: 'German' },
    { code: 'es', label: 'Spanish' },
    { code: 'it', label: 'Italian' },
    { code: 'ja', label: 'Japanese' },
    { code: 'ko', label: 'Korean' },
    { code: 'zh', label: 'Chinese' },
    { code: 'hi', label: 'Hindi' },
  ];

  close(): void {
    this.dialogRef.close();
  }

  ngOnInit(): void {
    this.loadGenres();
    this.loadNowPlaying(true);

    this.searchCtrl.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(q => {
        const query = (q ?? '').trim();
        if (!query) {
          this.searchResults = [];
          this.searched      = false;
          this.searchError   = false;
          return of(null);
        }
        this.searching   = true;
        this.searchError = false;
        return this.http.get<TmdbMovieSearchResult[]>(
          `${this.base}/admin/tmdb/search?q=${encodeURIComponent(query)}`
        ).pipe(catchError(() => { this.searchError = true; return of(null); }));
      })
    ).subscribe(res => {
      this.searching = false;
      this.searched  = true;
      if (res !== null) this.searchResults = res ?? [];
    });
  }

  loadNowPlaying(reset: boolean): void {
    if (reset) {
      this.npResults    = [];
      this.npPage       = 0;
      this.npTotalPages = 1;
      this.npError      = false;
    }
    if (this.npLoading) return;

    const nextPage = this.npPage + 1;
    this.npLoading = true;

    let url = `${this.base}/admin/tmdb/now-playing?page=${nextPage}&sortBy=${encodeURIComponent(this.npSortBy)}`;
    if (this.npGenreId)  url += `&genreId=${this.npGenreId}`;
    if (this.npLanguage) url += `&language=${encodeURIComponent(this.npLanguage)}`;

    this.http.get<TmdbMoviePageResult>(url)
      .pipe(catchError(() => { this.npError = true; return of(null); }))
      .subscribe(res => {
        this.npLoading = false;
        if (res) {
          this.npResults    = [...this.npResults, ...res.results];
          this.npPage       = res.page;
          this.npTotalPages = res.totalPages;
        }
      });
  }

  private loadGenres(): void {
    this.http.get<Genre[]>(`${this.base}/admin/tmdb/genres`)
      .pipe(catchError(() => of([])))
      .subscribe(g => this.genres = g);
  }

  selectMovie(tmdbId: number): void {
    if (this.importingId !== null) return;
    this.importingId = tmdbId;
    this.http.get<TmdbMovieDetail>(`${this.base}/admin/tmdb/${tmdbId}`)
      .pipe(catchError(() => { this.importingId = null; return of(null); }))
      .subscribe(detail => {
        this.importingId = null;
        if (detail) this.dialogRef.close(detail);
      });
  }
}

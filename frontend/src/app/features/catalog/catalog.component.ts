import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { catchError, debounceTime, distinctUntilChanged, forkJoin, map, of, switchMap, tap } from 'rxjs';
import { MovieService } from '../../core/services/movie.service';
import { CinemaService } from '../../core/services/cinema.service';
import { MovieCardComponent } from '../../shared/movie-card/movie-card.component';
import { Genre, MovieSummary } from '../../core/models/catalog.models';
import { LanguageService } from '../../core/services/language.service';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule, MatSelectModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule,
    MatDatepickerModule, MatNativeDateModule,
    TranslateModule,
    MovieCardComponent,
  ],
  template: `
    <div class="catalog-shell">
      <h1 class="page-title">{{ 'catalog.title' | translate }}</h1>

      <!-- Filters -->
      <form [formGroup]="filters" (ngSubmit)="applyFilters()" class="filters-row">
        <mat-form-field appearance="outline" class="filter-field filter-field--search">
          <mat-label>{{ 'catalog.filter.title' | translate }}</mat-label>
          <input matInput formControlName="title" autocomplete="off" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter-field">
          <mat-label>{{ 'catalog.filter.city' | translate }}</mat-label>
          <mat-select formControlName="city">
            <mat-option value="">{{ 'catalog.filter.all' | translate }}</mat-option>
            @for (city of cities(); track city) {
              <mat-option [value]="city">{{ city }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter-field">
          <mat-label>{{ 'catalog.filter.date' | translate }}</mat-label>
          <input matInput [matDatepicker]="picker" formControlName="date" readonly />
          <mat-datepicker-toggle matSuffix [for]="picker" />
          <mat-datepicker #picker />
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter-field">
          <mat-label>{{ 'catalog.filter.format' | translate }}</mat-label>
          <mat-select formControlName="format">
            <mat-option value="">{{ 'catalog.filter.all' | translate }}</mat-option>
            @for (fmt of formats; track fmt) {
              <mat-option [value]="fmt">{{ fmt }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="filter-field">
          <mat-label>{{ 'catalog.filter.genre' | translate }}</mat-label>
          <mat-select formControlName="genreId">
            <mat-option [value]="null">{{ 'catalog.filter.all' | translate }}</mat-option>
            @for (g of genres(); track g.id) {
              <mat-option [value]="g.id">{{ g.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <button class="apply-btn" type="submit">
          {{ 'catalog.filter.apply' | translate }}
        </button>
        <button class="reset-btn" type="button" (click)="resetFilters()">
          {{ 'catalog.filter.reset' | translate }}
        </button>
      </form>

      <!-- Results -->
      @if (loading()) {
        <div class="spinner-wrap"><mat-spinner diameter="48" /></div>
      } @else if (movies().length === 0) {
        <p class="empty">{{ 'catalog.noResults' | translate }}</p>
      } @else {
        <div class="movie-grid" [class.movie-grid--refreshing]="refreshing()">
          @for (movie of movies(); track movie.id) {
            <app-movie-card [movie]="movie" />
          }
        </div>
      }

      @if (refreshing()) {
        <div class="refresh-indicator">
          <mat-spinner diameter="24" />
        </div>
      }
    </div>
  `,
  styles: [`
    .catalog-shell {
      position: relative;
      max-width: 1380px;
      margin: 0 auto;
      padding: 18px 16px 24px;
      color: #e2e8f0;
      font-family: 'DM Sans', sans-serif;
    }

    .page-title {
      margin: 0 0 24px;
      font-size: 30px;
      font-weight: 800;
      font-family: 'Syne', sans-serif;
      color: #f8fafc;
      letter-spacing: -0.02em;
    }

    .filters-row {
      display: flex;
      flex-wrap: nowrap;
      gap: 9px;
      align-items: center;
      width: 100%;
      margin-bottom: 28px;
    }

    /* Темна тема для Material form fields через CSS custom properties */
    .filter-field {
      min-width: 0;
      flex: 1 1 0;
      --mdc-outlined-text-field-label-text-color: #94a3b8;
      --mdc-outlined-text-field-input-text-color: #f1f5f9;
      --mdc-outlined-text-field-outline-color: rgba(255,255,255,0.18);
      --mdc-outlined-text-field-hover-outline-color: rgba(255,255,255,0.32);
      --mdc-outlined-text-field-focus-outline-color: #6366f1;
      --mdc-outlined-text-field-focus-label-text-color: #818cf8;
      --mdc-outlined-text-field-container-shape: 10px;
      --mdc-outlined-text-field-container-height: 57px;
      --mat-select-enabled-arrow-color: #94a3b8;
      --mat-select-enabled-trigger-text-color: #f1f5f9;
      --mat-select-placeholder-text-color: #94a3b8;
      --mat-form-field-container-height: 57px;
    }

    .filter-field--search {
      flex-grow: 1.15;
    }

    :host ::ng-deep .filter-field .mat-mdc-text-field-wrapper,
    :host ::ng-deep .filter-field .mat-mdc-form-field-flex {
      height: 57px;
    }

    :host ::ng-deep .filter-field .mat-mdc-form-field-infix {
      display: flex;
      align-items: center;
      min-height: 57px;
      padding-top: 0;
      padding-bottom: 0;
    }

    :host ::ng-deep .filter-field .mat-mdc-floating-label {
      top: 28px;
    }

    :host ::ng-deep .filter-field input.mat-mdc-input-element {
      text-align: left;
    }

    :host ::ng-deep .filter-field .mat-datepicker-toggle .mat-mdc-icon-button {
      width: 40px;
      height: 40px;
      padding: 8px;
    }

    :host ::ng-deep .filter-field .mat-mdc-select-trigger,
    :host ::ng-deep .filter-field .mat-mdc-select-value {
      height: 57px;
      display: flex;
      align-items: center;
      justify-content: flex-start;
      text-align: left;
    }

    :host ::ng-deep .filter-field .mat-mdc-select-arrow-wrapper {
      position: absolute;
      right: 12px;
    }

    .apply-btn {
      flex: 0 0 104px;
      height: 57px;
      padding: 0 18px;
      border-radius: 10px;
      border: none;
      background: linear-gradient(135deg, #6366f1 0%, #818cf8 100%);
      color: #fff;
      font-size: 14px;
      font-weight: 600;
      font-family: 'DM Sans', sans-serif;
      cursor: pointer;
      transition: opacity 0.15s, box-shadow 0.15s;
      box-shadow: 0 2px 12px rgba(99,102,241,0.35);
      white-space: nowrap;
    }
    .apply-btn:hover { opacity: 0.88; box-shadow: 0 4px 18px rgba(99,102,241,0.5); }

    .reset-btn {
      flex: 0 0 86px;
      height: 57px;
      padding: 0 15px;
      border-radius: 10px;
      border: 1px solid rgba(255,255,255,0.18);
      background: rgba(255,255,255,0.07);
      color: #cbd5e1;
      font-size: 14px;
      font-weight: 500;
      font-family: 'DM Sans', sans-serif;
      cursor: pointer;
      transition: background 0.15s, border-color 0.15s, color 0.15s;
      white-space: nowrap;
    }
    .reset-btn:hover {
      background: rgba(255,255,255,0.13);
      border-color: rgba(255,255,255,0.35);
      color: #f1f5f9;
    }

    @media (max-width: 760px) {
      .filters-row {
        overflow-x: auto;
        padding-bottom: 4px;
      }

      .filter-field {
        flex-basis: 132px;
        flex-shrink: 0;
      }

      .filter-field--search {
        flex-basis: 180px;
      }
    }

    .movie-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 20px;
      transition: opacity 0.16s ease;
    }

    .movie-grid--refreshing {
      opacity: 0.72;
      pointer-events: none;
    }

    .spinner-wrap { display: flex; justify-content: center; padding: 60px 0; }

    .refresh-indicator {
      position: fixed;
      right: 24px;
      bottom: 24px;
      z-index: 20;
      display: grid;
      place-items: center;
      width: 44px;
      height: 44px;
      border-radius: 10px;
      border: 1px solid rgba(255,255,255,0.16);
      background: rgba(15,23,42,0.84);
      box-shadow: 0 8px 24px rgba(0,0,0,0.28);
      backdrop-filter: blur(10px);
    }

    .empty {
      text-align: center;
      color: #4a6080;
      font-size: 16px;
      padding: 60px 0;
    }
  `],
})
export class CatalogComponent implements OnInit {
  private readonly movieSvc = inject(MovieService);
  private readonly cinemaSvc = inject(CinemaService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate = inject(TranslateService);
  private readonly language = inject(LanguageService);

  readonly movies = signal<MovieSummary[]>([]);
  readonly cities = signal<string[]>([]);
  readonly genres = signal<Genre[]>([]);
  readonly loading = signal(true);
  readonly refreshing = signal(false);

  readonly formats = ['2D', '3D', 'IMAX'];

  readonly filters = this.fb.nonNullable.group({
    title:   [''],
    city:    [''],
    date:    [null as Date | null],
    format:  [''],
    genreId: [null as number | null],
  });

  ngOnInit(): void {
    this.filters.valueChanges.pipe(
      debounceTime(120),
      map(() => this.buildMovieFilters()),
      distinctUntilChanged((a, b) => this.filtersSignature(a) === this.filtersSignature(b)),
      tap(() => {
        if (this.movies().length === 0) {
          this.loading.set(true);
        } else {
          this.refreshing.set(true);
        }
      }),
      switchMap(filters => this.movieSvc.getMovies(filters, this.language.currentLang).pipe(
        catchError(() => of(this.movies()))
      )),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(movies => {
      this.movies.set(movies);
      this.loading.set(false);
      this.refreshing.set(false);
    });

    this.loadCatalogData();

    this.language.lang$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(lang => this.loadCatalogData(true, lang));
  }

  applyFilters(): void {
    if (this.movies().length === 0) {
      this.loading.set(true);
    } else {
      this.refreshing.set(true);
    }
    this.movieSvc.getMovies(this.buildMovieFilters(), this.language.currentLang).subscribe({
      next: movies => {
        this.movies.set(movies);
        this.loading.set(false);
        this.refreshing.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.refreshing.set(false);
      },
    });
  }

  resetFilters(): void {
    this.filters.reset({ title: '', city: '', date: null, format: '', genreId: null });
  }

  private loadCatalogData(refresh = false, lang = this.language.currentLang): void {
    if (refresh && this.movies().length > 0) {
      this.refreshing.set(true);
    } else {
      this.loading.set(true);
    }

    forkJoin({
      movies:  this.movieSvc.getMovies(this.buildMovieFilters(), lang),
      cinemas: this.cinemaSvc.getCinemas(),
      genres:  this.cinemaSvc.getGenres(lang),
    }).subscribe({
      next: ({ movies, cinemas, genres }) => {
        this.movies.set(movies);
        this.cities.set([...new Set(cinemas.map(c => c.city))].sort());
        this.genres.set(genres);
        this.loading.set(false);
        this.refreshing.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.refreshing.set(false);
      },
    });
  }

  private toDateString(d: Date): string {
    return d.toISOString().slice(0, 10);
  }

  private buildMovieFilters() {
    const v = this.filters.getRawValue();
    return {
      title:   v.title.trim() || undefined,
      city:    v.city    || undefined,
      date:    v.date    ? this.toDateString(v.date) : undefined,
      format:  v.format  || undefined,
      genreId: v.genreId ?? undefined,
    };
  }

  private filtersSignature(filters: ReturnType<CatalogComponent['buildMovieFilters']>): string {
    return JSON.stringify(filters);
  }
}

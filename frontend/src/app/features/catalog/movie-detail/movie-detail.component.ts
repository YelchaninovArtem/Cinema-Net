import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe, DecimalPipe, SlicePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { catchError, forkJoin, of, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MovieService } from '../../../core/services/movie.service';
import { AccountService } from '../../../core/services/account.service';
import { ReviewService } from '../../../core/services/review.service';
import { AuthService } from '../../../core/auth/auth.service';
import { MovieDetail, Showtime } from '../../../core/models/catalog.models';
import { MovieReviewsDto, ReviewDto } from '../../../core/models/review.models';
import { LocalizedDatePipe } from '../../../shared/localized-date.pipe';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-movie-detail',
  standalone: true,
  imports: [
    RouterLink,
    LocalizedDatePipe, SlicePipe, CurrencyPipe, DecimalPipe,
    FormsModule,
    MatButtonModule, MatChipsModule, MatProgressSpinnerModule,
    MatDividerModule,
    TranslateModule,
  ],
  template: `
    <div class="detail-shell">
      @if (loading()) {
        <div class="spinner-wrap"><mat-spinner diameter="48" /></div>
      } @else if (movie()) {

        <!-- Герой: постер + інфо -->
        <div class="hero">
          <img class="poster"
               [src]="movie()!.posterUrl ?? 'https://placehold.co/400x600?text=No+Poster'"
               [alt]="movie()!.title" />
          <div class="info">
            <h1>{{ movie()!.title }}</h1>
            <dl class="meta-list">
              <div class="meta-item">
                <dt>{{ 'catalog.detail.duration' | translate }}</dt>
                <dd>{{ movie()!.durationMinutes }} {{ 'catalog.detail.minutesShort' | translate }}</dd>
              </div>
              <div class="meta-item">
                <dt>{{ 'catalog.detail.ageRating' | translate }}</dt>
                <dd>{{ movie()!.ageRating }}</dd>
              </div>
              <div class="meta-item">
                <dt>{{ 'catalog.detail.releaseDate' | translate }}</dt>
                <dd>{{ movie()!.releaseDateUtc | slice:0:10 }}</dd>
              </div>
            </dl>

            <!-- Рейтинг -->
            @if (reviewData()?.averageRating) {
              <div class="rating-row">
                <div class="stars">
                  @for (i of [1,2,3,4,5,6,7,8,9,10]; track i) {
                    <span class="star" [class.filled]="i <= roundedRating()">★</span>
                  }
                </div>
                <span class="rating-num">{{ reviewData()!.averageRating | number:'1.1-1' }} / 10</span>
                <span class="rating-count">({{ reviewData()!.totalReviews }})</span>
              </div>
            }

            <mat-chip-set>
              @for (g of movie()!.genres; track g) {
                <mat-chip>{{ g }}</mat-chip>
              }
            </mat-chip-set>
            <p class="description">{{ movie()!.description }}</p>

            <div class="action-row">
              @if (movie()!.trailerUrl) {
                <a class="trailer-btn" [href]="movie()!.trailerUrl!" target="_blank" rel="noopener">
                  <span class="trailer-play">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><polygon points="5,3 19,12 5,21"/></svg>
                  </span>
                  {{ 'catalog.trailer' | translate }}
                </a>
              }
              @if (auth.isLoggedIn()) {
                <button class="fav-btn" [class.active]="isFavorite()" (click)="toggleFavorite()">
                  <svg width="18" height="18" viewBox="0 0 24 24"
                       [attr.fill]="isFavorite() ? '#f87171' : 'none'"
                       stroke="#f87171" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>
                  </svg>
                </button>
              }
            </div>
          </div>
        </div>

        <mat-divider />

        <!-- Сеанси -->
        <h2 class="section-title">{{ 'catalog.showtimes' | translate }}</h2>
        @if (showtimes().length === 0) {
          <p class="empty">{{ 'catalog.noShowtimes' | translate }}</p>
        } @else {
          <div class="showtime-list">
            @for (s of showtimes(); track s.id) {
              <div class="showtime-card">
                <div class="showtime-row">
                  <div class="showtime-time-col">
                    <span class="time">{{ s.startUtc | localizedDate:'HH:mm' }}</span>
                    <span class="date">{{ s.startUtc | localizedDate:'dd MMM' }}</span>
                  </div>
                  <div class="branch">
                    <strong>{{ s.cinemaBranchName }}</strong>
                    <span class="hall">{{ s.hallName }}, {{ s.city }}</span>
                  </div>
                  <span class="format-badge">{{ s.format }}</span>
                  <div class="price-book">
                    <span class="price">{{ s.basePrice | currency:'UAH':'symbol':'1.0-0' }}</span>
                    <a class="book-btn" [routerLink]="['/buy', s.id]">
                      <span class="book-btn-text">{{ 'catalog.book' | translate }}</span>
                      <span class="book-btn-arrow">→</span>
                    </a>
                  </div>
                </div>
              </div>
            }
          </div>
        }

        <mat-divider style="margin-top:32px" />

        <!-- Відгуки -->
        <h2 class="section-title">{{ 'reviews.title' | translate }}</h2>

        <!-- Форма подачі / редагування відгуку -->
        @if (auth.isLoggedIn()) {
          @if (canReview() && !myReview()) {
            <div class="review-form">
              <h3 class="form-title">{{ 'reviews.writeReview' | translate }}</h3>
              <div class="star-picker">
                @for (i of [1,2,3,4,5,6,7,8,9,10]; track i) {
                  <button class="star-pick" [class.sel]="i <= formRating"
                          (click)="formRating = i" type="button">★</button>
                }
                <span class="star-label">{{ formRating }}/10</span>
              </div>
              <textarea class="review-textarea"
                        [(ngModel)]="formComment"
                        [placeholder]="'reviews.commentPlaceholder' | translate"
                        rows="4"></textarea>
              <div class="form-actions">
                <button class="submit-btn" (click)="submitReview()" [disabled]="submitting()">
                  @if (submitting()) { <span class="btn-spinner"></span> }
                  {{ 'reviews.submit' | translate }}
                </button>
              </div>
              @if (reviewError()) {
                <p class="review-error">{{ reviewError() | translate }}</p>
              }
            </div>
          } @else if (!myReview()) {
            <p class="review-hint">{{ 'reviews.watchFirst' | translate }}</p>
          }
        } @else {
          <p class="review-hint">
            <a routerLink="/auth/login">{{ 'reviews.loginToReview' | translate }}</a>
          </p>
        }

        <!-- Список відгуків -->
        @if (reviewData()?.reviews?.length) {
          <div class="reviews-list">
            @for (r of reviewData()!.reviews; track r.id) {
              <div class="review-card">
                <div class="rev-header">
                  <span class="rev-user">{{ r.userName }}</span>
                  <div class="rev-stars">
                    @for (i of [1,2,3,4,5,6,7,8,9,10]; track i) {
                      <span class="star-sm" [class.filled]="i <= r.rating">★</span>
                    }
                  </div>
                  <span class="rev-date">{{ r.createdUtc | localizedDate:'dd MMM y':'UTC' }}</span>
                  @if (canDeleteReview(r)) {
                    <button class="review-delete-btn" type="button" (click)="deleteReview(r)">
                      {{ 'reviews.delete' | translate }}
                    </button>
                  }
                </div>
                <p class="rev-comment">{{ r.comment }}</p>
              </div>
            }
          </div>
        } @else {
          <p class="empty-reviews">{{ 'reviews.noReviews' | translate }}</p>
        }

      } @else {
        <p class="empty">{{ 'catalog.movieNotFound' | translate }}</p>
      }

      <a mat-button routerLink="/catalog" class="back-link">← {{ 'catalog.back' | translate }}</a>
    </div>
  `,
  styles: [`
    .detail-shell { max-width: 960px; margin: 0 auto; padding: 24px 16px; color: #e2e8f0; font-family: 'DM Sans', sans-serif; }

    /* ── Hero ── */
    .hero { display: flex; gap: 32px; margin-bottom: 32px; }
    .poster { width: 220px; height: 330px; object-fit: cover; border-radius: 8px; flex-shrink: 0; }
    .info { flex: 1; }
    h1 { margin: 0 0 8px; font-size: 32px; color: #f8fafc; font-family: 'Syne', sans-serif; }
    .meta-list {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      margin: 0 0 14px;
      color: #94a3b8;
    }
    .meta-item {
      display: flex;
      align-items: center;
      gap: 6px;
      margin: 0;
      font-size: 14px;
    }
    .meta-item dt {
      color: #4a6080;
      font-weight: 600;
    }
    .meta-item dd {
      margin: 0;
      color: #94a3b8;
    }
    .description { margin: 16px 0; line-height: 1.6; color: #cbd5e1; }

    /* Рейтинг */
    .rating-row { display: flex; align-items: center; gap: 8px; margin-bottom: 10px; }
    .stars { display: flex; gap: 1px; }
    .star { font-size: 16px; color: #2a3a50; transition: color 0.1s; }
    .star.filled { color: #d4a853; }
    .rating-num { font-weight: 700; color: #d4a853; font-size: 14px; }
    .rating-count { font-size: 13px; color: #4a6080; }

    .section-title { margin: 24px 0 16px; font-size: 22px; color: #f1f5f9; font-family: 'Syne', sans-serif; }

    /* ── Seans ── */
    .showtime-list { display: flex; flex-direction: column; gap: 10px; }
    .showtime-card {
      background: linear-gradient(135deg, #0f1729 0%, #0d1422 100%);
      border: 1px solid rgba(255,255,255,0.07);
      border-radius: 14px;
      padding: 16px 20px;
      transition: border-color 0.2s, box-shadow 0.2s, transform 0.15s;
    }
    .showtime-card:hover {
      border-color: rgba(99,102,241,0.5);
      box-shadow: 0 4px 24px rgba(99,102,241,0.12);
      transform: translateY(-1px);
    }
    .showtime-row { display: flex; align-items: center; gap: 20px; flex-wrap: wrap; }
    .showtime-time-col { display: flex; align-items: baseline; gap: 8px; min-width: 90px; }
    .time { font-size: 24px; font-weight: 800; color: #f8fafc; letter-spacing: -0.5px; }
    .date { font-size: 12px; color: #64748b; font-weight: 500; background: rgba(255,255,255,0.06); padding: 2px 7px; border-radius: 4px; }
    .branch { display: flex; flex-direction: column; flex: 1; }
    .branch strong { color: #f1f5f9; font-weight: 600; font-size: 15px; }
    .hall { font-size: 12px; color: #64748b; margin-top: 3px; }
    .format-badge {
      background: linear-gradient(135deg, #1d4ed8, #2563eb);
      color: #fff;
      border-radius: 6px;
      padding: 4px 12px;
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 0.5px;
      box-shadow: 0 2px 8px rgba(37,99,235,0.35);
    }
    .price-book { display: flex; align-items: center; gap: 14px; margin-left: auto; }
    .price { font-size: 17px; font-weight: 700; color: #e2e8f0; white-space: nowrap; }
    .book-btn {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 9px 20px;
      background: linear-gradient(135deg, #4f46e5, #6366f1);
      color: #fff;
      border-radius: 10px;
      font-size: 14px;
      font-weight: 600;
      text-decoration: none;
      transition: background 0.18s, box-shadow 0.18s, transform 0.12s;
      box-shadow: 0 3px 14px rgba(99,102,241,0.4);
      white-space: nowrap;
    }
    .book-btn:hover {
      background: linear-gradient(135deg, #4338ca, #4f46e5);
      box-shadow: 0 5px 20px rgba(99,102,241,0.55);
      transform: translateY(-1px);
    }
    .book-btn:active { transform: translateY(0); }
    .book-btn-arrow { font-size: 16px; transition: transform 0.15s; }
    .book-btn:hover .book-btn-arrow { transform: translateX(3px); }

    /* ── Форма відгуку ── */
    .review-form {
      background: #0d1422;
      border: 1px solid rgba(99,102,241,0.2);
      border-radius: 14px;
      padding: 24px;
      margin-bottom: 28px;
    }
    .form-title { font-family: 'Syne', sans-serif; font-size: 16px; font-weight: 700; color: #f8fafc; margin: 0 0 16px; }

    .star-picker { display: flex; align-items: center; gap: 4px; margin-bottom: 14px; }
    .star-pick {
      font-size: 22px;
      color: #2a3a50;
      background: none;
      border: none;
      cursor: pointer;
      padding: 0;
      transition: color 0.1s, transform 0.1s;
      line-height: 1;
    }
    .star-pick.sel { color: #d4a853; }
    .star-pick:hover { transform: scale(1.2); color: #d4a853; }
    .star-label { font-size: 13px; color: #4a6080; margin-left: 8px; }

    .review-textarea {
      width: 100%;
      background: rgba(255,255,255,0.04);
      border: 1px solid rgba(255,255,255,0.1);
      border-radius: 8px;
      padding: 12px;
      color: #f1f5f9;
      font-family: 'DM Sans', sans-serif;
      font-size: 14px;
      resize: vertical;
      outline: none;
      box-sizing: border-box;
      transition: border-color 0.18s;
    }
    .review-textarea:focus { border-color: #6366f1; }

    .form-actions { display: flex; justify-content: flex-end; margin-top: 12px; }
    .submit-btn {
      background: #6366f1;
      border: none;
      border-radius: 8px;
      padding: 10px 24px;
      color: #fff;
      font-size: 14px;
      font-weight: 600;
      font-family: 'DM Sans', sans-serif;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 8px;
      transition: background 0.18s;
    }
    .submit-btn:hover:not(:disabled) { background: #4f46e5; }
    .submit-btn:disabled { opacity: 0.5; cursor: default; }
    .btn-spinner {
      width: 14px; height: 14px;
      border: 2px solid rgba(255,255,255,0.3);
      border-top-color: #fff;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .review-error { color: #f87171; font-size: 13px; margin: 8px 0 0; }
    .review-hint { color: #4a6080; font-size: 14px; margin-bottom: 24px; }
    .review-hint a { color: #6366f1; text-decoration: none; }
    .review-hint a:hover { color: #818cf8; }

    /* Мій відгук */
    .my-review-box {
      background: rgba(99,102,241,0.07);
      border: 1px solid rgba(99,102,241,0.2);
      border-radius: 12px;
      padding: 18px 20px;
      margin-bottom: 24px;
    }
    .my-review-header { display: flex; align-items: center; gap: 12px; margin-bottom: 10px; flex-wrap: wrap; }
    .my-review-label { font-weight: 600; color: #818cf8; font-size: 13px; }
    .del-btn { margin-left: auto; background: none; border: 1px solid rgba(248,113,113,0.3); border-radius: 6px; color: #f87171; font-size: 12px; padding: 3px 10px; cursor: pointer; transition: background 0.18s; }
    .del-btn:hover { background: rgba(248,113,113,0.1); }

    /* ── Список відгуків ── */
    .reviews-list { display: flex; flex-direction: column; gap: 12px; }
    .review-card {
      background: #0d1422;
      border: 1px solid rgba(255,255,255,0.07);
      border-radius: 12px;
      padding: 18px 20px;
    }
    .rev-header { display: flex; align-items: center; gap: 12px; margin-bottom: 10px; flex-wrap: wrap; }
    .rev-user { font-weight: 600; color: #f1f5f9; font-size: 14px; }
    .rev-stars { display: flex; gap: 1px; }
    .star-sm { font-size: 14px; color: #2a3a50; }
    .star-sm.filled { color: #d4a853; }
    .rev-date { font-size: 12px; color: #4a6080; margin-left: auto; }
    .review-delete-btn {
      background: none;
      border: 1px solid rgba(248,113,113,0.3);
      border-radius: 6px;
      color: #f87171;
      font-size: 12px;
      padding: 3px 10px;
      cursor: pointer;
      transition: background 0.18s, border-color 0.18s;
    }
    .review-delete-btn:hover {
      background: rgba(248,113,113,0.1);
      border-color: rgba(248,113,113,0.55);
    }
    .rev-comment { font-size: 14px; color: #94a3b8; line-height: 1.55; margin: 0; }
    .review-stars-small { display: flex; gap: 1px; margin-bottom: 8px; }
    .review-comment { font-size: 14px; color: #94a3b8; margin: 0; line-height: 1.5; }
    .empty-reviews { color: #4a6080; font-size: 14px; padding: 16px 0; }

    /* Misc */
    .spinner-wrap { display: flex; justify-content: center; padding: 60px 0; }
    .empty { color: #64748b; text-align: center; padding: 40px 0; }
    .back-link { margin-top: 24px; display: inline-block; }
    .action-row { display: flex; align-items: center; gap: 12px; margin-top: 16px; flex-wrap: wrap; }
    .trailer-btn {
      display: inline-flex;
      align-items: center;
      gap: 10px;
      padding: 10px 22px;
      background: transparent;
      border: 1.5px solid rgba(248,250,252,0.25);
      border-radius: 10px;
      color: #f8fafc;
      font-size: 14px;
      font-weight: 600;
      text-decoration: none;
      transition: border-color 0.18s, background 0.18s, box-shadow 0.18s, transform 0.12s;
      backdrop-filter: blur(4px);
    }
    .trailer-btn:hover {
      border-color: rgba(248,250,252,0.6);
      background: rgba(255,255,255,0.07);
      box-shadow: 0 4px 18px rgba(0,0,0,0.3);
      transform: translateY(-1px);
    }
    .trailer-btn:active { transform: translateY(0); }
    .trailer-play {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      background: rgba(255,255,255,0.12);
      border-radius: 50%;
      flex-shrink: 0;
      transition: background 0.18s;
    }
    .trailer-btn:hover .trailer-play { background: rgba(255,255,255,0.2); }
    .fav-btn {
      display: flex; align-items: center; justify-content: center;
      width: 42px; height: 42px; border-radius: 50%;
      border: 1px solid rgba(248,113,113,0.25);
      background: rgba(248,113,113,0.06);
      cursor: pointer;
      transition: background 0.18s, border-color 0.18s;
    }
    .fav-btn:hover { background: rgba(248,113,113,0.14); border-color: rgba(248,113,113,0.5); }
    .fav-btn.active { background: rgba(248,113,113,0.15); border-color: rgba(248,113,113,0.5); }
    @media (max-width: 600px) {
      .hero { flex-direction: column; }
      .poster { width: 100%; height: auto; }
    }
  `],
})
export class MovieDetailComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly movieSvc   = inject(MovieService);
  private readonly accountSvc = inject(AccountService);
  private readonly reviewSvc  = inject(ReviewService);
  private readonly translate  = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly language   = inject(LanguageService);
  readonly auth               = inject(AuthService);

  readonly movie      = signal<MovieDetail | null>(null);
  readonly showtimes  = signal<Showtime[]>([]);
  readonly loading    = signal(true);
  readonly isFavorite = signal(false);

  readonly reviewData = signal<MovieReviewsDto | null>(null);
  readonly canReview  = signal(false);
  readonly myReview   = signal<ReviewDto | null>(null);
  readonly submitting = signal(false);
  readonly reviewError = signal<string | null>(null);

  readonly roundedRating = computed(() =>
    Math.round(this.reviewData()?.averageRating ?? 0));

  formRating  = 7;
  formComment = '';

  private movieId = 0;

  ngOnInit(): void {
    this.movieId = Number(this.route.snapshot.paramMap.get('id'));

    this.language.lang$.pipe(
      switchMap(lang => {
        this.loading.set(this.movie() === null);
        return this.loadPageData(lang).pipe(
          catchError(() => of(null)),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(data => {
      if (!data) {
        this.loading.set(false);
        return;
      }

      this.movie.set(data.movie);
      this.showtimes.set(data.showtimes);
      this.reviewData.set(data.reviews);
      this.loading.set(false);
    });

    if (this.auth.isLoggedIn()) {
      // паралельно перевіряємо обране і можливість відгуку
      this.accountSvc.getFavorites().subscribe({
        next: favs => this.isFavorite.set(favs.some(f => f.movieId === this.movieId)),
        error: () => {},
      });
      this.reviewSvc.canReview(this.movieId).subscribe({
        next: r => {
          this.canReview.set(r.canReview);
          this.myReview.set(r.myReview);
        },
        error: () => {},
      });
    }
  }

  toggleFavorite(): void {
    const action = this.isFavorite()
      ? this.accountSvc.removeFavorite(this.movieId)
      : this.accountSvc.addFavorite(this.movieId);
    action.subscribe({ next: () => this.isFavorite.update(v => !v) });
  }

  submitReview(): void {
    if (this.formRating < 1 || !this.formComment.trim()) return;
    this.submitting.set(true);
    this.reviewError.set(null);

    this.reviewSvc.submit(this.movieId, this.formRating, this.formComment).subscribe({
      next: dto => {
        this.myReview.set(dto);
        this.submitting.set(false);
        this.formComment = '';
        this.refreshReviews();
      },
      error: err => {
        this.reviewError.set(typeof err.error === 'string' ? err.error : 'common.error');
        this.submitting.set(false);
      },
    });
  }

  canDeleteReview(review: ReviewDto): boolean {
    return this.auth.hasRole('Admin') || review.userId === this.auth.currentUserId();
  }

  deleteReview(review: ReviewDto): void {
    this.reviewSvc.delete(review.id).subscribe({
      next: () => {
        if (this.myReview()?.id === review.id) this.myReview.set(null);
        this.refreshReviews();
      },
    });

  }

  private refreshReviews(): void {
    this.reviewSvc.getMovieReviews(this.movieId).subscribe({
      next: d => this.reviewData.set(d),
    });
  }

  private loadPageData(lang = this.language.currentLang) {
    return forkJoin({
      movie:     this.movieSvc.getMovie(this.movieId, lang),
      showtimes: this.movieSvc.getShowtimes({ movieId: this.movieId }),
      reviews:   this.reviewSvc.getMovieReviews(this.movieId),
    });
  }
}

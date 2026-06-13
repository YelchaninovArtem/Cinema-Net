import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-buy-success',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  template: `
    <div class="success-page">
      <div class="success-card">
        <div class="success-icon">
          <svg width="56" height="56" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="12" r="12" fill="rgba(52,211,153,0.15)"/>
            <path d="M7 12.5l3.5 3.5 6.5-7" stroke="#34d399" stroke-width="2.2"
                  stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </div>

        <h1 class="success-title">{{ 'payment.successTitle' | translate }}</h1>
        <p class="success-msg">
          {{ (auth.isLoggedIn() ? 'payment.successMsg' : 'payment.successMsgGuest') | translate }}
        </p>

        @if (movieTitle()) {
          <div class="success-movie">
            <span class="success-movie-label">{{ ticketsForMovieKey() | translate }}</span>
            <strong class="success-movie-title">{{ movieTitle() }}</strong>
          </div>
        }

        <div class="success-actions">
          @if (auth.isLoggedIn()) {
            <a class="success-btn success-btn--primary" routerLink="/account">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                <path d="M20 12c0-1.1.9-2 2-2V6c0-1.1-.9-2-2-2H4c-1.1 0-2 .9-2 2v4c1.1 0 2 .9 2 2s-.9 2-2 2v4c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2v-4c-1.1 0-2-.9-2-2z"/>
              </svg>
              {{ 'payment.viewTickets' | translate }}
            </a>
          }
          <a class="success-btn success-btn--ghost" routerLink="/catalog">
            {{ 'catalog.back' | translate }}
          </a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: var(--app-content-height);
      background:
        radial-gradient(ellipse 50% 40% at 50% 0%, rgba(52,211,153,0.06) 0%, transparent 55%),
        radial-gradient(ellipse at 80% 85%, rgba(99,102,241,0.05) 0%, transparent 45%),
        #060a12;
      font-family: 'DM Sans', sans-serif;
    }
    .success-page {
      min-height: var(--app-content-height);
      box-sizing: border-box;
      display: flex; align-items: center; justify-content: center;
      padding: 24px 16px;
    }
    .success-card {
      width: 100%; max-width: 420px;
      background: #0d1422;
      border-radius: 20px;
      border: 1px solid rgba(52,211,153,0.12);
      box-shadow: 0 32px 72px rgba(0,0,0,0.55), 0 0 0 1px rgba(52,211,153,0.04);
      padding: 48px 32px 40px;
      display: flex; flex-direction: column; align-items: center; gap: 20px;
      animation: card-in 0.5s cubic-bezier(0.22,1,0.36,1) both;
      text-align: center;
    }
    @keyframes card-in {
      from { opacity: 0; transform: translateY(32px) scale(0.95); }
      to   { opacity: 1; transform: translateY(0) scale(1); }
    }
    .success-icon { animation: pop 0.6s 0.1s cubic-bezier(0.34,1.56,0.64,1) both; }
    @keyframes pop {
      from { transform: scale(0.4); opacity: 0; }
      to   { transform: scale(1); opacity: 1; }
    }
    .success-title {
      margin: 0;
      font-family: 'Syne', sans-serif;
      font-size: 28px; font-weight: 800;
      color: #f1f5f9; letter-spacing: -0.01em;
    }
    .success-msg { margin: 0; font-size: 14px; color: #64748b; line-height: 1.6; }
    .success-movie {
      width: 100%;
      box-sizing: border-box;
      padding: 14px 20px;
      background: rgba(212,168,83,0.08);
      border: 1px solid rgba(212,168,83,0.2);
      border-radius: 12px;
      display: flex;
      flex-direction: column;
      gap: 5px;
    }
    .success-movie-label {
      font-size: 12px;
      color: #94a3b8;
    }
    .success-movie-title {
      font-family: 'Syne', sans-serif;
      font-size: 17px;
      color: #d4a853;
    }
    .success-actions {
      display: flex; flex-direction: column; gap: 10px;
      width: 100%; margin-top: 8px;
    }
    .success-btn {
      display: flex; align-items: center; justify-content: center; gap: 8px;
      padding: 13px 20px;
      border-radius: 10px;
      font-size: 14px; font-weight: 700;
      font-family: 'Syne', sans-serif;
      text-decoration: none; cursor: pointer;
      transition: opacity 0.18s, transform 0.15s, box-shadow 0.18s;
    }
    .success-btn--primary {
      background: linear-gradient(135deg, #d4a853 0%, #b8871e 100%);
      color: #07091a;
      box-shadow: 0 4px 20px rgba(212,168,83,0.3);
    }
    .success-btn--primary:hover {
      opacity: 0.88; transform: translateY(-1px);
      box-shadow: 0 8px 28px rgba(212,168,83,0.38);
    }
    .success-btn--ghost {
      background: rgba(255,255,255,0.04);
      border: 1px solid rgba(255,255,255,0.08);
      color: #64748b;
    }
    .success-btn--ghost:hover { background: rgba(255,255,255,0.07); color: #94a3b8; }
  `],
})
export class BuySuccessComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly auth = inject(AuthService);

  readonly movieTitle = signal('');
  readonly ticketCount = signal(0);
  readonly ticketsForMovieKey = computed(() =>
    this.ticketCount() === 1 ? 'payment.ticketForMovie' : 'payment.ticketsForMovie'
  );

  ngOnInit(): void {
    const ids = this.route.snapshot.queryParamMap.get('ticketIds') ?? '';
    this.ticketCount.set(ids ? ids.split(',').map(Number).filter(Boolean).length : 0);
    this.movieTitle.set(this.route.snapshot.queryParamMap.get('movie') ?? '');
  }
}

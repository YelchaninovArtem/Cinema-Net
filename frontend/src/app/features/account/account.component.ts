import { Component, inject, OnInit, signal, HostListener } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { AccountService } from '../../core/services/account.service';
import { ReviewService } from '../../core/services/review.service';
import { FavoriteSummary, LoyaltyBalance } from '../../core/models/account.models';
import { TicketSummary, TicketDetail } from '../../core/models/ticket.models';
import { UserReviewDto } from '../../core/models/review.models';
import { LocalizedDatePipe } from '../../shared/localized-date.pipe';

type Tab = 'tickets' | 'favorites' | 'reviews' | 'loyalty';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [RouterLink, LocalizedDatePipe, CurrencyPipe, TranslateModule],
  template: `
    <div class="account-shell">

      <div class="page-header">
        <h1 class="page-title">{{ 'account.title' | translate }}</h1>
      </div>

      <div class="tab-row">
        <button class="tab-btn" [class.active]="activeTab() === 'tickets'"
                (click)="setTab('tickets')">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
            <path d="M20 12c0-1.1.9-2 2-2V6c0-1.1-.9-2-2-2H4c-1.1 0-2 .9-2 2v4c1.1 0 2 .9 2 2s-.9 2-2 2v4c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2v-4c-1.1 0-2-.9-2-2z"/>
          </svg>
          {{ 'account.tabs.tickets' | translate }}
          @if (tickets().length) {
            <span class="tab-badge">{{ tickets().length }}</span>
          }
        </button>
        <button class="tab-btn" [class.active]="activeTab() === 'favorites'"
                (click)="setTab('favorites')">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
          </svg>
          {{ 'account.tabs.favorites' | translate }}
          @if (favorites().length) {
            <span class="tab-badge">{{ favorites().length }}</span>
          }
        </button>
        <button class="tab-btn" [class.active]="activeTab() === 'reviews'"
                (click)="setTab('reviews')">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
            <path d="M21 6h-2v9H7v2c0 .55.45 1 1 1h10l4 4V7c0-.55-.45-1-1-1zM17 12V3c0-.55-.45-1-1-1H3c-.55 0-1 .45-1 1v14l4-4h10c.55 0 1-.45 1-1z"/>
          </svg>
          {{ 'account.tabs.reviews' | translate }}
          @if (reviews().length) {
            <span class="tab-badge">{{ reviews().length }}</span>
          }
        </button>
        <button class="tab-btn" [class.active]="activeTab() === 'loyalty'"
                (click)="setTab('loyalty')">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
          </svg>
          {{ 'account.tabs.loyalty' | translate }}
        </button>
      </div>

      <!-- ── TICKETS TAB ── -->
      @if (activeTab() === 'tickets') {
        @if (loadingTickets()) {
          <div class="loading-wrap"><div class="spinner"></div></div>
        } @else if (tickets().length === 0) {
          <div class="empty-state">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="#1e2d40">
              <path d="M20 12c0-1.1.9-2 2-2V6c0-1.1-.9-2-2-2H4c-1.1 0-2 .9-2 2v4c1.1 0 2 .9 2 2s-.9 2-2 2v4c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2v-4c-1.1 0-2-.9-2-2z"/>
            </svg>
            <p>{{ 'account.empty.tickets' | translate }}</p>
            <a class="cta-link" routerLink="/catalog">{{ 'catalog.back' | translate }}</a>
          </div>
        } @else {
          <div class="tickets-list">
            @for (t of tickets(); track t.id) {
              <div class="tkt" [class.tkt--open]="expandedId() === t.id">
                <div class="tkt-shell">

                  <!-- Stub -->
                  <div class="tkt-stub" [attr.data-fmt]="t.format" (click)="toggleExpand(t.id)">
                    <span class="tkt-id">#{{ t.id }}</span>
                    <span class="tkt-seat-label">{{ 'account.row' | translate }} {{ t.row }}, {{ 'account.seat' | translate }} {{ t.col }}</span>
                    <div class="tkt-stub-mid">
                      @if (t.format) {
                        <span class="tkt-fmt-badge">{{ t.format }}</span>
                      }
                    </div>
                    <span class="tkt-price">{{ t.finalAmount | currency:'UAH':'symbol':'1.0-0' }}</span>
                  </div>

                  <div class="tkt-perf">
                    <span class="tkt-notch tkt-notch--t"></span>
                    <span class="tkt-notch tkt-notch--b"></span>
                  </div>

                  <!-- Body -->
                  <div class="tkt-body" (click)="toggleExpand(t.id)">
                    <div class="tkt-stamp" [attr.data-status]="t.status">
                      {{ 'account.status.' + t.status | translate }}
                    </div>
                    <a class="tkt-title"
                       [routerLink]="['/catalog/movies', t.movieId]"
                       (click)="$event.stopPropagation()">
                      {{ t.movieTitle }}
                    </a>
                    <div class="tkt-meta">
                      <svg width="11" height="11" viewBox="0 0 24 24" fill="#334155" style="flex-shrink:0">
                        <path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67V7z"/>
                      </svg>
                      <span>{{ t.showtimeUtc | localizedDate:'d MMM y' }}</span>
                      <span class="tkt-dot">·</span>
                      <span class="tkt-time">{{ t.showtimeUtc | localizedDate:'HH:mm' }}</span>
                      <span class="tkt-dot">·</span>
                      <span>{{ t.hallName }}</span>
                    </div>
                    <div class="tkt-footer">
                      @if (canRefund(t)) {
                        <button class="refund-btn"
                                type="button"
                                [disabled]="refundingTicketId() === t.id"
                                (click)="refundTicket(t, $event)">
                          @if (refundingTicketId() === t.id) {
                            <span class="refund-spinner"></span>
                            {{ 'account.refunding' | translate }}
                          } @else {
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                              <path d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.96-.7 2.8l1.46 1.46A7.93 7.93 0 0 0 20 12c0-4.42-3.58-8-8-8Zm-6 8c0-1.01.25-1.96.7-2.8L5.24 7.74A7.93 7.93 0 0 0 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3c-3.31 0-6-2.69-6-6Z"/>
                            </svg>
                            {{ 'account.refundTicket' | translate }}
                          }
                        </button>
                      }
                      <svg class="expand-arrow" [class.open]="expandedId() === t.id"
                           width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z"/>
                      </svg>
                    </div>
                    @if (ticketActionMessage().ticketId === t.id) {
                      <p class="ticket-action-msg" [class.error]="ticketActionMessage().kind === 'error'">
                        {{ ticketActionMessage().key | translate }}
                      </p>
                    }
                  </div>
                </div>

                <!-- QR detail -->
                @if (expandedId() === t.id) {
                  <div class="ticket-detail">
                    @if (loadingDetail()) {
                      <div class="detail-loading"><div class="spinner spinner--sm"></div></div>
                    } @else if (detail()) {
                      <div class="qr-wrap">
                        @if (t.status === 'Paid') {
                          @if (qrBlobUrl()) {
                            <div class="qr-img-wrap">
                              <img [src]="qrBlobUrl()!" [alt]="'QR ' + t.id" class="qr-img" />
                              <button class="qr-expand-btn" (click)="lightboxTicketId.set(t.id)">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                                  <path d="M7 14H5v5h5v-2H7v-3zm-2-4h2V7h3V5H5v5zm12 7h-3v2h5v-5h-2v3zM14 5v2h3v3h2V5h-5z"/>
                                </svg>
                              </button>
                            </div>
                          } @else {
                            <div class="qr-loading"><div class="spinner spinner--sm"></div></div>
                          }
                        } @else {
                          <div class="qr-status">
                            {{ 'account.status.' + t.status | translate }}
                          </div>
                        }
                        <div class="qr-info">
                          <div class="qr-info-row">
                            <span class="qr-ticket-id">{{ 'account.ticketNumber' | translate }} #{{ detail()!.id }}</span>
                          </div>
                          <div class="qr-info-row">
                            <span class="qr-label">{{ 'account.seat' | translate:{row: detail()!.seat.row, col: detail()!.seat.col} }}</span>
                            <span class="qr-type">{{ detail()!.seat.type }}</span>
                          </div>
                          <div class="qr-info-row">
                            <span class="qr-hall">{{ detail()!.hallName }}</span>
                          </div>
                        </div>
                      </div>
                    }
                  </div>
                }
              </div>
            }
          </div>
        }
      }

      <!-- ── FAVORITES TAB ── -->
      @if (activeTab() === 'favorites') {
        @if (loadingFavorites()) {
          <div class="loading-wrap"><div class="spinner"></div></div>
        } @else if (favorites().length === 0) {
          <div class="empty-state">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="#1e2d40">
              <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
            </svg>
            <p>{{ 'account.empty.favorites' | translate }}</p>
            <a class="cta-link" routerLink="/catalog">{{ 'catalog.back' | translate }}</a>
          </div>
        } @else {
          <div class="fav-grid">
            @for (fav of favorites(); track fav.movieId) {
              <div class="fav-card">
                <div class="fav-poster">
                  @if (fav.posterUrl) {
                    <img [src]="fav.posterUrl" [alt]="fav.title" class="poster-img" loading="lazy" />
                  } @else {
                    <div class="poster-placeholder">
                      <svg width="32" height="32" viewBox="0 0 24 24" fill="#1e2d40">
                        <path d="M18 4l2 4h-3l-2-4h-2l2 4h-3l-2-4H8l2 4H7L5 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V4h-4z"/>
                      </svg>
                    </div>
                  }
                  <button class="fav-remove" (click)="removeFavorite(fav.movieId)">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                      <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
                    </svg>
                  </button>
                </div>
                <div class="fav-info">
                  <span class="fav-title">{{ fav.title }}</span>
                  <a class="fav-link" [routerLink]="['/catalog/movies', fav.movieId]">
                    {{ 'account.viewMovie' | translate }}
                  </a>
                </div>
              </div>
            }
          </div>
        }
      }

      <!-- ── REVIEWS TAB ── -->
      @if (activeTab() === 'reviews') {
        @if (loadingReviews()) {
          <div class="loading-wrap"><div class="spinner"></div></div>
        } @else if (reviews().length === 0) {
          <div class="empty-state">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="#1e2d40">
              <path d="M21 6h-2v9H7v2c0 .55.45 1 1 1h10l4 4V7c0-.55-.45-1-1-1zM17 12V3c0-.55-.45-1-1-1H3c-.55 0-1 .45-1 1v14l4-4h10c.55 0 1-.45 1-1z"/>
            </svg>
            <p>{{ 'account.empty.reviews' | translate }}</p>
            <a class="cta-link" routerLink="/catalog">{{ 'catalog.back' | translate }}</a>
          </div>
        } @else {
          <div class="reviews-list">
            @for (r of reviews(); track r.id) {
              <div class="review-card">
                <div class="review-card-header">
                  <a class="review-movie" [routerLink]="['/catalog/movies', r.movieId]">{{ r.movieTitle }}</a>
                  <span class="review-date">{{ r.createdUtc | localizedDate:'dd.MM.yyyy' }}</span>
                  <button class="review-delete" type="button" (click)="deleteReview(r.id)">
                    {{ 'account.deleteReview' | translate }}
                  </button>
                </div>
                <div class="review-stars">
                  @for (i of [1,2,3,4,5,6,7,8,9,10]; track i) {
                    <span class="review-star" [class.filled]="i <= r.rating">★</span>
                  }
                  <span class="review-rating">{{ r.rating }}/10</span>
                </div>
                <p class="review-comment">{{ r.comment }}</p>
              </div>
            }
          </div>
        }
      }

      <!-- ── LOYALTY TAB ── -->
      @if (activeTab() === 'loyalty') {
        @if (loadingLoyalty()) {
          <div class="loading-wrap"><div class="spinner"></div></div>
        } @else {
          <div class="loyalty-panel">
            <div class="loyalty-hero">
              <div class="loyalty-star">
                <svg width="40" height="40" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                </svg>
              </div>
              <div class="loyalty-stats">
                <div class="loyalty-balance">{{ loyaltyData()?.balance ?? 0 }}</div>
                <div class="loyalty-balance-label">{{ 'loyalty.availablePoints' | translate }}</div>
              </div>
            </div>
            <div class="loyalty-cards">
              <div class="loyalty-card">
                <div class="lc-label">{{ 'loyalty.totalEarned' | translate }}</div>
                <div class="lc-value">{{ loyaltyData()?.totalEarned ?? 0 }}</div>
              </div>
              <div class="loyalty-card">
                <div class="lc-label">{{ 'loyalty.rate' | translate }}</div>
                <div class="lc-value">1 : 10</div>
                <div class="lc-note">{{ 'loyalty.rateNote' | translate }}</div>
              </div>
              <div class="loyalty-card">
                <div class="lc-label">{{ 'loyalty.maxDiscount' | translate }}</div>
                <div class="lc-value">50%</div>
                <div class="lc-note">{{ 'loyalty.maxDiscountNote' | translate }}</div>
              </div>
            </div>
            <div class="loyalty-how">
              <h3 class="how-title">{{ 'loyalty.howItWorks' | translate }}</h3>
              <div class="how-steps">
                <div class="how-step"><div class="step-num">1</div><div>{{ 'loyalty.step1' | translate }}</div></div>
                <div class="how-step"><div class="step-num">2</div><div>{{ 'loyalty.step2' | translate }}</div></div>
                <div class="how-step"><div class="step-num">3</div><div>{{ 'loyalty.step3' | translate }}</div></div>
              </div>
            </div>
          </div>
        }
      }

    </div>

    <!-- QR Lightbox -->
    @if (lightboxTicketId() !== null) {
      <div class="qr-lightbox" (click)="closeLightbox()">
        <div class="qr-lightbox-inner" (click)="$event.stopPropagation()">
          <button class="qr-lightbox-close" (click)="closeLightbox()">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
              <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
            </svg>
          </button>
          <img [src]="qrBlobUrl()!" alt="QR fullscreen" class="qr-lightbox-img" />
          <p class="qr-lightbox-hint">{{ 'account.qrHint' | translate }}</p>
        </div>
      </div>
    }
  `,
  styles: [`
    :host { display: block; }

    .account-shell {
      max-width: 900px;
      margin: 0 auto;
      padding: 32px 16px 48px;
      font-family: 'DM Sans', sans-serif;
      color: #e2e8f0;
      zoom: 1.15;
    }

    .page-header { margin-bottom: 28px; }
    .page-title {
      font-family: 'Syne', sans-serif;
      font-size: 30px; font-weight: 800; color: #f8fafc;
      margin: 0; letter-spacing: -0.02em;
    }

    .tab-row {
      display: flex; gap: 4px; margin-bottom: 28px;
      background: rgba(255,255,255,0.04); border-radius: 12px; padding: 4px;
    }
    .tab-btn {
      flex: 1; display: flex; align-items: center; justify-content: center; gap: 8px;
      padding: 12px 16px; border: none; border-radius: 9px;
      background: transparent; color: #4a6080;
      font-size: 13px; font-weight: 600; font-family: 'DM Sans', sans-serif;
      cursor: pointer; transition: background 0.18s, color 0.18s, box-shadow 0.18s;
    }
    .tab-btn.active { background: #0d1422; color: #f1f5f9; box-shadow: 0 1px 6px rgba(0,0,0,0.4); }
    .tab-btn svg { flex-shrink: 0; }
    .tab-badge {
      background: rgba(99,102,241,0.2); color: #818cf8;
      border-radius: 99px; padding: 1px 7px; font-size: 11px; font-weight: 700;
    }
    .tab-btn.active .tab-badge { background: rgba(99,102,241,0.3); }

    .loading-wrap { display: flex; justify-content: center; padding: 60px 0; }
    .spinner {
      width: 36px; height: 36px;
      border: 3px solid rgba(99,102,241,0.15); border-top-color: #6366f1;
      border-radius: 50%; animation: spin 0.8s linear infinite;
    }
    .spinner--sm { width: 22px; height: 22px; border-width: 2px; }
    @keyframes spin { to { transform: rotate(360deg); } }

    .empty-state {
      display: flex; flex-direction: column; align-items: center;
      gap: 16px; padding: 64px 24px; color: #2a3a50; text-align: center;
    }
    .empty-state p { font-size: 15px; color: #4a6080; margin: 0; }
    .cta-link { font-size: 13px; color: #6366f1; text-decoration: none; font-weight: 600; transition: color 0.18s; }
    .cta-link:hover { color: #818cf8; }

    /* Tickets list */
    .tickets-list { display: flex; flex-direction: column; gap: 14px; }

    .tkt {
      border-radius: 14px; overflow: hidden;
      box-shadow: 0 4px 22px rgba(0,0,0,0.5);
      transition: box-shadow 0.22s, transform 0.22s;
    }
    .tkt:hover { box-shadow: 0 10px 38px rgba(0,0,0,0.65); transform: translateY(-2px); }
    .tkt--open { box-shadow: 0 0 0 1px rgba(212,168,83,0.2), 0 14px 44px rgba(0,0,0,0.6); }

    .tkt-shell { display: flex; min-height: 100px; position: relative; }

    .tkt-stub {
      width: 80px; flex-shrink: 0;
      background: linear-gradient(155deg, #101d38 0%, #060d1c 100%);
      display: flex; flex-direction: column; align-items: center;
      justify-content: flex-start; gap: 10px; padding: 14px 8px;
      cursor: pointer; user-select: none; position: relative; overflow: hidden;
    }
    .tkt-stub::before {
      content: ''; position: absolute; left: 0; top: 0; bottom: 0; width: 3px;
      background: linear-gradient(180deg, #d4a853 0%, #8b5e1a 50%, #d4a853 100%);
    }
    .tkt-stub[data-fmt="3D"]::before {
      background: linear-gradient(180deg, #38bdf8 0%, #0369a1 50%, #38bdf8 100%);
    }
    .tkt-stub[data-fmt="IMAX"]::before {
      background: linear-gradient(180deg, #a78bfa 0%, #6d28d9 50%, #a78bfa 100%);
    }

    .tkt-fmt-badge {
      font-family: 'Syne', sans-serif; font-size: 11px; font-weight: 900;
      letter-spacing: 0.12em; text-transform: uppercase;
      padding: 2px 6px; border-radius: 4px;
      background: rgba(212,168,83,0.15); color: #d4a853;
      border: 1px solid rgba(212,168,83,0.35);
    }
    .tkt-stub[data-fmt="3D"] .tkt-fmt-badge {
      background: rgba(56,189,248,0.12); color: #38bdf8;
      border-color: rgba(56,189,248,0.35);
    }
    .tkt-stub[data-fmt="IMAX"] .tkt-fmt-badge {
      background: rgba(167,139,250,0.12); color: #a78bfa;
      border-color: rgba(167,139,250,0.35);
    }
    .tkt-seat-label {
      font-family: 'Syne', sans-serif; font-size: 11px; font-weight: 700;
      color: #d4a853; letter-spacing: 0.05em; text-align: center; z-index: 1;
    }
    .tkt-id {
      z-index: 1;
      font-family: 'Syne', sans-serif;
      font-size: 12px;
      font-weight: 900;
      color: #f1f5f9;
      letter-spacing: 0.04em;
    }
    .tkt-price {
      font-family: 'Syne', sans-serif; font-size: 13px; font-weight: 700;
      color: #3a5070; z-index: 1; text-align: center; margin-top: auto;
    }
    .tkt-stub-mid { z-index: 1; }

    .tkt-perf { width: 22px; flex-shrink: 0; background: #0d1422; position: relative; }
    .tkt-perf::after {
      content: ''; position: absolute; top: 10px; bottom: 10px; left: 50%;
      transform: translateX(-1px); width: 0;
      border-left: 2px dashed rgba(255,255,255,0.06);
    }
    .tkt-notch {
      position: absolute; left: 50%; transform: translateX(-50%);
      width: 20px; height: 10px; background: #070b13; z-index: 4;
    }
    .tkt-notch--t { top: 0; border-radius: 0 0 10px 10px; }
    .tkt-notch--b { bottom: 0; border-radius: 10px 10px 0 0; }

    .tkt-body {
      flex: 1; min-width: 0; background: #0d1422;
      padding: 14px 18px 14px 14px;
      display: flex; flex-direction: column; gap: 5px;
      cursor: pointer; user-select: none; position: relative;
    }
    .tkt-stamp {
      position: absolute; top: 13px; right: 44px;
      padding: 3px 9px; border-radius: 3px;
      font-size: 8px; font-weight: 900; letter-spacing: 0.22em;
      text-transform: uppercase; transform: rotate(-8deg);
      border: 1.5px solid currentColor; opacity: 0.75; pointer-events: none;
    }
    .tkt-stamp[data-status="Paid"]           { color: #4ade80; }
    .tkt-stamp[data-status="Used"]           { color: #60a5fa; }
    .tkt-stamp[data-status="NotUsed"]        { color: #94a3b8; }
    .tkt-stamp[data-status="PendingPayment"] { color: #fbbf24; }
    .tkt-stamp[data-status="Cancelled"]     { color: #f87171; }
    .tkt-stamp[data-status="Refunded"]      { color: #f87171; }

    .tkt-title {
      font-family: 'Syne', sans-serif; font-size: 16px; font-weight: 800;
      color: #f1f5f9; white-space: nowrap; overflow: hidden;
      text-overflow: ellipsis; padding-right: 96px; letter-spacing: -0.01em;
      text-decoration: none; width: fit-content; max-width: 100%;
      transition: color 0.18s, text-decoration-color 0.18s;
    }
    .tkt-title:hover {
      color: #d4a853;
      text-decoration: underline;
      text-decoration-thickness: 2px;
      text-underline-offset: 4px;
    }
    .tkt-meta {
      display: flex; align-items: center; gap: 5px;
      font-size: 11px; color: #2a3a52; flex-wrap: wrap;
    }
    .tkt-dot { color: #182330; }
    .tkt-time { color: #3d5570; font-weight: 600; }
    .tkt-format { color: #d4a853; font-size: 10px; font-weight: 800; letter-spacing: 0.08em; text-transform: uppercase; }
    .tkt-footer { display: flex; align-items: center; justify-content: flex-end; gap: 8px; margin-top: auto; padding-top: 4px; }
    .refund-btn {
      display: inline-flex; align-items: center; justify-content: center; gap: 6px;
      min-height: 30px; padding: 0 11px; border-radius: 6px;
      border: 1px solid rgba(212,168,83,0.34);
      background: rgba(212,168,83,0.08); color: #d4a853;
      font-size: 11px; font-weight: 800; font-family: 'DM Sans', sans-serif;
      cursor: pointer; transition: background 0.18s, border-color 0.18s, color 0.18s;
    }
    .refund-btn:hover:not(:disabled) { background: rgba(212,168,83,0.16); border-color: rgba(212,168,83,0.55); color: #f2c86f; }
    .refund-btn:disabled { opacity: 0.55; cursor: wait; }
    .refund-spinner {
      width: 12px; height: 12px; border-radius: 50%;
      border: 2px solid rgba(212,168,83,0.22); border-top-color: #d4a853;
      animation: spin 0.75s linear infinite;
    }
    .ticket-action-msg {
      margin: 4px 0 0; color: #4ade80; font-size: 11px; font-weight: 700;
    }
    .ticket-action-msg.error { color: #f87171; }
    .expand-arrow { color: #1e2d40; transition: transform 0.22s, color 0.18s; flex-shrink: 0; }
    .expand-arrow.open { transform: rotate(180deg); color: #d4a853; }

    /* QR detail */
    .ticket-detail {
      background: #080e1a;
      border-top: 1px dashed rgba(212,168,83,0.1);
      padding: 20px;
      animation: slide-down 0.2s ease both;
    }
    @keyframes slide-down {
      from { opacity: 0; transform: translateY(-6px); }
      to   { opacity: 1; transform: translateY(0); }
    }
    .detail-loading { display: flex; justify-content: center; padding: 20px 0; }

    .qr-wrap { display: flex; gap: 20px; align-items: flex-start; flex-wrap: wrap; }
    .qr-img-wrap { position: relative; width: 140px; height: 140px; flex-shrink: 0; }
    .qr-img {
      width: 140px; height: 140px; border-radius: 8px;
      background: #fff; padding: 6px; box-sizing: border-box; display: block;
    }
    .qr-expand-btn {
      position: absolute; inset: 0; width: 100%; height: 100%;
      border: none; border-radius: 8px;
      background: rgba(0,0,0,0); color: transparent; cursor: pointer;
      display: flex; align-items: center; justify-content: center;
      transition: background 0.18s, color 0.18s;
    }
    .qr-expand-btn:hover { background: rgba(0,0,0,0.5); color: #fff; }
    .qr-loading {
      width: 140px; height: 140px;
      display: flex; align-items: center; justify-content: center;
      background: rgba(255,255,255,0.04); border-radius: 8px;
    }
    .qr-status {
      min-width: 140px; min-height: 80px; border-radius: 8px;
      display: flex; align-items: center; justify-content: center;
      background: rgba(96,165,250,0.08); border: 1px solid rgba(96,165,250,0.18);
      color: #60a5fa; font-size: 12px; font-weight: 800;
      letter-spacing: 0.12em; text-transform: uppercase;
    }
    .qr-info { display: flex; flex-direction: column; gap: 8px; padding-top: 4px; }
    .qr-info-row { display: flex; align-items: center; gap: 8px; }
    .qr-label { font-size: 14px; color: #94a3b8; font-weight: 500; }
    .qr-ticket-id {
      font-family: 'Syne', sans-serif;
      font-size: 15px;
      font-weight: 800;
      color: #d4a853;
    }
    .qr-type { font-size: 11px; color: #4a6080; text-transform: uppercase; letter-spacing: 0.06em; }
    .qr-hall { font-size: 12px; color: #4a6080; }

    /* QR Lightbox */
    .qr-lightbox {
      position: fixed; inset: 0; z-index: 1000;
      background: rgba(0,0,0,0.88);
      display: flex; align-items: center; justify-content: center;
      padding: 24px;
      animation: fade-in 0.18s ease;
      backdrop-filter: blur(6px);
    }
    @keyframes fade-in { from { opacity: 0; } to { opacity: 1; } }
    .qr-lightbox-inner {
      position: relative; display: flex; flex-direction: column;
      align-items: center; gap: 16px;
      animation: zoom-in 0.2s cubic-bezier(0.34, 1.56, 0.64, 1);
    }
    @keyframes zoom-in {
      from { transform: scale(0.7); opacity: 0; }
      to   { transform: scale(1); opacity: 1; }
    }
    .qr-lightbox-close {
      position: absolute; top: -44px; right: -8px;
      width: 36px; height: 36px; border-radius: 50%;
      border: 1px solid rgba(255,255,255,0.15);
      background: rgba(255,255,255,0.08); color: #e2e8f0;
      cursor: pointer; display: flex; align-items: center; justify-content: center;
      transition: background 0.18s;
    }
    .qr-lightbox-close:hover { background: rgba(255,255,255,0.16); }
    .qr-lightbox-img {
      width: min(80vw, 80vh); height: min(80vw, 80vh);
      background: #fff; border-radius: 16px; padding: 16px;
      box-sizing: border-box; box-shadow: 0 32px 80px rgba(0,0,0,0.7);
    }
    .qr-lightbox-hint { font-size: 13px; color: rgba(255,255,255,0.45); text-align: center; margin: 0; }

    /* Favorites */
    .fav-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 16px; }
    .fav-card {
      background: #0d1422; border: 1px solid rgba(255,255,255,0.07);
      border-radius: 12px; overflow: hidden;
      transition: border-color 0.18s, transform 0.18s;
    }
    .fav-card:hover { border-color: rgba(255,255,255,0.14); transform: translateY(-2px); }
    .fav-poster { position: relative; aspect-ratio: 2/3; overflow: hidden; }
    .poster-img { width: 100%; height: 100%; object-fit: cover; display: block; }
    .poster-placeholder {
      width: 100%; height: 100%; background: #0a0f1c;
      display: flex; align-items: center; justify-content: center;
    }
    .fav-remove {
      position: absolute; top: 8px; right: 8px; width: 30px; height: 30px;
      border-radius: 50%; border: none; background: rgba(0,0,0,0.65);
      color: #f87171; cursor: pointer;
      display: flex; align-items: center; justify-content: center;
      opacity: 0; transition: opacity 0.18s, background 0.18s;
    }
    .fav-card:hover .fav-remove { opacity: 1; }
    .fav-remove:hover { background: rgba(239,68,68,0.25); }
    .fav-info { padding: 10px 12px 12px; display: flex; flex-direction: column; gap: 6px; }
    .fav-title {
      font-size: 13px; font-weight: 600; color: #f1f5f9; line-height: 1.3;
      display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;
    }
    .fav-link { font-size: 12px; color: #6366f1; text-decoration: none; font-weight: 600; transition: color 0.18s; }
    .fav-link:hover { color: #818cf8; }

    /* Reviews */
    .reviews-list { display: flex; flex-direction: column; gap: 12px; }
    .review-card {
      background: #0d1422;
      border: 1px solid rgba(255,255,255,0.07);
      border-radius: 12px;
      padding: 18px 20px;
    }
    .review-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 10px;
      flex-wrap: wrap;
    }
    .review-movie {
      color: #f1f5f9;
      font-family: 'Syne', sans-serif;
      font-size: 15px;
      font-weight: 800;
      text-decoration: none;
      transition: color 0.18s;
    }
    .review-movie:hover { color: #d4a853; text-decoration: underline; text-underline-offset: 4px; }
    .review-date { color: #4a6080; font-size: 12px; margin-left: auto; }
    .review-delete {
      background: none;
      border: 1px solid rgba(248,113,113,0.3);
      border-radius: 6px;
      color: #f87171;
      font-size: 12px;
      padding: 4px 10px;
      cursor: pointer;
      transition: background 0.18s, border-color 0.18s;
    }
    .review-delete:hover { background: rgba(248,113,113,0.1); border-color: rgba(248,113,113,0.55); }
    .review-stars { display: flex; align-items: center; gap: 2px; margin-bottom: 8px; }
    .review-star { color: #2a3a50; font-size: 14px; }
    .review-star.filled { color: #d4a853; }
    .review-rating { color: #d4a853; font-size: 12px; font-weight: 700; margin-left: 6px; }
    .review-comment { color: #94a3b8; font-size: 14px; line-height: 1.55; margin: 0; }

    /* Loyalty */
    .loyalty-panel { display: flex; flex-direction: column; gap: 24px; }
    .loyalty-hero {
      background: linear-gradient(135deg, #0d1a38 0%, #13113a 100%);
      border: 1px solid rgba(99,102,241,0.2); border-radius: 16px;
      padding: 32px 28px; display: flex; align-items: center; gap: 24px;
    }
    .loyalty-star {
      width: 80px; height: 80px; border-radius: 50%;
      background: rgba(212,168,83,0.12); border: 2px solid rgba(212,168,83,0.25);
      display: flex; align-items: center; justify-content: center;
      color: #d4a853; flex-shrink: 0; box-shadow: 0 0 40px rgba(212,168,83,0.15);
    }
    .loyalty-stats { flex: 1; }
    .loyalty-balance {
      font-family: 'Syne', sans-serif; font-size: 48px; font-weight: 800;
      color: #f8fafc; line-height: 1; letter-spacing: -0.03em;
    }
    .loyalty-balance-label { font-size: 14px; color: #4a6080; margin-top: 4px; }
    .loyalty-cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 12px; }
    .loyalty-card { background: #0d1422; border: 1px solid rgba(255,255,255,0.07); border-radius: 12px; padding: 18px 20px; }
    .lc-label { font-size: 12px; color: #4a6080; text-transform: uppercase; letter-spacing: 0.06em; margin-bottom: 8px; }
    .lc-value { font-family: 'Syne', sans-serif; font-size: 28px; font-weight: 800; color: #d4a853; letter-spacing: -0.02em; }
    .lc-note { font-size: 11px; color: #4a6080; margin-top: 4px; }
    .loyalty-how { background: #0d1422; border: 1px solid rgba(255,255,255,0.07); border-radius: 14px; padding: 24px; }
    .how-title { font-family: 'Syne', sans-serif; font-size: 16px; font-weight: 700; color: #f8fafc; margin: 0 0 16px; }
    .how-steps { display: flex; flex-direction: column; gap: 12px; }
    .how-step { display: flex; align-items: center; gap: 14px; font-size: 14px; color: #94a3b8; }
    .step-num {
      width: 26px; height: 26px; border-radius: 50%;
      background: rgba(99,102,241,0.15); border: 1px solid rgba(99,102,241,0.3);
      color: #818cf8; font-size: 12px; font-weight: 700;
      display: flex; align-items: center; justify-content: center; flex-shrink: 0;
    }
  `],
})
export class AccountComponent implements OnInit {
  private readonly accountSvc = inject(AccountService);
  private readonly reviewSvc = inject(ReviewService);

  readonly activeTab       = signal<Tab>('tickets');
  readonly tickets         = signal<TicketSummary[]>([]);
  readonly favorites       = signal<FavoriteSummary[]>([]);
  readonly reviews         = signal<UserReviewDto[]>([]);
  readonly loyaltyData     = signal<LoyaltyBalance | null>(null);
  readonly detail          = signal<TicketDetail | null>(null);
  readonly expandedId      = signal<number | null>(null);
  readonly loadingTickets  = signal(true);
  readonly loadingFavorites = signal(true);
  readonly loadingReviews  = signal(true);
  readonly loadingLoyalty  = signal(true);
  readonly loadingDetail   = signal(false);
  readonly qrBlobUrl       = signal<string | null>(null);
  readonly lightboxTicketId = signal<number | null>(null);
  readonly refundingTicketId = signal<number | null>(null);
  readonly ticketActionMessage = signal<{ ticketId: number | null; key: string; kind: 'success' | 'error' }>({
    ticketId: null,
    key: '',
    kind: 'success',
  });

  @HostListener('document:keydown.escape')
  closeLightbox(): void { this.lightboxTicketId.set(null); }

  ngOnInit(): void {
    this.accountSvc.getTickets().subscribe({
      next: ts => { this.tickets.set(ts); this.loadingTickets.set(false); },
      error: () => this.loadingTickets.set(false),
    });
    this.accountSvc.getFavorites().subscribe({
      next: fs => { this.favorites.set(fs); this.loadingFavorites.set(false); },
      error: () => this.loadingFavorites.set(false),
    });
    this.reviewSvc.getMyReviews().subscribe({
      next: rs => { this.reviews.set(rs); this.loadingReviews.set(false); },
      error: () => this.loadingReviews.set(false),
    });
    this.accountSvc.getLoyaltyBalance().subscribe({
      next: lb => { this.loyaltyData.set(lb); this.loadingLoyalty.set(false); },
      error: () => { this.loyaltyData.set({ balance: 0, totalEarned: 0 }); this.loadingLoyalty.set(false); },
    });
  }

  setTab(tab: Tab): void { this.activeTab.set(tab); }

  toggleExpand(id: number): void {
    if (this.expandedId() === id) {
      this.expandedId.set(null);
      this.detail.set(null);
      this.qrBlobUrl.set(null);
      return;
    }
    this.expandedId.set(id);
    this.detail.set(null);
    this.qrBlobUrl.set(null);
    this.loadingDetail.set(true);

    this.accountSvc.getTicketDetail(id).subscribe({
      next: d => {
        this.detail.set(d);
        this.loadingDetail.set(false);
        if (d.status === 'Paid') {
          this.accountSvc.getQrBlob(id).subscribe({
            next: url => this.qrBlobUrl.set(url),
          });
        }
      },
      error: () => this.loadingDetail.set(false),
    });
  }

  removeFavorite(movieId: number): void {
    this.accountSvc.removeFavorite(movieId).subscribe({
      next: () => this.favorites.update(fs => fs.filter(f => f.movieId !== movieId)),
    });
  }

  canRefund(ticket: TicketSummary): boolean {
    return ticket.status === 'Paid' && new Date(ticket.showtimeUtc).getTime() > Date.now();
  }

  refundTicket(ticket: TicketSummary, event: Event): void {
    event.stopPropagation();
    if (!this.canRefund(ticket) || this.refundingTicketId() !== null) return;

    this.refundingTicketId.set(ticket.id);
    this.ticketActionMessage.set({ ticketId: null, key: '', kind: 'success' });

    this.accountSvc.refundTicket(ticket.id).subscribe({
      next: result => {
        this.refundingTicketId.set(null);
        this.tickets.update(ts => ts.map(t =>
          t.id === ticket.id ? { ...t, status: 'Refunded' as const } : t));
        const currentDetail = this.detail();
        if (currentDetail?.id === ticket.id) {
          this.detail.set({ ...currentDetail, status: 'Refunded' });
          this.qrBlobUrl.set(null);
        }
        this.ticketActionMessage.set({
          ticketId: result.ticketId,
          key: 'account.refundSuccess',
          kind: 'success',
        });
      },
      error: () => {
        this.refundingTicketId.set(null);
        this.ticketActionMessage.set({
          ticketId: ticket.id,
          key: 'account.refundFailed',
          kind: 'error',
        });
      },
    });
  }

  deleteReview(id: number): void {
    this.reviewSvc.delete(id).subscribe({
      next: () => this.reviews.update(rs => rs.filter(r => r.id !== id)),
    });
  }
}

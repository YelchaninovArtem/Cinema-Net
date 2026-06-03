import {
  AfterViewInit, Component, computed, ElementRef, inject,
  OnDestroy, OnInit, signal, ViewChild,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { loadStripe, Stripe, StripeCardElement } from '@stripe/stripe-js';
import { environment } from '../../../../environments/environment';
import { TicketService } from '../../../core/services/ticket.service';
import { PaymentService } from '../../../core/services/payment.service';
import { AccountService } from '../../../core/services/account.service';
import { AuthService } from '../../../core/auth/auth.service';
import { SeatMapComponent } from '../../../shared/seat-map/seat-map.component';
import { SeatCoord, SeatMap, CreateTicketsResponse } from '../../../core/models/ticket.models';
import { LoyaltyBalance } from '../../../core/models/account.models';
import { LocalizedDatePipe } from '../../../shared/localized-date.pipe';

type PaymentTab = 'stripe' | 'paypal';
type Step = 'seats' | 'payment';

@Component({
  selector: 'app-buy-confirm',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule, FormsModule, LocalizedDatePipe, CurrencyPipe, DecimalPipe,
    MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
    TranslateModule, SeatMapComponent,
  ],
  template: `
    <div class="buy-page">

      @if (loading()) {
        <div class="buy-loading"><div class="buy-spinner"></div></div>

      } @else if (seatMap()) {

        <!-- ── Header ── -->
        <header class="buy-header">
          <div class="buy-format-badge">{{ seatMap()!.format }}</div>
          <h1 class="buy-title">{{ seatMap()!.movieTitle }}</h1>
          <div class="buy-meta">
            <span class="buy-meta-item">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
              </svg>
              {{ seatMap()!.cinemaBranchName }}, {{ seatMap()!.city }}
            </span>
            <span class="buy-sep">·</span>
            <span class="buy-meta-item">{{ seatMap()!.hallName }}</span>
            <span class="buy-sep">·</span>
            <span class="buy-meta-item">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                <path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67V7z"/>
              </svg>
              {{ seatMap()!.startUtc | localizedDate:'dd MMM yyyy, HH:mm' }}
            </span>
          </div>
        </header>

        <!-- ── Step: seats ── -->
        @if (step() === 'seats') {
          <div class="buy-map-wrap">
            <app-seat-map [seatMap]="seatMap()!" (selected)="onSeatsSelected($event)" />
            <div class="seat-hold-note">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                <path d="M12 1.75a10.25 10.25 0 1 0 0 20.5 10.25 10.25 0 0 0 0-20.5Zm0 18.5a8.25 8.25 0 1 1 0-16.5 8.25 8.25 0 0 1 0 16.5Zm.75-13.5h-1.5v6.1l4.25 2.55.78-1.28-3.53-2.12V6.75Z"/>
              </svg>
              <span>{{ 'booking.seatHoldNote' | translate }}</span>
            </div>
            @if (isCashier()) {
              <div class="cashier-purchase-note">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm1 15h-2v-2h2v2Zm0-4h-2V7h2v6Z"/>
                </svg>
                <span>{{ 'buy.cashierPurchaseNotAllowed' | translate }}</span>
              </div>
            }
          </div>

          @if (selectedSeats().length > 0) {
            <div class="buy-summary">
              <div class="buy-summary-left">
                <span class="buy-seats-count">{{ selectedSeats().length }}</span>
                <span class="buy-seats-label">{{ 'buy.seatsSelected' | translate }}</span>
              </div>

              <div class="buy-summary-right">
                <div class="buy-total">{{ baseTotal() | currency:'UAH':'symbol':'1.0-0' }}</div>

                @if (!auth.isLoggedIn()) {
                  <form [formGroup]="guestForm" class="buy-guest-form">
                    <mat-form-field appearance="outline" class="buy-field">
                      <mat-label>{{ 'auth.email' | translate }}</mat-label>
                      <input matInput type="email" formControlName="email" />
                      @if (guestForm.controls.email.hasError('required')) {
                        <mat-error>{{ 'validation.required' | translate }}</mat-error>
                      }
                      @if (guestForm.controls.email.hasError('email')) {
                        <mat-error>{{ 'validation.invalidEmail' | translate }}</mat-error>
                      }
                    </mat-form-field>
                  </form>
                }

                @if (seatError()) {
                  <p class="buy-error">{{ seatError() | translate }}</p>
                }

                <button class="buy-cta" [disabled]="submittingTickets() || isCashier()" (click)="proceedToPayment()">
                  @if (submittingTickets()) {
                    <mat-spinner diameter="20" />
                  } @else {
                    {{ 'buy.proceedToPayment' | translate }}
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                      <path d="M8.59 16.59L13.17 12 8.59 7.41 10 6l6 6-6 6-1.41-1.41z"/>
                    </svg>
                  }
                </button>
              </div>
            </div>
          }
        }

        <!-- ── Step: payment ── -->
        @if (step() === 'payment' && ticketsResp()) {
          <div class="pay-card">
            <div class="top-accent"></div>

            <!-- Order summary -->
            <div class="pay-header">
              <h2 class="pay-title">{{ 'payment.title' | translate }}</h2>
              <div class="pay-meta">
                <span class="meta-chip">#{{ ticketsResp()!.paymentId }}</span>
                <span class="meta-amount">{{ finalTotal() | currency:'UAH':'symbol':'1.0-0' }}</span>
              </div>
              @if (promoApplied() || loyaltyApplied()) {
                <div class="pay-breakdown">
                  <span class="pay-breakdown-row">
                    {{ 'buy.baseTotal' | translate }}: {{ baseTotal() | currency:'UAH':'symbol':'1.0-0' }}
                  </span>
                  @if (promoApplied()) {
                    <span class="pay-breakdown-row pay-breakdown-discount">
                      {{ 'buy.promoDiscount' | translate }}: -{{ promoDiscount() | number:'1.0-0' }} UAH
                    </span>
                  }
                  @if (loyaltyApplied()) {
                    <span class="pay-breakdown-row pay-breakdown-discount">
                      {{ 'buy.loyaltyDiscount' | translate }}: -{{ loyaltyDiscount() | number:'1.0-0' }} UAH
                    </span>
                  }
                </div>
              }
            </div>

            <!-- Promo code -->
            <div class="promo-section">
              @if (!promoApplied()) {
                <div class="promo-row">
                  <input
                    class="promo-input"
                    type="text"
                    [(ngModel)]="promoCode"
                    [placeholder]="'payment.promoPlaceholder' | translate"
                    [disabled]="promoLoading()"
                    (keydown.enter)="applyPromo()"
                  />
                  <button class="promo-btn" [disabled]="promoLoading() || !promoCode.trim()" (click)="applyPromo()">
                    @if (promoLoading()) { <span class="btn-spinner btn-spinner--sm"></span> }
                    @else { {{ 'payment.promoApply' | translate }} }
                  </button>
                </div>
              } @else {
                <div class="promo-applied">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="#34d399">
                    <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z"/>
                  </svg>
                  <span>{{ 'payment.promoApplied' | translate }}
                    <strong>-{{ promoDiscount() | number:'1.0-0' }} UAH</strong>
                  </span>
                </div>
              }
              @if (promoError()) {
                <div class="promo-error">{{ promoError() | translate }}</div>
              }
            </div>

            <!-- Loyalty -->
            @if (auth.isLoggedIn() && loyalty()) {
              <div class="loyalty-section">
                @if (!loyaltyApplied()) {
                  <div class="loyalty-balance">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="#d4a853">
                      <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                    </svg>
                    {{ 'buy.loyaltyAvailable' | translate }}: <strong>{{ loyalty()!.balance }} {{ 'loyalty.points' | translate }}</strong>
                  </div>
                  @if (loyalty()!.balance > 0) {
                    <div class="loyalty-row">
                      <input
                        class="promo-input"
                        type="number"
                        [(ngModel)]="loyaltyPointsInput"
                        [placeholder]="'loyalty.pointsPlaceholder' | translate"
                        [max]="maxRedeemable()"
                        min="1"
                      />
                      <button class="promo-btn" [disabled]="!loyaltyPointsInput || loyaltyPointsInput < 1" (click)="applyLoyalty()">
                        {{ 'loyalty.apply' | translate }}
                      </button>
                    </div>
                    <div class="loyalty-hint">{{ 'buy.loyaltyMax' | translate }}: {{ maxRedeemable() }} {{ 'loyalty.points' | translate }}</div>
                  }
                } @else {
                  <div class="promo-applied">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="#d4a853">
                      <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                    </svg>
                    <span>{{ 'buy.loyaltyApplied' | translate: { points: loyaltyPointsApplied() } }}</span>
                    <button class="loyalty-cancel" (click)="cancelLoyalty()">{{ 'loyalty.cancel' | translate }}</button>
                  </div>
                }
              </div>
            }

            <!-- Payment tabs -->
            <div class="tab-switcher">
              <button class="tab-btn" [class.active]="activeTab() === 'stripe'" (click)="setTab('stripe')">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M20 4H4c-1.11 0-2 .89-2 2v12c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-10H4V6h16v2z"/>
                </svg>
                {{ 'payment.stripe' | translate }}
              </button>
              <button class="tab-btn" [class.active]="activeTab() === 'paypal'" (click)="setTab('paypal')">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M7.076 21.337H2.47a.641.641 0 01-.633-.74L4.944 2.59A.859.859 0 015.79 1.97h6.918c2.325 0 4.132.632 5.176 1.828.942 1.083 1.261 2.5 1.033 4.224-.017.132-.035.263-.056.394-.7 4.03-2.95 5.74-6.93 5.74H9.94a.862.862 0 00-.852.729l-.875 5.52a.641.641 0 01-.633.54l-.504.392z"/>
                </svg>
                {{ 'payment.paypal' | translate }}
              </button>
            </div>

            <!-- Stripe tab -->
            @if (activeTab() === 'stripe') {
              <div class="tab-content">
                <div #gpayContainer class="gpay-container" [style.display]="googlePayAvailable() ? 'block' : 'none'"></div>
                <div class="or-divider" [style.display]="googlePayAvailable() ? 'flex' : 'none'">
                  <span>{{ 'buy.orPayByCard' | translate }}</span>
                </div>
                <label class="stripe-label">{{ 'buy.cardDetails' | translate }}</label>
                <div #stripeCard class="stripe-element"></div>
                @if (payError()) {
                  <div class="pay-error">{{ payError() | translate }}</div>
                }
                <button class="pay-btn" [disabled]="processing()" (click)="payWithStripe()">
                  @if (processing()) { <span class="btn-spinner"></span> }
                  @else { {{ 'payment.payNow' | translate }} }
                </button>
              </div>
            }

            <!-- PayPal tab -->
            @if (activeTab() === 'paypal') {
              <div class="tab-content">
                <div class="paypal-info">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="#64748b">
                    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z"/>
                  </svg>
                  <span>{{ 'payment.paypalInfo' | translate }}</span>
                </div>
                <div class="usd-note">
                  ≈ {{ '$' + (usdAmount() | number:'1.2-2') }} USD
                  <span class="usd-rate">{{ 'payment.paypalRate' | translate }}</span>
                </div>
                @if (payError()) {
                  <div class="pay-error">{{ payError() | translate }}</div>
                }
                <button class="pay-btn paypal-btn" [disabled]="processing()" (click)="payWithPayPal()">
                  @if (processing()) { <span class="btn-spinner btn-spinner--dark"></span> }
                  @else { {{ 'payment.redirectToPayPal' | translate }} }
                </button>
              </div>
            }

            <button class="back-link" (click)="backToSeats()">← {{ 'buy.backToSeats' | translate }}</button>
          </div>
        }

      } @else {
        <div class="buy-empty">
          <p>{{ 'booking.notFound' | translate }}</p>
          <a routerLink="/catalog">← {{ 'catalog.back' | translate }}</a>
        </div>
      }

      <a class="buy-back" routerLink="/catalog">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
          <path d="M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z"/>
        </svg>
        {{ 'catalog.back' | translate }}
      </a>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: var(--app-content-height);
      background:
        radial-gradient(ellipse at 20% 0%, rgba(99,102,241,0.08) 0%, transparent 50%),
        radial-gradient(ellipse at 80% 100%, rgba(245,158,11,0.05) 0%, transparent 50%),
        linear-gradient(160deg, #0a0e1a 0%, #070b13 50%, #0c0e18 100%);
      font-family: 'DM Sans', 'Roboto', sans-serif;
      color: #e2e8f0;
    }

    .buy-page {
      max-width: 860px;
      margin: 0 auto;
      padding: 32px 20px 120px;
      display: flex;
      flex-direction: column;
      gap: 24px;
      zoom: 1.15;
    }

    .buy-loading { display: flex; justify-content: center; padding: 100px 0; }
    .buy-spinner {
      width: 44px; height: 44px;
      border: 3px solid rgba(255,255,255,0.08);
      border-top-color: #f59e0b;
      border-radius: 50%;
      animation: spin 0.9s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .buy-header {
      border-left: 3px solid #d4a853;
      padding-left: 20px;
      animation: fade-in 0.5s ease both;
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .buy-format-badge {
      display: inline-flex;
      align-items: center;
      padding: 3px 12px;
      background: rgba(212,168,83,0.15);
      border: 1px solid rgba(212,168,83,0.35);
      border-radius: 20px;
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.15em;
      color: #d4a853;
      text-transform: uppercase;
      width: fit-content;
    }
    .buy-title {
      margin: 0;
      font-family: 'Syne', sans-serif;
      font-size: clamp(22px, 5vw, 36px);
      font-weight: 800;
      line-height: 1.1;
      color: #f1f5f9;
      letter-spacing: -0.01em;
    }
    .buy-meta {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
      font-size: 13px;
      color: #7b8db0;
    }
    .buy-meta-item { display: flex; align-items: center; gap: 5px; }
    .buy-sep { color: rgba(255,255,255,0.15); }

    .buy-map-wrap {
      display: flex;
      flex-direction: column;
      gap: 12px;
      animation: fade-in 0.5s 0.1s ease both;
    }

    .seat-hold-note {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 10px 14px;
      border: 1px solid rgba(212,168,83,0.24);
      border-radius: 10px;
      background: rgba(212,168,83,0.08);
      color: #d9c18e;
      font-size: 13px;
      line-height: 1.4;
      text-align: center;
    }
    .seat-hold-note svg {
      flex: 0 0 auto;
      color: #d4a853;
    }
    .cashier-purchase-note {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 10px 14px;
      border: 1px solid rgba(96,165,250,0.24);
      border-radius: 10px;
      background: rgba(37,99,235,0.1);
      color: #bfdbfe;
      font-size: 13px;
      line-height: 1.4;
      text-align: center;
    }
    .cashier-purchase-note svg {
      flex: 0 0 auto;
      color: #60a5fa;
    }

    /* Bottom summary bar */
    .buy-summary {
      position: fixed;
      bottom: 0; left: 0; right: 0;
      z-index: 100;
      background: rgba(10,14,26,0.88);
      backdrop-filter: blur(20px);
      border-top: 1px solid rgba(212,168,83,0.25);
      box-shadow: 0 -8px 32px rgba(0,0,0,0.5);
      padding: 16px 24px;
      display: flex;
      align-items: flex-start;
      gap: 24px;
      justify-content: space-between;
      flex-wrap: wrap;
      animation: slide-up 0.35s cubic-bezier(0.34,1.3,0.64,1) both;
    }
    @keyframes slide-up {
      from { transform: translateY(100%); opacity: 0; }
      to   { transform: translateY(0); opacity: 1; }
    }
    .buy-summary-left { display: flex; align-items: baseline; gap: 8px; }
    .buy-seats-count {
      font-family: 'Syne', sans-serif;
      font-size: 32px; font-weight: 800; color: #f1f5f9; line-height: 1;
    }
    .buy-seats-label { font-size: 13px; color: #6b7a99; }
    .buy-summary-right {
      display: flex; flex-direction: column; align-items: flex-end; gap: 10px;
    }
    .buy-total {
      font-family: 'Syne', sans-serif;
      font-size: 28px; font-weight: 800; color: #f1f5f9;
    }

    .buy-guest-form { display: flex; gap: 10px; flex-wrap: wrap; justify-content: flex-end; }
    .buy-field { width: 200px; }
    .buy-field ::ng-deep .mat-mdc-text-field-wrapper { background: rgba(255,255,255,0.04) !important; }
    .buy-field ::ng-deep .mdc-notched-outline__leading,
    .buy-field ::ng-deep .mdc-notched-outline__notch,
    .buy-field ::ng-deep .mdc-notched-outline__trailing { border-color: rgba(255,255,255,0.15) !important; }
    .buy-field ::ng-deep input { color: #e2e8f0 !important; }
    .buy-field ::ng-deep label { color: #6b7a99 !important; }

    .buy-error { font-size: 13px; color: #f87171; margin: 0; }

    .buy-cta {
      display: flex; align-items: center; gap: 8px;
      padding: 12px 28px;
      background: linear-gradient(135deg, #d4a853 0%, #c47c20 100%);
      color: #0a0e1a; border: none; border-radius: 10px;
      font-family: 'Syne', sans-serif; font-size: 15px; font-weight: 700;
      cursor: pointer;
      transition: transform 0.15s, box-shadow 0.15s, filter 0.15s;
      box-shadow: 0 4px 20px rgba(212,168,83,0.35);
      white-space: nowrap;
    }
    .buy-cta:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 8px 28px rgba(212,168,83,0.45);
      filter: brightness(1.08);
    }
    .buy-cta:disabled { opacity: 0.6; cursor: not-allowed; }

    /* Payment card */
    .pay-card {
      width: 100%;
      max-width: 520px;
      margin: 0 auto;
      zoom: 0.9;
      background: #0d1422;
      border-radius: 16px;
      border: 1px solid rgba(255,255,255,0.07);
      box-shadow: 0 32px 72px rgba(0,0,0,0.55);
      overflow: hidden;
      animation: card-in 0.4s cubic-bezier(0.22,1,0.36,1) both;
      padding-bottom: 28px;
    }
    @keyframes card-in {
      from { opacity: 0; transform: translateY(24px) scale(0.97); }
      to   { opacity: 1; transform: translateY(0) scale(1); }
    }
    .top-accent {
      height: 2px;
      background: linear-gradient(90deg, #6366f1 0%, #d4a853 55%, transparent 100%);
      margin-bottom: 28px;
    }
    .pay-header { padding: 0 24px 20px; }
    .pay-title {
      font-family: 'Syne', sans-serif;
      font-size: 22px; font-weight: 800; color: #f8fafc;
      margin: 0 0 12px; letter-spacing: -0.01em;
    }
    .pay-meta { display: flex; align-items: center; gap: 12px; }
    .meta-chip {
      background: rgba(99,102,241,0.12);
      border: 1px solid rgba(99,102,241,0.2);
      border-radius: 6px; padding: 4px 10px;
      font-size: 13px; color: #818cf8; font-weight: 600;
    }
    .meta-amount { font-size: 20px; font-weight: 700; color: #f8fafc; }
    .pay-breakdown { margin-top: 12px; display: flex; flex-direction: column; gap: 4px; }
    .pay-breakdown-row { font-size: 12px; color: #64748b; }
    .pay-breakdown-discount { color: #34d399; }

    /* Promo */
    .promo-section {
      padding: 0 24px 16px;
      display: flex; flex-direction: column; gap: 8px;
    }
    .promo-row, .loyalty-row { display: flex; gap: 8px; }
    .promo-input {
      flex: 1;
      background: rgba(255,255,255,0.04);
      border: 1px solid rgba(255,255,255,0.09);
      border-radius: 8px; padding: 10px 14px;
      color: #f1f5f9; font-size: 13px; font-family: 'DM Sans', sans-serif;
      outline: none;
      transition: border-color 0.18s;
    }
    .promo-input:focus { border-color: rgba(99,102,241,0.45); }
    .promo-input:disabled { opacity: 0.5; }
    .promo-input[type=number] { text-transform: none; letter-spacing: 0; }
    .promo-input::placeholder { color: #334155; }
    .promo-btn {
      padding: 10px 16px;
      background: rgba(99,102,241,0.12);
      border: 1px solid rgba(99,102,241,0.25);
      border-radius: 8px; color: #818cf8;
      font-size: 12px; font-weight: 700; font-family: 'DM Sans', sans-serif;
      letter-spacing: 0.06em; text-transform: uppercase;
      cursor: pointer; display: flex; align-items: center; gap: 6px;
      white-space: nowrap; min-width: 80px; justify-content: center;
      transition: background 0.18s, border-color 0.18s;
    }
    .promo-btn:hover:not(:disabled) {
      background: rgba(99,102,241,0.22);
      border-color: rgba(99,102,241,0.45);
    }
    .promo-btn:disabled { opacity: 0.4; cursor: not-allowed; }
    .promo-applied {
      display: flex; align-items: center; gap: 8px;
      padding: 10px 14px;
      background: rgba(52,211,153,0.07);
      border: 1px solid rgba(52,211,153,0.2);
      border-radius: 8px; font-size: 13px; color: #6ee7b7;
    }
    .promo-error { font-size: 12px; color: #fca5a5; padding: 0 4px; }

    /* Loyalty section */
    .loyalty-section {
      padding: 0 24px 16px;
      display: flex; flex-direction: column; gap: 8px;
    }
    .loyalty-balance {
      display: flex; align-items: center; gap: 6px;
      font-size: 13px; color: #94a3b8;
    }
    .loyalty-hint { font-size: 11px; color: #475569; padding: 0 2px; }
    .loyalty-cancel {
      margin-left: auto; background: none; border: none;
      color: #f87171; font-size: 12px; cursor: pointer;
      text-decoration: underline; padding: 0;
    }

    /* Tabs */
    .tab-switcher {
      display: flex; margin: 0 24px 24px;
      background: rgba(255,255,255,0.04);
      border-radius: 10px; padding: 4px; gap: 4px;
    }
    .tab-btn {
      flex: 1; display: flex; align-items: center; justify-content: center; gap: 8px;
      padding: 10px; border: none; border-radius: 7px;
      background: transparent; color: #4a6080;
      font-size: 13px; font-weight: 600; font-family: 'DM Sans', sans-serif;
      cursor: pointer; transition: background 0.18s, color 0.18s;
    }
    .tab-btn.active { background: #0d1422; color: #f1f5f9; box-shadow: 0 1px 4px rgba(0,0,0,0.4); }

    .tab-content {
      padding: 0 24px;
      display: flex; flex-direction: column; gap: 14px;
      animation: fade-in 0.2s ease both;
    }
    .gpay-container { min-height: 50px; border-radius: 10px; overflow: hidden; }
    .or-divider {
      display: flex; align-items: center; gap: 12px;
      color: #2a3547; font-size: 11px; font-weight: 600;
      letter-spacing: 0.08em; text-transform: uppercase;
    }
    .or-divider::before, .or-divider::after {
      content: ''; flex: 1; height: 1px;
      background: rgba(255,255,255,0.06);
    }
    .stripe-label {
      font-size: 11px; font-weight: 700; letter-spacing: 0.1em;
      text-transform: uppercase; color: #4a6080;
    }
    .stripe-element {
      background: rgba(255,255,255,0.035);
      border: 1px solid rgba(255,255,255,0.09);
      border-radius: 10px; padding: 14px 16px; min-height: 44px;
    }
    .paypal-info {
      display: flex; align-items: flex-start; gap: 10px;
      padding: 14px;
      background: rgba(255,255,255,0.03);
      border: 1px solid rgba(255,255,255,0.06);
      border-radius: 10px; font-size: 13px; color: #64748b; line-height: 1.5;
    }
    .usd-note {
      display: flex; align-items: baseline; gap: 8px;
      padding: 10px 14px;
      background: rgba(0,156,222,0.06);
      border: 1px solid rgba(0,156,222,0.15);
      border-radius: 8px; font-size: 15px; font-weight: 700; color: #38bdf8;
    }
    .usd-rate { font-size: 11px; font-weight: 400; color: #4a6080; }
    .pay-error {
      background: rgba(239,68,68,0.07);
      border: 1px solid rgba(239,68,68,0.18);
      border-radius: 8px; padding: 10px 14px;
      font-size: 13px; color: #fca5a5;
    }
    .pay-btn {
      width: 100%; padding: 14px;
      background: linear-gradient(135deg, #d4a853 0%, #b8871e 100%);
      border: none; border-radius: 10px;
      color: #07091a; font-size: 14px; font-weight: 700;
      font-family: 'Syne', sans-serif; letter-spacing: 0.08em; text-transform: uppercase;
      cursor: pointer; display: flex; align-items: center; justify-content: center;
      min-height: 50px;
      transition: opacity 0.18s, transform 0.15s, box-shadow 0.18s;
    }
    .pay-btn:hover:not(:disabled) {
      opacity: 0.88; transform: translateY(-1px);
      box-shadow: 0 10px 28px rgba(212,168,83,0.28);
    }
    .pay-btn:disabled { opacity: 0.38; cursor: not-allowed; }
    .paypal-btn { background: linear-gradient(135deg, #009cde 0%, #003087 100%); color: #fff; }
    .paypal-btn:hover:not(:disabled) { box-shadow: 0 10px 28px rgba(0,156,222,0.25); }
    .btn-spinner {
      width: 20px; height: 20px;
      border: 2px solid rgba(7,9,26,0.2);
      border-top-color: #07091a;
      border-radius: 50%; animation: spin 0.7s linear infinite;
    }
    .btn-spinner--dark { border-color: rgba(255,255,255,0.2); border-top-color: #fff; }
    .btn-spinner--sm { width: 14px; height: 14px; border: 2px solid rgba(129,140,248,0.2); border-top-color: #818cf8; border-radius: 50%; animation: spin 0.7s linear infinite; }

    .back-link {
      display: block; text-align: center; margin: 16px 24px 0;
      font-size: 13px; color: #4f5fa8; text-decoration: none;
      background: none; border: none; cursor: pointer; font-family: 'DM Sans', sans-serif;
      transition: color 0.18s;
    }
    .back-link:hover { color: #818cf8; }

    .buy-back {
      display: inline-flex; align-items: center; gap: 6px;
      font-size: 13px; color: #4b5e80; text-decoration: none;
      transition: color 0.15s; margin-top: -8px;
    }
    .buy-back:hover { color: #94a3b8; }
    .buy-empty { text-align: center; padding: 80px 0; color: #4b5e80; display: flex; flex-direction: column; gap: 12px; }
    .buy-empty a { color: #d4a853; text-decoration: none; }

    @keyframes fade-in {
      from { opacity: 0; transform: translateY(12px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    @media (max-width: 600px) {
      .buy-summary { flex-direction: column; }
      .buy-summary-right { align-items: stretch; }
      .buy-guest-form { flex-direction: column; }
      .buy-field { width: 100%; }
      .buy-cta { justify-content: center; }
    }
  `],
})
export class BuyConfirmComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('stripeCard')    stripeCardRef?: ElementRef<HTMLDivElement>;
  @ViewChild('gpayContainer') gpayContainerRef?: ElementRef<HTMLDivElement>;

  private readonly route       = inject(ActivatedRoute);
  private readonly router      = inject(Router);
  private readonly ticketSvc   = inject(TicketService);
  private readonly paySvc      = inject(PaymentService);
  private readonly accountSvc  = inject(AccountService);
  readonly auth                = inject(AuthService);
  private readonly fb          = inject(FormBuilder);

  // Seat step
  readonly seatMap       = signal<SeatMap | null>(null);
  readonly loading       = signal(true);
  readonly selectedSeats = signal<SeatCoord[]>([]);
  readonly seatError     = signal<string | null>(null);
  readonly submittingTickets = signal(false);
  readonly step          = signal<Step>('seats');
  // Guest form
  readonly guestForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  // After tickets created
  readonly ticketsResp = signal<CreateTicketsResponse | null>(null);

  // Promo
  promoCode = '';
  readonly promoApplied  = signal(false);
  readonly promoLoading  = signal(false);
  readonly promoError    = signal<string | null>(null);
  readonly promoDiscount = signal(0);

  // Loyalty
  readonly loyalty            = signal<LoyaltyBalance | null>(null);
  loyaltyPointsInput          = 0;
  readonly loyaltyApplied     = signal(false);
  readonly loyaltyPointsApplied = signal(0);
  readonly loyaltyDiscount    = signal(0);

  // Payment
  readonly activeTab         = signal<PaymentTab>('stripe');
  readonly processing        = signal(false);
  readonly payError          = signal<string | null>(null);
  readonly googlePayAvailable = signal(false);
  readonly uahToUsdRate      = signal<number>(0.0225);
  readonly isCashier         = computed(() => this.auth.hasRole('Cashier'));

  private stripe: Stripe | null = null;
  private cardEl: StripeCardElement | null = null;
  private googlePayClient: google.payments.api.PaymentsClient | null = null;

  readonly baseTotal = computed(() => {
    const map = this.seatMap();
    if (!map) return 0;
    return this.selectedSeats().reduce((sum, s) => {
      const type  = map.layout[s.row - 1][s.col - 1].toLowerCase();
      const coeff = type === 'vip' ? 1.5 : type === 'love' ? 2.0 : 1.0;
      return sum + map.basePrice * coeff;
    }, 0);
  });

  readonly finalTotal = computed(() => {
    const resp = this.ticketsResp();
    if (!resp) return this.baseTotal();
    return resp.totalAmount;
  });

  readonly maxRedeemable = computed(() => {
    const bal = this.loyalty()?.balance ?? 0;
    const max = Math.floor(this.finalTotal() * 0.5);
    return Math.min(bal, max);
  });

  readonly usdAmount = computed(() =>
    Math.round(this.finalTotal() * this.uahToUsdRate() * 100) / 100
  );

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('showtimeId'));
    this.ticketSvc.getSeatMap(id).subscribe({
      next:  map => { this.seatMap.set(map); this.loading.set(false); },
      error: ()  => this.loading.set(false),
    });
    if (this.auth.isLoggedIn()) {
      this.accountSvc.getLoyaltyBalance().subscribe({
        next: bal => this.loyalty.set(bal),
      });
    }
    this.paySvc.getExchangeRate().subscribe({ next: r => this.uahToUsdRate.set(r) });
  }

  async ngAfterViewInit(): Promise<void> {
    this.stripe = await loadStripe(environment.stripePublicKey);
  }

  ngOnDestroy(): void {
    this.cardEl?.unmount();
  }

  onSeatsSelected(seats: SeatCoord[]): void {
    this.selectedSeats.set(seats);
    this.seatError.set(null);
  }

  proceedToPayment(): void {
    if (this.isCashier()) {
      this.seatError.set('buy.cashierPurchaseNotAllowed');
      return;
    }
    if (!this.auth.isLoggedIn() && this.guestForm.invalid) {
      this.guestForm.markAllAsTouched();
      return;
    }
    this.submittingTickets.set(true);
    this.seatError.set(null);

    const g = this.guestForm.getRawValue();
    this.ticketSvc.createTickets({
      showtimeId: this.seatMap()!.showtimeId,
      seats: this.selectedSeats(),
      guestEmail: this.auth.isLoggedIn() ? undefined : g.email,
    }).subscribe({
      next: resp => {
        this.submittingTickets.set(false);
        this.ticketsResp.set(resp);
        this.step.set('payment');
        // Mount Stripe after step change
        setTimeout(() => this.mountStripeCard(), 100);
        this.initGooglePay();
      },
      error: err => {
        this.submittingTickets.set(false);
        const body = err?.error;
        const msg  = body?.error ?? 'booking.error';
        if (err?.status === 409) this.refreshSeatMap();
        this.seatError.set(msg.includes('already taken')
          ? 'buy.seatsUnavailable'
          : msg.includes('Cashier accounts cannot buy tickets online')
            ? 'buy.cashierPurchaseNotAllowed'
            : msg);
      },
    });
  }

  private refreshSeatMap(clearSelected = false): void {
    const showtimeId = this.seatMap()?.showtimeId;
    if (!showtimeId) return;
    this.ticketSvc.getSeatMap(showtimeId).subscribe({
      next: map => {
        this.seatMap.set(map);
        if (clearSelected) {
          this.selectedSeats.set([]);
        }
      },
    });
  }

  backToSeats(): void {
    this.step.set('seats');
    this.ticketsResp.set(null);
    this.promoApplied.set(false);
    this.promoDiscount.set(0);
    this.loyaltyApplied.set(false);
    this.loyaltyDiscount.set(0);
    this.loyaltyPointsApplied.set(0);
    this.payError.set(null);
  }

  // ── Promo ─────────────────────────────────────────────────────────────────

  applyPromo(): void {
    // Promo is applied by re-creating tickets with promoCode
    const code = this.promoCode.trim();
    if (!code || !this.seatMap()) return;
    this.promoLoading.set(true);
    this.promoError.set(null);

    const g = this.guestForm.getRawValue();
    this.ticketSvc.createTickets({
      showtimeId: this.seatMap()!.showtimeId,
      seats: this.selectedSeats(),
      guestEmail: this.auth.isLoggedIn() ? undefined : g.email,
      promoCode: code,
      loyaltyPointsToRedeem: this.loyaltyPointsApplied() || undefined,
    }).subscribe({
      next: resp => {
        const discount = this.baseTotal() - this.loyaltyDiscount() - resp.totalAmount;
        this.ticketsResp.set(resp);
        this.promoDiscount.set(discount > 0 ? discount : 0);
        this.promoApplied.set(discount > 0);
        if (discount <= 0) this.promoError.set('payment.promoError');
        this.promoLoading.set(false);
      },
      error: () => { this.promoError.set('payment.promoError'); this.promoLoading.set(false); },
    });
  }

  // ── Loyalty ───────────────────────────────────────────────────────────────

  applyLoyalty(): void {
    const pts = Math.min(this.loyaltyPointsInput, this.maxRedeemable());
    if (pts < 1) return;

    const g = this.guestForm.getRawValue();
    this.ticketSvc.createTickets({
      showtimeId: this.seatMap()!.showtimeId,
      seats: this.selectedSeats(),
      guestEmail: this.auth.isLoggedIn() ? undefined : g.email,
      promoCode: this.promoApplied() ? this.promoCode.trim() : undefined,
      loyaltyPointsToRedeem: pts,
    }).subscribe({
      next: resp => {
        const discount = this.baseTotal() - this.promoDiscount() - resp.totalAmount;
        this.ticketsResp.set(resp);
        this.loyaltyDiscount.set(discount > 0 ? discount : pts);
        this.loyaltyPointsApplied.set(pts);
        this.loyaltyApplied.set(true);
      },
    });
  }

  cancelLoyalty(): void {
    const g = this.guestForm.getRawValue();
    this.ticketSvc.createTickets({
      showtimeId: this.seatMap()!.showtimeId,
      seats: this.selectedSeats(),
      guestEmail: this.auth.isLoggedIn() ? undefined : g.email,
      promoCode: this.promoApplied() ? this.promoCode.trim() : undefined,
    }).subscribe({
      next: resp => {
        this.ticketsResp.set(resp);
        this.loyaltyApplied.set(false);
        this.loyaltyDiscount.set(0);
        this.loyaltyPointsApplied.set(0);
        this.loyaltyPointsInput = 0;
      },
    });
  }

  // ── Payment ───────────────────────────────────────────────────────────────

  setTab(tab: PaymentTab): void {
    this.activeTab.set(tab);
    this.payError.set(null);
    if (tab === 'stripe') setTimeout(() => this.mountStripeCard(), 0);
  }

  private mountStripeCard(): void {
    if (!this.stripe || !this.stripeCardRef?.nativeElement) return;
    this.cardEl?.unmount();
    const elements = this.stripe.elements();
    this.cardEl = elements.create('card', {
      hidePostalCode: true,
      style: {
        base: {
          color: '#f1f5f9',
          fontFamily: '"DM Sans", sans-serif',
          fontSize: '15px',
          '::placeholder': { color: '#334155' },
          iconColor: '#64748b',
        },
        invalid: { color: '#f87171' },
      },
    });
    this.cardEl.mount(this.stripeCardRef.nativeElement);
  }

  private navigateAfterSuccessfulPayment(ticketIds: string): void {
    this.router.navigate(['/buy/success'], {
      queryParams: { ticketIds, movie: this.seatMap()!.movieTitle },
    });
  }

  async payWithStripe(): Promise<void> {
    if (!this.stripe || !this.cardEl || !this.ticketsResp()) return;
    this.processing.set(true);
    this.payError.set(null);
    const paymentId = this.ticketsResp()!.paymentId;
    const returnUrl = `${window.location.origin}/buy/paypal-return?paymentId=${paymentId}`;

    this.paySvc.createIntent(paymentId, 'stripe', { returnUrl }).subscribe({
      next: async resp => {
        if (!resp.clientSecret) {
          this.payError.set('payment.errorNoSecret');
          this.processing.set(false);
          return;
        }
        const { error, paymentIntent } = await this.stripe!.confirmCardPayment(
          resp.clientSecret, { payment_method: { card: this.cardEl! } }
        );
        this.processing.set(false);
        if (error) {
          this.payError.set(error.message ?? 'payment.error');
        } else if (paymentIntent?.status === 'succeeded') {
          const ids = this.ticketsResp()!.tickets.map(t => t.id).join(',');
          this.paySvc.confirmStripeClient(paymentId, paymentIntent.id).subscribe({
            next: () => this.navigateAfterSuccessfulPayment(ids),
            error: () => this.payError.set('payment.error'),
          });
        }
      },
      error: err => {
        this.processing.set(false);
        const msg = err?.error?.error ?? '';
        this.payError.set(msg.toLowerCase().includes('not found') ? 'payment.errorNotFound' : 'payment.error');
      },
    });
  }

  payWithPayPal(): void {
    if (!this.ticketsResp()) return;
    this.processing.set(true);
    this.payError.set(null);
    const paymentId = this.ticketsResp()!.paymentId;
    const returnUrl = `${window.location.origin}/buy/paypal-return?paymentId=${paymentId}`;

    this.paySvc.createIntent(paymentId, 'paypal', { returnUrl }).subscribe({
      next: resp => {
        if (resp.approvalUrl) {
          window.location.href = resp.approvalUrl;
        } else {
          this.processing.set(false);
          this.payError.set('payment.error');
        }
      },
      error: () => { this.processing.set(false); this.payError.set('payment.error'); },
    });
  }

  // ── Google Pay ─────────────────────────────────────────────────────────────

  private initGooglePay(): void {
    if (!(window as unknown as { google?: unknown }).google) return;
    this.onGooglePayLoaded();
  }

  private async onGooglePayLoaded(): Promise<void> {
    this.googlePayClient = new google.payments.api.PaymentsClient({ environment: 'TEST' });
    const { result } = await this.googlePayClient.isReadyToPay({
      apiVersion: 2, apiVersionMinor: 0,
      allowedPaymentMethods: [this.buildCardPaymentMethod()],
    });
    if (!result) return;
    this.googlePayAvailable.set(true);
    setTimeout(() => this.renderGooglePayButton(), 0);
  }

  private buildCardPaymentMethod(): google.payments.api.PaymentMethodSpecification {
    return {
      type: 'CARD',
      parameters: {
        allowedAuthMethods: ['PAN_ONLY', 'CRYPTOGRAM_3DS'],
        allowedCardNetworks: ['AMEX', 'DISCOVER', 'MASTERCARD', 'VISA'],
      },
      tokenizationSpecification: {
        type: 'PAYMENT_GATEWAY',
        parameters: {
          gateway: 'stripe',
          'stripe:version': '2024-06-20',
          'stripe:publishableKey': environment.stripePublicKey,
        },
      },
    };
  }

  private renderGooglePayButton(): void {
    const container = this.gpayContainerRef?.nativeElement;
    if (!this.googlePayClient || !container) return;
    container.innerHTML = '';
    const button = this.googlePayClient.createButton({
      onClick: () => this.onGooglePayClick(),
      buttonType: 'buy', buttonColor: 'black', buttonSizeMode: 'fill',
    });
    container.appendChild(button);
  }

  private async onGooglePayClick(): Promise<void> {
    if (!this.googlePayClient || !this.ticketsResp()) return;
    const paymentDataRequest: google.payments.api.PaymentDataRequest = {
      apiVersion: 2, apiVersionMinor: 0,
      allowedPaymentMethods: [this.buildCardPaymentMethod()],
      merchantInfo: { merchantId: environment.googlePayMerchantId, merchantName: 'Cinema Network' },
      transactionInfo: {
        totalPriceStatus: 'FINAL',
        totalPrice: this.finalTotal().toFixed(2),
        currencyCode: 'UAH', countryCode: 'UA',
      },
    };
    try {
      const paymentData = await this.googlePayClient.loadPaymentData(paymentDataRequest);
      const tokenStr = paymentData.paymentMethodData.tokenizationData.token;
      let tokenId: string;
      try { tokenId = (JSON.parse(tokenStr) as { id: string }).id; }
      catch { tokenId = tokenStr; }
      this.processing.set(true);
      this.paySvc.confirmGooglePay(this.ticketsResp()!.paymentId, tokenId).subscribe({
        next: () => {
          this.processing.set(false);
          const ids = this.ticketsResp()!.tickets.map(t => t.id).join(',');
          this.navigateAfterSuccessfulPayment(ids);
        },
        error: () => { this.processing.set(false); this.payError.set('payment.error'); },
      });
    } catch (err: unknown) {
      if ((err as { statusCode?: string })?.statusCode !== 'CANCELED') {
        this.payError.set('payment.error');
      }
    }
  }
}

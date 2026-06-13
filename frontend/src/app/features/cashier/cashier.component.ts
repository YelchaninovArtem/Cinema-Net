import { Component, ElementRef, ViewChild, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { lastValueFrom } from 'rxjs';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { CashierService, VerifyTicketResult, OfflineSaleResult, RefundResult } from '../../core/services/cashier.service';
import { SeatTypeCode } from '../../core/services/admin.service';
import { TicketService } from '../../core/services/ticket.service';
import { SeatMap } from '../../core/models/ticket.models';
import { MovieService } from '../../core/services/movie.service';
import { Showtime } from '../../core/models/catalog.models';
import { LocalizedDatePipe } from '../../shared/localized-date.pipe';

@Component({
  selector: 'app-cashier',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TranslateModule, LocalizedDatePipe,
    MatTabsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatCardModule, MatSnackBarModule, MatProgressSpinnerModule,
    MatChipsModule, ZXingScannerModule,
  ],
  template: `
<div class="cashier-page">
  <div class="cashier-container">
    <header class="cashier-header">
      <mat-icon class="cashier-logo">point_of_sale</mat-icon>
      <h1 class="cashier-title">{{ 'cashier.title' | translate }}</h1>
    </header>

    <mat-tab-group class="cashier-tabs" [(selectedIndex)]="selectedTabIndex">

      <!-- ══ TAB 1: QR SCANNER ══ -->
      <mat-tab [label]="'cashier.tabs.scanner' | translate">
        <div class="tab-content">
          <h2>{{ 'cashier.scanner.heading' | translate }}</h2>
          <p class="scanner-rule"><mat-icon>schedule</mat-icon><span>{{ 'cashier.scanner.checkInRule' | translate }}</span></p>

          @if (!scannerEnabled()) {
            <button mat-flat-button color="primary" (click)="startScanner()">
              <mat-icon>qr_code_scanner</mat-icon>
              {{ 'cashier.tabs.scanner' | translate }}
            </button>
          }

          @if (scannerEnabled()) {
            <div class="scanner-wrapper">
              <zxing-scanner
                [enable]="scannerEnabled()"
                (scanSuccess)="onQrScanned($event)"
                (camerasFound)="onCamerasFound($event)"
                [device]="selectedCamera() ?? undefined">
              </zxing-scanner>
              <p class="scanner-hint">{{ 'cashier.scanner.hint' | translate }}</p>
              @if (cameras().length > 1) {
                <mat-form-field appearance="outline" class="camera-select">
                  <mat-label>{{ 'cashier.scanner.camera' | translate }}</mat-label>
                  <mat-select [(ngModel)]="selectedCameraDevice" (ngModelChange)="onCameraChange($event)">
                    @for (cam of cameras(); track cam.deviceId) {
                      <mat-option [value]="cam">{{ cam.label || cam.deviceId }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              }
            </div>
          }

          @if (scannedTicket()) {
            <mat-card class="ticket-card"
              [class.status-paid]="scannedTicket()!.status === 'Paid'"
              [class.status-used]="scannedTicket()!.status === 'Used'">
              <mat-card-header>
                <mat-icon mat-card-avatar
                  [class.icon-ok]="scannedTicket()!.status === 'Paid'"
                  [class.icon-warn]="scannedTicket()!.status !== 'Paid'">
                  {{ scannedTicket()!.status === 'Paid' ? 'check_circle' : 'warning' }}
                </mat-icon>
                <mat-card-title>{{ scannedTicket()!.movieTitle }}</mat-card-title>
                <mat-card-subtitle>{{ scannedTicket()!.cinemaBranchName }} - {{ scannedTicket()!.hallName }}</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <div class="ticket-details">
                  <div class="detail-row">
                    <span class="label">{{ 'cashier.refunds.showtime' | translate }}</span>
                    <span>{{ scannedTicket()!.showtimeUtc | localizedDate:'dd.MM.yyyy HH:mm' }}</span>
                  </div>
                  <div class="detail-row">
                    <span class="label">{{ 'cashier.refunds.seat' | translate }}</span>
                    <span>{{ 'cashier.ticket.row' | translate }} {{ scannedTicket()!.row }}, {{ 'cashier.ticket.col' | translate }} {{ scannedTicket()!.col }}</span>
                  </div>
                  <div class="detail-row">
                    <span class="label">{{ 'cashier.refunds.status' | translate }}</span>
                    <mat-chip [class.chip-paid]="scannedTicket()!.status === 'Paid'" [class.chip-used]="scannedTicket()!.status === 'Used'">
                      {{ 'account.status.' + scannedTicket()!.status | translate }}
                    </mat-chip>
                  </div>
                  <div class="detail-row">
                    <span class="label">{{ 'cashier.refunds.amount' | translate }}</span>
                    <span class="amount">{{ scannedTicket()!.finalAmount | number:'1.2-2' }} UAH</span>
                  </div>
                </div>
              </mat-card-content>
              <mat-card-actions>
                @if (scannedTicket()!.status === 'Paid') {
                  <button mat-flat-button color="primary" class="action-button loading-button" (click)="markUsed()" [disabled]="scanning() || !canMarkUsed(scannedTicket()!)">
                    @if (scanning()) { <mat-spinner diameter="18"></mat-spinner> }
                    <span [class.is-hidden]="scanning()">{{ 'cashier.scanner.markUsed' | translate }}</span>
                  </button>
                }
                <button mat-button (click)="resetScanner()">{{ 'cashier.scanner.scanAnother' | translate }}</button>
              </mat-card-actions>
              @if (scannedTicket()!.status === 'Paid' && !canMarkUsed(scannedTicket()!)) {
                <p class="checkin-warning">{{ 'cashier.scanner.tooEarly' | translate }}</p>
              }
            </mat-card>
          }

          @if (scanError()) {
            <div class="scan-error"><mat-icon>error_outline</mat-icon><span>{{ scanError() }}</span></div>
          }
        </div>
      </mat-tab>

      <!-- ══ TAB 2: OFFLINE SALE ══ -->
      <mat-tab [label]="'cashier.tabs.offlineSale' | translate">
        <div class="tab-content">
          <h2>{{ 'cashier.offlineSale.heading' | translate }}</h2>
          <div class="sale-form">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'cashier.offlineSale.selectShowtime' | translate }}</mat-label>
              <mat-select [(ngModel)]="selectedShowtimeId" (ngModelChange)="onShowtimeSelected($event)">
                @for (st of showtimes(); track st.id) {
                  <mat-option [value]="st.id">{{ st.movieTitle }} - {{ st.startUtc | localizedDate:'dd.MM HH:mm' }} ({{ st.hallName }})</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>{{ 'cashier.offlineSale.guestEmail' | translate }}</mat-label>
              <input matInput type="email" [(ngModel)]="saleGuestEmail" required />
            </mat-form-field>
          </div>

          @if (seatMap()) {
            <section #offlineSeatStage class="offline-seat-stage">
              <div class="seat-stage-header">
                <p class="seat-hint">{{ 'cashier.offlineSale.selectSeats' | translate }}</p>
                <div class="seat-legend">
                  <span class="legend-item"><span class="legend-seat type-1"></span>{{ 'booking.legend.available' | translate }}</span>
                  <span class="legend-item"><span class="legend-seat type-2"></span>{{ 'booking.legend.vip' | translate }}</span>
                  <span class="legend-item"><span class="legend-seat type-3"></span>{{ 'booking.legend.love' | translate }}</span>
                  <span class="legend-item"><span class="legend-seat selected"></span>{{ 'booking.legend.selected' | translate }}</span>
                  <span class="legend-item"><span class="legend-seat taken"></span>{{ 'booking.legend.taken' | translate }}</span>
                </div>
              </div>
              @if (seatMap()!.format === '3D') {
                <p class="three-d-note">{{ 'booking.threeDGlassesNote' | translate }}</p>
              }
              <div class="seat-grid-wrapper">
                <div class="screen-bar"><span>{{ 'booking.screen' | translate }}</span></div>
                <div class="seat-grid" [style.grid-template-columns]="'24px repeat(' + seatMap()!.cols + ', 36px)'">
                  @for (row of getSeatRows(); track $index) {
                    <span class="row-label">{{ $index + 1 }}</span>
                    @for (seat of row; track seat.col) {
                      <button class="seat-btn"
                        [class.type-1]="seat.type === 1" [class.type-2]="seat.type === 2" [class.type-3]="seat.type === 3"
                        [class.selected]="isSeatSelected(seat.row, seat.col)"
                        [class.taken]="isSeatTaken(seat.row, seat.col)"
                        [disabled]="isSeatTaken(seat.row, seat.col)"
                        [title]="seatLabel(seat.type) + ' · ' + ('cashier.ticket.row' | translate) + ' ' + seat.row + ', ' + ('cashier.ticket.col' | translate) + ' ' + seat.col + ' · ' + seatPrice(seat.type).toFixed(2) + ' UAH'"
                        (click)="toggleSeat(seat.row, seat.col)">
                      </button>
                    }
                  }
                </div>
              </div>
            </section>
            <div class="sale-summary">
              <div class="sale-summary-info">
                <span class="sale-summary-count">{{ 'cashier.offlineSale.selectedCount' | translate : { count: selectedSeats().length } }}</span>
                @if (selectedSeats().length > 0) {
                  <div class="selected-seat-list">
                    @for (seat of selectedSeatDetails(); track seat.row + '-' + seat.col) {
                      <span class="selected-seat-chip">
                        {{ 'cashier.ticket.row' | translate }} {{ seat.row }}, {{ 'cashier.ticket.col' | translate }} {{ seat.col }}
                        <strong>{{ seat.price | number:'1.2-2' }} UAH</strong>
                      </span>
                    }
                  </div>
                  <span class="sale-summary-total">{{ 'cashier.offlineSale.total' | translate }}: {{ selectedSeatsTotal() | number:'1.2-2' }} UAH</span>
                }
              </div>
              <button mat-flat-button color="accent" class="sale-button loading-button"
                [disabled]="selectedSeats().length === 0 || saleLoading()"
                (click)="completeSale()">
                @if (saleLoading()) { <mat-spinner diameter="18"></mat-spinner> }
                <span [class.is-hidden]="saleLoading()">{{ 'cashier.offlineSale.sell' | translate }}</span>
              </button>
            </div>
          }
          @if (!selectedShowtimeId && showtimes().length === 0) {
            <p class="no-data">{{ 'cashier.offlineSale.noShowtimes' | translate }}</p>
          }
        </div>
      </mat-tab>

      <!-- ══ TAB 3: REFUNDS ══ -->
      <mat-tab [label]="'cashier.tabs.refunds' | translate">
        <div class="tab-content">
          <h2>{{ 'cashier.refunds.heading' | translate }}</h2>
          <div class="refund-search">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'cashier.refunds.ticketId' | translate }}</mat-label>
              <input matInput type="number" [(ngModel)]="refundTicketId" (keydown.enter)="findTicket()" />
            </mat-form-field>
            <button mat-flat-button color="primary" (click)="findTicket()" [disabled]="!refundTicketId || refundLoading()">
              {{ 'cashier.refunds.search' | translate }}
            </button>
          </div>

          @if (refundTicket()) {
            <mat-card class="ticket-card refund-ticket-card" [class.status-refunded]="refundResult()">
              @if (refundResult()) {
                <div class="refund-success-banner">
                  <span class="refund-success-icon"><mat-icon>check</mat-icon></span>
                  <div>
                    <strong>{{ 'cashier.refunds.completedTitle' | translate }}</strong>
                    <span>{{ 'cashier.refunds.completedMessage' | translate : { id: refundResult()!.ticketId, amount: (refundResult()!.refundedAmount | number:'1.2-2') } }}</span>
                  </div>
                </div>
              }
              <mat-card-header>
                <mat-card-title>{{ refundTicket()!.movieTitle }}</mat-card-title>
                <mat-card-subtitle>{{ refundTicket()!.cinemaBranchName }} - {{ refundTicket()!.hallName }}</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <div class="ticket-details">
                  <div class="detail-row"><span class="label">{{ 'cashier.refunds.showtime' | translate }}</span><span>{{ refundTicket()!.showtimeUtc | localizedDate:'dd.MM.yyyy HH:mm' }}</span></div>
                  <div class="detail-row"><span class="label">{{ 'cashier.refunds.seat' | translate }}</span><span>{{ 'cashier.ticket.row' | translate }} {{ refundTicket()!.row }}, {{ 'cashier.ticket.col' | translate }} {{ refundTicket()!.col }}</span></div>
                  <div class="detail-row">
                    <span class="label">{{ 'cashier.refunds.status' | translate }}</span>
                    <mat-chip [class.chip-refunded]="refundTicket()!.status === 'Refunded'">
                      @if (refundTicket()!.status === 'Refunded') {
                        <mat-icon>check_circle</mat-icon>
                      }
                      {{ 'account.status.' + refundTicket()!.status | translate }}
                    </mat-chip>
                  </div>
                  <div class="detail-row"><span class="label">{{ 'cashier.refunds.amount' | translate }}</span><span class="amount">{{ refundTicket()!.finalAmount | number:'1.2-2' }} UAH</span></div>
                </div>
              </mat-card-content>
              @if (!refundResult()) {
                <mat-card-actions>
                  @if (canRefund(refundTicket()!)) {
                  <button mat-flat-button color="warn" class="action-button loading-button" (click)="doRefund()" [disabled]="refundLoading()">
                    @if (refundLoading()) { <mat-spinner diameter="18"></mat-spinner> }
                    <span [class.is-hidden]="refundLoading()">{{ 'cashier.refunds.refund' | translate }}</span>
                  </button>
                  } @else {
                    <p class="cannot-refund">{{ 'cashier.refunds.cannotRefund' | translate : { status: refundTicket()!.status } }}</p>
                  }
                </mat-card-actions>
              }
            </mat-card>
          }

          @if (refundError()) {
            <div class="scan-error"><mat-icon>error_outline</mat-icon><span>{{ refundError() }}</span></div>
          }
        </div>
      </mat-tab>
    </mat-tab-group>
  </div>
</div>
  `,
  styles: [`
    .cashier-page { min-height: var(--app-content-height); box-sizing: border-box; background: linear-gradient(135deg, #0f172a 0%, #1e1b4b 50%, #0f172a 100%); padding: 24px 16px; }
    .cashier-container { max-width: 900px; margin: 0 auto; }
    .cashier-header { display: flex; align-items: center; gap: 12px; margin-bottom: 28px; }
    .cashier-logo { font-size: 36px; width: 36px; height: 36px; color: #a78bfa; }
    .cashier-title { font-size: 1.8rem; font-weight: 700; color: #f1f5f9; margin: 0; }
    .cashier-tabs { background: rgba(255,255,255,0.04); border-radius: 16px; overflow: hidden; }
    ::ng-deep .cashier-tabs .mat-mdc-tab-header { background: rgba(255,255,255,0.06); min-height: 60px; }
    ::ng-deep .cashier-tabs .mat-mdc-tab { height: 60px; }
    ::ng-deep .cashier-tabs .mat-mdc-tab .mdc-tab__content { height: 60px; }
    ::ng-deep .cashier-tabs .mat-mdc-tab .mdc-tab__text-label { color: rgba(255,255,255,0.6); font-size: 1.25rem; }
    ::ng-deep .cashier-tabs .mat-mdc-tab .mdc-tab-indicator__content--underline { border-top-width: 3px; }
    ::ng-deep .cashier-tabs .mat-mdc-tab.mdc-tab--active .mdc-tab__text-label { color: #a78bfa; }
    .tab-content { padding: 28px 24px; color: #e2e8f0; }
    .tab-content h2 { font-size: 1.25rem; font-weight: 600; margin: 0 0 20px; color: #c4b5fd; }
    .scanner-rule { display: flex; align-items: center; gap: 8px; max-width: 620px; margin: -8px 0 18px; padding: 10px 12px; border-radius: 8px; color: #fde68a; background: rgba(251,191,36,0.1); border: 1px solid rgba(251,191,36,0.22); font-size: 0.92rem; }
    .scanner-rule mat-icon { width: 20px; height: 20px; font-size: 20px; flex: 0 0 auto; }
    .scanner-wrapper { max-width: 480px; margin: 0 auto 20px; }
    .scanner-wrapper zxing-scanner { border-radius: 12px; overflow: hidden; border: 2px solid rgba(167,139,250,0.4); }
    .scanner-hint { text-align: center; color: rgba(255,255,255,0.5); font-size: 0.85rem; margin-top: 8px; }
    .camera-select { width: 100%; margin-top: 12px; }
    .ticket-card { max-width: 520px; margin: 20px auto 0; background: rgba(255,255,255,0.06) !important; border-radius: 12px !important; border: 1px solid rgba(255,255,255,0.1); }
    .ticket-card.status-paid { border-color: rgba(34,197,94,0.4); }
    .ticket-card.status-used { border-color: rgba(251,191,36,0.4); }
    .ticket-card.status-refunded { border-color: rgba(34,197,94,0.48); box-shadow: 0 12px 30px rgba(16,185,129,0.1); }
    ::ng-deep .ticket-card mat-card-title { color: #f1f5f9 !important; font-size: 1.1rem; }
    ::ng-deep .ticket-card mat-card-subtitle { color: rgba(255,255,255,0.5) !important; }
    .icon-ok { color: #22c55e !important; }
    .icon-warn { color: #fbbf24 !important; }
    .ticket-details { padding: 12px 0; }
    .detail-row { display: flex; justify-content: space-between; align-items: center; padding: 6px 0; border-bottom: 1px solid rgba(255,255,255,0.06); font-size: 0.9rem; }
    .detail-row:last-child { border-bottom: none; }
    .label { color: rgba(255,255,255,0.5); }
    .amount { font-weight: 600; color: #a78bfa; }
    .chip-paid { background: rgba(34,197,94,0.2) !important; color: #22c55e !important; }
    .chip-used { background: rgba(251,191,36,0.2) !important; color: #fbbf24 !important; }
    ::ng-deep .chip-refunded {
      --mdc-chip-label-text-color: #bbf7d0;
      --mdc-chip-with-icon-icon-color: #4ade80;
      background: rgba(34,197,94,0.2) !important;
      color: #bbf7d0 !important;
      border: 1px solid rgba(74,222,128,0.38);
    }
    ::ng-deep .chip-refunded .mdc-evolution-chip__text-label,
    ::ng-deep .chip-refunded .mat-mdc-chip-action-label { display: inline-flex; align-items: center; color: #bbf7d0 !important; font-weight: 700; line-height: 1; }
    ::ng-deep .chip-refunded .mat-icon { display: inline-flex; align-items: center; justify-content: center; width: 16px; height: 16px; margin: 0 5px 0 0; font-size: 16px; line-height: 16px; vertical-align: middle; color: #4ade80 !important; }
    .refund-success-banner { display: flex; align-items: center; gap: 12px; margin: 14px 14px 0; padding: 14px 16px; border: 1px solid rgba(74,222,128,0.28); border-radius: 10px; background: linear-gradient(135deg, rgba(34,197,94,0.16), rgba(16,185,129,0.08)); color: #dcfce7; }
    .refund-success-banner > div { display: grid; gap: 3px; min-width: 0; }
    .refund-success-banner strong { color: #86efac; font-size: 0.95rem; }
    .refund-success-banner span:not(.refund-success-icon) { color: rgba(220,252,231,0.78); font-size: 0.84rem; line-height: 1.4; }
    .refund-success-icon { display: inline-flex; align-items: center; justify-content: center; width: 34px; height: 34px; flex: 0 0 34px; border-radius: 50%; background: #22c55e; color: #052e16; }
    .refund-success-icon mat-icon { width: 20px; height: 20px; font-size: 20px; font-weight: 700; }
    .scan-error { display: flex; align-items: center; gap: 8px; color: #f87171; background: rgba(239,68,68,0.1); padding: 12px 16px; border-radius: 8px; margin-top: 16px; max-width: 520px; margin-inline: auto; }
    ::ng-deep .ticket-card .mat-mdc-card-actions { padding: 12px 16px 16px; gap: 8px; display: flex; }
    .sale-form { display: flex; flex-direction: column; gap: 4px; max-width: 480px; margin-bottom: 20px; }
    .sale-form mat-form-field { width: 100%; }
    .offline-seat-stage { animation: seat-stage-in 0.18s ease-out; }
    .seat-stage-header { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; margin-bottom: 12px; flex-wrap: wrap; }
    .seat-hint { color: rgba(255,255,255,0.68); font-size: 0.9rem; margin: 0; }
    .three-d-note { margin: 0 0 12px; padding: 10px 12px; border: 1px solid rgba(129,140,248,0.28); border-radius: 8px; background: rgba(99,102,241,0.1); color: #c7d2fe; font-size: 0.86rem; }
    .seat-legend { display: flex; align-items: center; justify-content: flex-end; flex-wrap: wrap; gap: 8px 14px; }
    .legend-item { display: inline-flex; align-items: center; gap: 7px; color: rgba(226,232,240,0.78); font-size: 0.78rem; font-weight: 600; white-space: nowrap; }
    .legend-seat { width: 18px; height: 15px; border-radius: 5px 5px 2px 2px; flex: 0 0 auto; }
    .legend-seat.type-1 { background: linear-gradient(170deg, #2ecc71 0%, #1a9e52 100%); box-shadow: 0 2px 0 #0f7a35; }
    .legend-seat.type-2 { background: linear-gradient(170deg, #fbbf24 0%, #d97706 100%); box-shadow: 0 2px 0 #92520a; }
    .legend-seat.type-3 { background: linear-gradient(170deg, #fb7185 0%, #e0185e 100%); box-shadow: 0 2px 0 #981248; }
    .legend-seat.selected { background: linear-gradient(170deg, #a5b4fc 0%, #6366f1 100%); box-shadow: 0 2px 0 #3730a3; }
    .legend-seat.taken { background: rgba(255,255,255,0.18); opacity: 0.42; filter: grayscale(1); }
    .seat-grid-wrapper { background: rgba(0,0,0,0.34); border: 1px solid rgba(255,255,255,0.05); border-radius: 12px; padding: 24px 28px 22px; overflow-x: auto; min-height: 260px; display: flex; flex-direction: column; justify-content: center; }
    .screen-bar { background: linear-gradient(90deg, transparent, rgba(167,139,250,0.72), transparent); height: 7px; border-radius: 3px; margin-bottom: 32px; position: relative; min-width: 560px; }
    .screen-bar span { position: absolute; left: 50%; transform: translateX(-50%); top: 14px; font-size: 0.7rem; color: rgba(255,255,255,0.38); letter-spacing: 0.18em; font-weight: 700; }
    .seat-grid { display: grid; gap: 8px; justify-content: center; width: max-content; min-width: min(100%, 560px); margin: 0 auto; }
    .row-label { display: flex; align-items: center; justify-content: center; font-size: 0.75rem; color: rgba(255,255,255,0.24); width: 24px; font-variant-numeric: tabular-nums; }
    .seat-btn { width: 36px; height: 31px; border: none; border-radius: 8px 8px 4px 4px; cursor: pointer; transition: transform 0.15s, filter 0.15s; padding: 0; min-width: 0; position: relative; }
    .seat-btn::after { content: ''; position: absolute; top: 4px; left: 20%; right: 20%; height: 3px; border-radius: 999px; background: rgba(255,255,255,0.23); }
    .seat-btn:not(.taken):hover { transform: translateY(-2px) scale(1.1); filter: brightness(1.3); }
    .seat-btn.type-1 { background: linear-gradient(170deg, #2ecc71 0%, #1a9e52 100%); box-shadow: 0 2px 0 #0f7a35; }
    .seat-btn.type-2 { background: linear-gradient(170deg, #fbbf24 0%, #d97706 100%); box-shadow: 0 2px 0 #92520a; }
    .seat-btn.type-3 { background: linear-gradient(170deg, #fb7185 0%, #e0185e 100%); box-shadow: 0 2px 0 #981248; }
    .seat-btn.selected { background: linear-gradient(170deg, #a5b4fc 0%, #6366f1 100%); box-shadow: 0 2px 0 #3730a3, 0 0 18px rgba(99,102,241,0.4); outline: 2px solid #fff; outline-offset: 2px; filter: brightness(1.12); }
    .seat-btn.taken { opacity: 0.22; cursor: not-allowed; filter: grayscale(1); box-shadow: none; }
    .sale-summary { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin-top: 16px; padding: 14px 16px; background: rgba(255,255,255,0.05); border-radius: 8px; min-height: 80px; }
    .sale-summary-info { min-width: 0; display: flex; flex-direction: column; gap: 8px; color: #f8fafc; }
    .sale-summary-count { font-weight: 700; }
    .sale-summary-total { color: #c4b5fd; font-weight: 800; }
    .selected-seat-list { display: flex; flex-wrap: wrap; gap: 6px; max-width: 620px; }
    .selected-seat-chip { display: inline-flex; align-items: center; gap: 8px; padding: 5px 9px; border-radius: 6px; background: rgba(255,255,255,0.08); color: rgba(248,250,252,0.9); font-size: 0.82rem; white-space: nowrap; }
    .selected-seat-chip strong { color: #fbbf24; font-weight: 800; }
    .sale-button { min-width: 198px; }
    .action-button { min-width: 164px; }
    .loading-button { position: relative; display: inline-grid !important; place-items: center; min-height: 40px; white-space: nowrap; }
    .loading-button mat-spinner { position: absolute; inset: 50% auto auto 50%; transform: translate(-50%, -50%); }
    .loading-button .is-hidden { opacity: 0; }
    ::ng-deep .loading-button .mdc-button__label { display: inline-grid; place-items: center; min-width: 0; }
    ::ng-deep .loading-button[disabled] { opacity: 0.82; }
    ::ng-deep .loading-button mat-spinner circle { stroke: #f8fafc !important; }
    .no-data { color: rgba(255,255,255,0.4); margin-top: 20px; }
    .refund-search { display: flex; gap: 12px; align-items: center; max-width: 600px; margin-bottom: 20px; }
    .refund-search mat-form-field { flex: 1; }
    .refund-search ::ng-deep .mat-mdc-form-field-subscript-wrapper { display: none; }
    .refund-search button { min-width: 170px; height: 52px; align-self: center; }
    .cannot-refund { color: #f87171; font-size: 0.85rem; margin: 0; padding: 8px 16px; }
    .checkin-warning { color: #fbbf24; font-size: 0.85rem; margin: 0; padding: 0 16px 16px; }
    ::ng-deep mat-form-field .mat-mdc-text-field-wrapper { background: rgba(255,255,255,0.06) !important; }
    ::ng-deep mat-form-field .mdc-floating-label,
    ::ng-deep mat-form-field input,
    ::ng-deep mat-form-field .mat-mdc-select-value,
    ::ng-deep mat-form-field .mat-mdc-select-value-text { color: #f8fafc !important; }
    ::ng-deep mat-form-field .mat-mdc-select-arrow { color: rgba(248,250,252,0.74) !important; }
    ::ng-deep mat-form-field .mdc-notched-outline__leading,
    ::ng-deep mat-form-field .mdc-notched-outline__notch,
    ::ng-deep mat-form-field .mdc-notched-outline__trailing { border-color: rgba(226,232,240,0.38) !important; }
    @media (max-width: 640px) {
      .seat-grid-wrapper { padding: 20px 16px; min-height: 220px; }
      .screen-bar { min-width: 420px; }
      .seat-stage-header { align-items: flex-start; }
      .seat-legend { justify-content: flex-start; }
      .sale-summary { align-items: stretch; flex-direction: column; }
      .selected-seat-list { max-width: 100%; }
      .selected-seat-chip { white-space: normal; }
      .sale-button { width: 100%; min-width: 0; }
    }
    @keyframes seat-stage-in {
      from { opacity: 0; transform: translateY(-6px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `],
})
export class CashierComponent implements OnInit, OnDestroy {
  private readonly cashier = inject(CashierService);
  private readonly ticketSvc = inject(TicketService);
  private readonly movieSvc = inject(MovieService);
  private readonly snack = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  selectedTabIndex = 0;
  @ViewChild('offlineSeatStage') private offlineSeatStage?: ElementRef<HTMLElement>;

  // Сканер
  scannerEnabled = signal(false);
  scanning = signal(false);
  scannedTicket = signal<VerifyTicketResult | null>(null);
  scanError = signal<string | null>(null);
  cameras = signal<MediaDeviceInfo[]>([]);
  selectedCamera = signal<MediaDeviceInfo | null>(null);
  selectedCameraDevice: MediaDeviceInfo | null = null;

  // Офлайн-продаж
  showtimes = signal<Showtime[]>([]);
  private allShowtimes: Showtime[] = [];
  private showtimeRefreshTimer: ReturnType<typeof setInterval> | null = null;
  selectedShowtimeId: number | null = null;
  seatMap = signal<SeatMap | null>(null);
  selectedSeats = signal<{ row: number; col: number }[]>([]);
  saleGuestEmail = '';
  saleLoading = signal(false);

  // Повернення
  refundTicketId: number | null = null;
  refundTicket = signal<VerifyTicketResult | null>(null);
  refundError = signal<string | null>(null);
  refundLoading = signal(false);
  refundResult = signal<RefundResult | null>(null);

  async ngOnInit(): Promise<void> {
    await this.loadShowtimes();
    this.showtimeRefreshTimer = setInterval(() => this.applySaleWindowFilter(), 30_000);
  }

  ngOnDestroy(): void {
    if (this.showtimeRefreshTimer) {
      clearInterval(this.showtimeRefreshTimer);
    }
  }

  startScanner(): void {
    this.scannedTicket.set(null);
    this.scanError.set(null);
    this.scannerEnabled.set(true);
  }

  onCamerasFound(cameras: MediaDeviceInfo[]): void {
    this.cameras.set(cameras);
    if (cameras.length > 0) {
      this.selectedCamera.set(cameras[0]);
      this.selectedCameraDevice = cameras[0];
    }
  }

  onCameraChange(device: MediaDeviceInfo): void {
    this.selectedCamera.set(device);
  }

  async onQrScanned(qrToken: string): Promise<void> {
    if (this.scanning()) return;
    this.scannerEnabled.set(false);
    this.scanning.set(true);
    this.scanError.set(null);
    try {
      const ticket = await lastValueFrom<VerifyTicketResult>(this.cashier.verifyByQr(qrToken));
      this.scannedTicket.set(ticket);
    } catch {
      const msg = await lastValueFrom<string>(this.translate.get('cashier.refunds.notFound'));
      this.scanError.set(msg);
    } finally {
      this.scanning.set(false);
    }
  }

  async markUsed(): Promise<void> {
    const ticket = this.scannedTicket();
    if (!ticket) return;
    if (!this.canMarkUsed(ticket)) {
      const msg = await lastValueFrom<string>(this.translate.get('cashier.scanner.tooEarly'));
      this.snack.open(msg, '', { duration: 4000, panelClass: 'snack-error' });
      return;
    }
    this.scanning.set(true);
    try {
      const updated = await lastValueFrom<VerifyTicketResult>(this.cashier.useTicket(ticket.ticketId));
      this.scannedTicket.set(updated);
      const msg = await lastValueFrom<string>(this.translate.get('cashier.scanner.success'));
      this.snack.open(msg, '', { duration: 3000, panelClass: 'snack-success' });
    } catch (err: any) {
      const msg = err?.error?.error ?? await lastValueFrom<string>(this.translate.get('common.error'));
      this.snack.open(msg, '', { duration: 4000, panelClass: 'snack-error' });
    } finally {
      this.scanning.set(false);
    }
  }

  resetScanner(): void {
    this.scannedTicket.set(null);
    this.scanError.set(null);
    this.scannerEnabled.set(false);
  }

  canMarkUsed(ticket: VerifyTicketResult): boolean {
    const checkInStart = new Date(ticket.showtimeUtc).getTime() - 20 * 60 * 1000;
    return Date.now() >= checkInStart;
  }

  canRefund(ticket: VerifyTicketResult): boolean {
    return ticket.status === 'Paid' && new Date(ticket.showtimeUtc).getTime() > Date.now();
  }

  private async loadShowtimes(): Promise<void> {
    try {
      const all = await lastValueFrom<Showtime[]>(this.movieSvc.getShowtimes());
      this.allShowtimes = all;
      this.applySaleWindowFilter();
    } catch {
      const msg = await lastValueFrom<string>(this.translate.get('admin.loadError'));
      this.snack.open(msg, '', { duration: 4000, panelClass: 'snack-error' });
    }
  }

  async onShowtimeSelected(showtimeId: number): Promise<void> {
    this.seatMap.set(null);
    this.selectedSeats.set([]);
    try {
      const map = await lastValueFrom<SeatMap>(this.ticketSvc.getSeatMap(showtimeId));
      this.seatMap.set(map);
      setTimeout(() => {
        this.offlineSeatStage?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 0);
    } catch { /* тихо ігноруємо помилку завантаження карти місць */ }
  }

  private async refreshSeatMap(showtimeId: number): Promise<void> {
    try {
      const map = await lastValueFrom<SeatMap>(this.ticketSvc.getSeatMap(showtimeId));
      this.seatMap.set(map);
    } catch { /* тихо ігноруємо помилку оновлення карти місць */ }
  }

  // layout може прийти як number[][] або string[][], тому нормалізуємо значення типів місць
  getSeatRows(): { row: number; col: number; type: SeatTypeCode }[][] {
    const map = this.seatMap();
    if (!map) return [];
    return map.layout.map((rowTypes, ri) =>
      rowTypes.map((type, ci) => ({ row: ri + 1, col: ci + 1, type: this.normalizeSeatType(type) }))
    );
  }

  seatLabel(type: SeatTypeCode): string {
    if (type === 2) return this.translate.instant('booking.legend.vip');
    if (type === 3) return this.translate.instant('booking.legend.love');
    return this.translate.instant('booking.legend.available');
  }

  private normalizeSeatType(type: unknown): SeatTypeCode {
    if (typeof type === 'number') return (type === 2 || type === 3 ? type : 1) as SeatTypeCode;
    const value = String(type).trim().toLowerCase();
    if (value === '2' || value === 'vip') return 2;
    if (value === '3' || value === 'love' || value === 'loveseat' || value === 'reclining') return 3;
    return 1;
  }

  seatPrice(type: SeatTypeCode): number {
    const basePrice = this.seatMap()?.basePrice ?? 0;
    const coefficient = type === 2 ? 1.5 : type === 3 ? 2 : 1;
    return basePrice * coefficient;
  }

  selectedSeatDetails(): { row: number; col: number; type: SeatTypeCode; price: number }[] {
    return this.selectedSeats().map(seat => {
      const type = this.getSeatTypeAt(seat.row, seat.col);
      return { ...seat, type, price: this.seatPrice(type) };
    });
  }

  selectedSeatsTotal(): number {
    return this.selectedSeatDetails().reduce((sum, seat) => sum + seat.price, 0);
  }

  private getSeatTypeAt(row: number, col: number): SeatTypeCode {
    const raw = this.seatMap()?.layout[row - 1]?.[col - 1];
    return this.normalizeSeatType(raw);
  }

  isSeatTaken(row: number, col: number): boolean {
    return this.seatMap()?.takenSeats.some(s => s.row === row && s.col === col) ?? false;
  }

  private applySaleWindowFilter(): void {
    const now = Date.now();
    const available = this.allShowtimes.filter((s: Showtime) =>
      new Date(s.startUtc).getTime() + 60_000 > now);

    this.showtimes.set(available);
    if (this.selectedShowtimeId && !available.some(s => s.id === this.selectedShowtimeId)) {
      this.selectedShowtimeId = null;
      this.seatMap.set(null);
      this.selectedSeats.set([]);
    }
  }

  isSeatSelected(row: number, col: number): boolean {
    return this.selectedSeats().some(s => s.row === row && s.col === col);
  }

  toggleSeat(row: number, col: number): void {
    if (this.isSeatTaken(row, col)) return;
    const cur = this.selectedSeats();
    const idx = cur.findIndex(s => s.row === row && s.col === col);
    this.selectedSeats.set(idx >= 0 ? cur.filter((_, i) => i !== idx) : [...cur, { row, col }]);
  }

  async completeSale(): Promise<void> {
    if (!this.selectedShowtimeId || this.selectedSeats().length === 0) return;
    if (!this.saleGuestEmail.trim()) {
      const msg = await lastValueFrom<string>(this.translate.get('cashier.offlineSale.emailRequired'));
      this.snack.open(msg, '', { duration: 4000, panelClass: 'snack-error' });
      return;
    }
    this.saleLoading.set(true);
    try {
      const result = await lastValueFrom<OfflineSaleResult>(this.cashier.createOfflineSale({
        showtimeId: this.selectedShowtimeId,
        seats: this.selectedSeats(),
        guestEmail: this.saleGuestEmail || null,
      }));
      const msg = await lastValueFrom<string>(this.translate.get('cashier.offlineSale.success', { amount: result.totalAmount.toFixed(2) }));
      this.snack.open(msg, '', {
        duration: 4000,
        panelClass: ['snack-success', 'cashier-sale-success-snack']
      });
      this.selectedSeats.set([]);
      await this.refreshSeatMap(this.selectedShowtimeId);
    } catch (err: any) {
      const key = err?.status === 409 ? 'cashier.offlineSale.conflict' : 'common.error';
      const msg = await lastValueFrom<string>(this.translate.get(key));
      this.snack.open(msg, '', { duration: 4000, panelClass: 'snack-error' });
    } finally {
      this.saleLoading.set(false);
    }
  }

  async findTicket(): Promise<void> {
    if (!this.refundTicketId) return;
    this.refundTicket.set(null);
    this.refundResult.set(null);
    this.refundError.set(null);
    this.refundLoading.set(true);
    try {
      const ticket = await lastValueFrom<VerifyTicketResult>(this.cashier.getTicketById(this.refundTicketId));
      this.refundTicket.set(ticket);
    } catch {
      const msg = await lastValueFrom<string>(this.translate.get('cashier.refunds.notFound'));
      this.refundError.set(msg);
    } finally {
      this.refundLoading.set(false);
    }
  }

  async doRefund(): Promise<void> {
    const ticket = this.refundTicket();
    if (!ticket) return;
    this.refundLoading.set(true);
    try {
      const result = await lastValueFrom<RefundResult>(this.cashier.refundTicket(ticket.ticketId));
      const msg = await lastValueFrom<string>(this.translate.get('cashier.refunds.success', { id: ticket.ticketId }));
      this.snack.open(msg, '', { duration: 4000, panelClass: 'snack-success' });
      this.refundResult.set(result);
      this.refundTicket.set({ ...ticket, status: result.status || 'Refunded' });
    } catch (err: any) {
      const msg = err?.error?.error ?? await lastValueFrom<string>(this.translate.get('common.error'));
      this.snack.open(msg, '', { duration: 4000, panelClass: 'snack-error' });
    } finally {
      this.refundLoading.set(false);
    }
  }
}

import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { PaymentService } from '../../../core/services/payment.service';

@Component({
  selector: 'app-buy-paypal-return',
  standalone: true,
  imports: [TranslateModule],
  template: `
    <div class="pp-page">
      @if (error()) {
        <div class="pp-error">
          <p>{{ error() | translate }}</p>
          <button class="pp-back" (click)="goBack()">{{ 'catalog.back' | translate }}</button>
        </div>
      } @else {
        <div class="pp-spinner"></div>
        <p class="pp-msg">{{ 'payment.processing' | translate }}</p>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .pp-page {
      min-height: var(--app-content-height);
      display: flex; flex-direction: column; align-items: center; justify-content: center;
      gap: 16px;
      background: #060a12;
      font-family: 'DM Sans', sans-serif;
    }
    .pp-spinner {
      width: 48px; height: 48px;
      border: 3px solid rgba(255,255,255,0.08);
      border-top-color: #d4a853;
      border-radius: 50%;
      animation: spin 0.9s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .pp-msg { color: #64748b; font-size: 14px; }
    .pp-error { display: flex; flex-direction: column; align-items: center; gap: 12px; color: #fca5a5; }
    .pp-back {
      background: none; border: 1px solid rgba(255,255,255,0.1);
      border-radius: 8px; padding: 8px 20px;
      color: #94a3b8; cursor: pointer; font-size: 13px;
    }
  `],
})
export class BuyPaypalReturnComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly paySvc = inject(PaymentService);

  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const params  = this.route.snapshot.queryParamMap;
    const orderId = params.get('token');
    const paymentId = Number(params.get('paymentId') ?? '0');

    if (params.get('cancelled') === 'true' || !orderId) {
      this.error.set('payment.captureFailed');
      return;
    }

    this.paySvc.capturePayPal(orderId).subscribe({
      next: () => {
        this.router.navigate(['/buy/success'], { queryParams: { paymentId } });
      },
      error: () => this.error.set('payment.captureFailed'),
    });
  }

  goBack(): void {
    this.router.navigate(['/catalog']);
  }
}

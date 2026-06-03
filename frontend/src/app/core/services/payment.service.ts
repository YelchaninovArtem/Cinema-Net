import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { CreateIntentRequest, CreateIntentResponse } from '../models/payment.models';

interface ExchangeRateResponse {
  base: string;
  target: string;
  rate: number;
  fetchedAt: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  createIntent(paymentId: number, provider: 'stripe' | 'paypal', req: CreateIntentRequest): Observable<CreateIntentResponse> {
    return this.http.post<CreateIntentResponse>(`${this.base}/payments/${paymentId}/intent/${provider}`, req);
  }

  capturePayPal(orderId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/payments/paypal/capture?orderId=${orderId}`, {});
  }

  confirmGooglePay(paymentId: number, googlePayToken: string): Observable<void> {
    return this.http.post<void>(`${this.base}/payments/stripe/google-pay`, { paymentId, googlePayToken });
  }

  confirmStripeClient(paymentId: number, paymentIntentId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/payments/${paymentId}/confirm-stripe`, { paymentIntentId });
  }

  getExchangeRate(): Observable<number> {
    return this.http.get<ExchangeRateResponse>(`${this.base}/payments/exchange-rate`).pipe(
      map(res => res.rate)
    );
  }
}

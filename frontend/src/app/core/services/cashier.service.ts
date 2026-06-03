import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface VerifyTicketResult {
  ticketId: number;
  movieTitle: string;
  hallName: string;
  cinemaBranchName: string;
  showtimeUtc: string;
  format: string;
  row: number;
  col: number;
  seatType: string;
  status: string;
  guestEmail: string | null;
  finalAmount: number;
}

export interface OfflineSaleRequest {
  showtimeId: number;
  seats: { row: number; col: number }[];
  guestEmail: string | null;
}

export interface OfflineSaleResult {
  paymentId: number;
  totalAmount: number;
  ticketIds: number[];
}

export interface RefundResult {
  ticketId: number;
  refundedAmount: number;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class CashierService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/cashier`;

  verifyByQr(qrToken: string): Observable<VerifyTicketResult> {
    return this.http.get<VerifyTicketResult>(`${this.base}/ticket/verify`, {
      params: { qr: qrToken }
    });
  }

  getTicketById(ticketId: number): Observable<VerifyTicketResult> {
    return this.http.get<VerifyTicketResult>(`${this.base}/tickets/${ticketId}`);
  }

  useTicket(ticketId: number): Observable<VerifyTicketResult> {
    return this.http.post<VerifyTicketResult>(`${this.base}/tickets/${ticketId}/use`, {});
  }

  createOfflineSale(request: OfflineSaleRequest): Observable<OfflineSaleResult> {
    return this.http.post<OfflineSaleResult>(`${this.base}/offline-sale`, request);
  }

  refundTicket(ticketId: number): Observable<RefundResult> {
    return this.http.post<RefundResult>(`${this.base}/tickets/${ticketId}/refund`, {});
  }
}

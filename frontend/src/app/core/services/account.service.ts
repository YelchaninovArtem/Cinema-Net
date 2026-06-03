import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
// environment used for apiUrl below
import { FavoriteSummary, LoyaltyBalance } from '../models/account.models';
import { TicketSummary, TicketDetail } from '../models/ticket.models';

export interface AccountRefundResult {
  ticketId: number;
  refundedAmount: number;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/account`;

  getTickets(): Observable<TicketSummary[]> {
    return this.http.get<TicketSummary[]>(`${this.base}/tickets`);
  }

  getTicketDetail(id: number): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${environment.apiUrl}/tickets/${id}`);
  }

  getQrBlob(ticketId: number): Observable<string> {
    return this.http
      .get(`${environment.apiUrl}/tickets/${ticketId}/qr`, { responseType: 'blob' })
      .pipe(map(blob => URL.createObjectURL(blob)));
  }

  refundTicket(ticketId: number): Observable<AccountRefundResult> {
    return this.http.post<AccountRefundResult>(`${this.base}/tickets/${ticketId}/refund`, {});
  }

  getFavorites(): Observable<FavoriteSummary[]> {
    return this.http.get<FavoriteSummary[]>(`${this.base}/favorites`);
  }

  addFavorite(movieId: number): Observable<void> {
    return this.http.post<void>(`${this.base}/favorites/${movieId}`, {});
  }

  removeFavorite(movieId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/favorites/${movieId}`);
  }

  getLoyaltyBalance(): Observable<LoyaltyBalance> {
    return this.http.get<LoyaltyBalance>(`${this.base}/loyalty/balance`);
  }
}

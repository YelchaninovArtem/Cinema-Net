import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateTicketsRequest, CreateTicketsResponse, SeatMap } from '../models/ticket.models';

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getSeatMap(showtimeId: number): Observable<SeatMap> {
    return this.http.get<SeatMap>(`${this.base}/showtimes/${showtimeId}/seats`);
  }

  createTickets(request: CreateTicketsRequest): Observable<CreateTicketsResponse> {
    return this.http.post<CreateTicketsResponse>(`${this.base}/tickets`, request);
  }
}

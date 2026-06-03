export interface SeatCoord { row: number; col: number; }

export interface SeatMap {
  showtimeId: number;
  movieTitle: string;
  hallName: string;
  cinemaBranchName: string;
  city: string;
  startUtc: string;
  format: string;
  basePrice: number;
  rows: number;
  cols: number;
  layout: string[][];
  takenSeats: SeatCoord[];
}

export interface CreateTicketsRequest {
  showtimeId: number;
  seats: SeatCoord[];
  guestEmail?: string;
  promoCode?: string;
  loyaltyPointsToRedeem?: number;
}

export interface TicketDto {
  id: number;
  seat: { row: number; col: number; type: string; price: number };
  qrToken: string;
  finalAmount: number;
}

export interface CreateTicketsResponse {
  paymentId: number;
  totalAmount: number;
  tickets: TicketDto[];
}

export type TicketStatus = 'PendingPayment' | 'Paid' | 'Cancelled' | 'Used' | 'Refunded' | 'NotUsed';

export interface TicketSummary {
  id: number;
  movieId: number;
  movieTitle: string;
  showtimeUtc: string;
  hallName: string;
  format: string;
  row: number;
  col: number;
  status: TicketStatus;
  finalAmount: number;
  createdUtc: string;
}

export interface TicketDetail {
  id: number;
  showtimeId: number;
  movieId: number;
  movieTitle: string;
  showtimeUtc: string;
  hallName: string;
  format: string;
  seat: { row: number; col: number; type: string; price: number };
  status: TicketStatus;
  finalAmount: number;
  guestEmail?: string;
  qrCodeUrl?: string;
}

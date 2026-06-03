import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, TokenPayload } from '../models/auth.models';

const ACCESS_TOKEN_KEY = 'cinema.access_token';
const REFRESH_TOKEN_KEY = 'cinema.refresh_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // реактивний сигнал поточного стану авторизації
  readonly isLoggedIn = signal(this.hasValidToken());
  readonly currentUserId = signal<string | null>(this.isLoggedIn() ? this.extractUserId() : null);
  readonly currentRole = signal<string | null>(this.isLoggedIn() ? this.extractRole() : null);
  readonly currentEmail = signal<string | null>(this.isLoggedIn() ? this.extractEmail() : null);

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/register`, request)
      .pipe(tap(resp => this.storeTokens(resp)));
  }

  login(request: LoginRequest, activateSession = true): Observable<AuthResponse> {
    const response = this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, request);
    return activateSession ? response.pipe(tap(resp => this.storeTokens(resp))) : response;
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY) ?? '';
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, { refreshToken })
      .pipe(tap(resp => this.storeTokens(resp)));
  }

  logout(): void {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY) ?? '';
    this.http
      .post(`${environment.apiUrl}/auth/logout`, { refreshToken })
      .subscribe({ error: () => {} }); // best-effort revoke

    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    this.isLoggedIn.set(false);
    this.currentUserId.set(null);
    this.currentRole.set(null);
    this.currentEmail.set(null);
    this.router.navigate(['/auth/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  hasRole(role: string): boolean {
    return this.isLoggedIn() && this.currentRole() === role;
  }

  completeLogin(resp: AuthResponse): void {
    this.storeTokens(resp);
  }

  private storeTokens(resp: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, resp.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, resp.refreshToken);
    this.isLoggedIn.set(true);
    this.currentUserId.set(this.extractUserId());
    this.currentRole.set(resp.role);
    this.currentEmail.set(resp.email);
  }

  private hasValidToken(): boolean {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (!token) return false;
    const payload = this.decodePayload(token);
    return payload !== null && payload.exp * 1000 > Date.now();
  }

  private extractRole(): string | null {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    return token ? (this.decodePayload(token)?.role ?? null) : null;
  }

  private extractUserId(): string | null {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    return token ? (this.decodePayload(token)?.sub ?? null) : null;
  }

  private extractEmail(): string | null {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    return token ? (this.decodePayload(token)?.email ?? null) : null;
  }

  private decodePayload(token: string): TokenPayload | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const json = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(json) as TokenPayload;
    } catch {
      return null;
    }
  }
}

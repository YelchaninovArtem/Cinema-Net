import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  template: `
    <div class="auth-page">
      <div class="auth-card">

        <div class="ticket-perfs"></div>

        <div class="card-body">
          <div class="top-accent"></div>

          <div class="brand">
            <svg width="26" height="26" viewBox="0 0 24 24" fill="none">
              <rect x="2" y="6" width="20" height="12" rx="2" stroke="#d4a853" stroke-width="1.5"/>
              <path d="M7 6V18M17 6V18" stroke="#d4a853" stroke-width="1.5"/>
              <path d="M2 9H7M17 9H22M2 15H7M17 15H22" stroke="#d4a853" stroke-width="1.5"/>
            </svg>
            <span class="brand-name">{{ 'app.title' | translate }}</span>
          </div>

          <h1 class="card-title">{{ 'auth.register.title' | translate }}</h1>

          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">

            <div class="name-row">
              <div class="field">
                <label class="field-label">{{ 'auth.firstName' | translate }} <span class="required-marker">*</span></label>
                <input
                  class="field-input"
                  [class.has-error]="form.controls.firstName.touched && form.controls.firstName.invalid"
                  formControlName="firstName"
                  autocomplete="given-name" />
                @if (form.controls.firstName.touched && form.controls.firstName.hasError('required')) {
                  <span class="field-err">{{ 'validation.required' | translate }}</span>
                }
              </div>
              <div class="field">
                <label class="field-label">{{ 'auth.lastName' | translate }} <span class="required-marker">*</span></label>
                <input
                  class="field-input"
                  [class.has-error]="form.controls.lastName.touched && form.controls.lastName.invalid"
                  formControlName="lastName"
                  autocomplete="family-name" />
                @if (form.controls.lastName.touched && form.controls.lastName.hasError('required')) {
                  <span class="field-err">{{ 'validation.required' | translate }}</span>
                }
              </div>
            </div>

            <div class="field">
              <label class="field-label">{{ 'auth.email' | translate }} <span class="required-marker">*</span></label>
              <input
                class="field-input"
                [class.has-error]="form.controls.email.touched && form.controls.email.invalid"
                type="email"
                formControlName="email"
                autocomplete="email" />
              @if (form.controls.email.touched && form.controls.email.hasError('required')) {
                <span class="field-err">{{ 'validation.required' | translate }}</span>
              } @else if (form.controls.email.touched && form.controls.email.hasError('email')) {
                <span class="field-err">{{ 'validation.invalidEmail' | translate }}</span>
              }
            </div>

            <div class="field">
              <label class="field-label">{{ 'auth.password' | translate }} <span class="required-marker">*</span></label>
              <input
                class="field-input"
                [class.has-error]="form.controls.password.touched && form.controls.password.invalid"
                type="password"
                formControlName="password"
                autocomplete="new-password" />
              @if (form.controls.password.touched && form.controls.password.hasError('required')) {
                <span class="field-err">{{ 'validation.required' | translate }}</span>
              } @else if (form.controls.password.touched && form.controls.password.hasError('minlength')) {
                <span class="field-err">{{ 'validation.minLength' | translate: { min: 6 } }}</span>
              }
            </div>

            @if (errors().length) {
              <div class="auth-error">
                @for (e of errors(); track e) { <div>{{ e | translate }}</div> }
              </div>
            }

            <button class="auth-btn" type="submit" [disabled]="loading()">
              @if (loading()) {
                <span class="btn-spinner"></span>
              } @else {
                {{ 'auth.register.submit' | translate }}
              }
            </button>

          </form>

          <div class="card-footer">
            <a routerLink="/auth/login">{{ 'auth.register.toLogin' | translate }}</a>
          </div>
        </div>

      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }

    .auth-page {
      min-height: var(--app-content-height);
      box-sizing: border-box;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 24px 16px;
      background:
        radial-gradient(ellipse 55% 35% at 50% 0%, rgba(212,168,83,0.11) 0%, transparent 60%),
        radial-gradient(ellipse at 85% 90%, rgba(99,102,241,0.06) 0%, transparent 45%),
        #060a12;
      font-family: 'DM Sans', sans-serif;
    }

    .auth-card {
      width: 100%;
      max-width: 460px;
      display: flex;
      background: #0a0f1e;
      border-radius: 16px;
      border: 1px solid rgba(255,255,255,0.07);
      box-shadow:
        0 32px 72px rgba(0,0,0,0.6),
        0 0 0 1px rgba(212,168,83,0.05) inset;
      overflow: hidden;
      animation: card-in 0.4s cubic-bezier(0.22, 1, 0.36, 1) both;
    }
    @keyframes card-in {
      from { opacity: 0; transform: translateY(24px) scale(0.97); }
      to   { opacity: 1; transform: translateY(0) scale(1); }
    }

    .ticket-perfs {
      flex-shrink: 0;
      width: 28px;
      background-image: radial-gradient(circle at 14px 22px, #060a12 7px, transparent 7px);
      background-size: 28px 44px;
      background-repeat: repeat-y;
      border-right: 1px solid rgba(255,255,255,0.05);
    }

    .card-body {
      flex: 1;
      padding: 32px 28px;
      min-width: 0;
    }

    .top-accent {
      height: 2px;
      background: linear-gradient(90deg, #6366f1 0%, #d4a853 55%, transparent 100%);
      border-radius: 1px;
      margin-bottom: 28px;
    }

    .brand {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 18px;
    }
    .brand-name {
      font-size: 11px;
      font-weight: 600;
      letter-spacing: 0.14em;
      text-transform: uppercase;
      color: #3d4f6a;
    }

    .card-title {
      font-family: 'Syne', sans-serif;
      font-size: 30px;
      font-weight: 800;
      color: #f8fafc;
      margin: 0 0 24px;
      letter-spacing: -0.02em;
    }

    .auth-form { display: flex; flex-direction: column; gap: 14px; }

    /* Рядок з іменем і прізвищем поруч */
    .name-row { display: flex; gap: 12px; }
    .name-row .field { flex: 1; min-width: 0; }

    .field { display: flex; flex-direction: column; gap: 7px; }

    .field-label {
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      color: #4a6080;
    }
    .required-marker {
      color: #d4a853;
      font-weight: 900;
    }

    .field-input {
      background: rgba(255,255,255,0.035);
      border: 1px solid rgba(255,255,255,0.09);
      border-radius: 10px;
      padding: 13px 16px;
      font-size: 15px;
      font-family: 'DM Sans', sans-serif;
      color: #f1f5f9;
      outline: none;
      transition: border-color 0.18s, box-shadow 0.18s, background 0.18s;
      width: 100%;
      box-sizing: border-box;
    }
    .field-input::placeholder { color: #283347; }

    /* Скасовуємо браузерне autofill-забарвлення */
    .field-input:-webkit-autofill,
    .field-input:-webkit-autofill:hover,
    .field-input:-webkit-autofill:focus {
      -webkit-box-shadow: 0 0 0 1000px #0a0f1e inset;
      -webkit-text-fill-color: #f1f5f9;
      caret-color: #f1f5f9;
      transition: background-color 5000s ease-in-out 0s;
    }

    .field-input:focus {
      border-color: rgba(99,102,241,0.6);
      background: rgba(99,102,241,0.05);
      box-shadow: 0 0 0 3px rgba(99,102,241,0.1);
    }
    .field-input.has-error { border-color: rgba(248,113,113,0.5); }
    .field-input.has-error:focus {
      border-color: rgba(248,113,113,0.7);
      background: rgba(239,68,68,0.04);
      box-shadow: 0 0 0 3px rgba(239,68,68,0.1);
    }

    .field-err { font-size: 12px; color: #f87171; padding-left: 2px; }

    .auth-error {
      background: rgba(239,68,68,0.07);
      border: 1px solid rgba(239,68,68,0.18);
      border-radius: 8px;
      padding: 11px 14px;
      font-size: 13px;
      color: #fca5a5;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .auth-btn {
      width: 100%;
      padding: 14px;
      margin-top: 4px;
      background: linear-gradient(135deg, #d4a853 0%, #b8871e 100%);
      border: none;
      border-radius: 10px;
      color: #07091a;
      font-size: 14px;
      font-weight: 700;
      font-family: 'Syne', sans-serif;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 50px;
      transition: opacity 0.18s, transform 0.15s, box-shadow 0.18s;
    }
    .auth-btn:hover:not(:disabled) {
      opacity: 0.88;
      transform: translateY(-1px);
      box-shadow: 0 10px 28px rgba(212,168,83,0.28);
    }
    .auth-btn:active:not(:disabled) { transform: translateY(0); }
    .auth-btn:disabled { opacity: 0.38; cursor: not-allowed; }

    .btn-spinner {
      width: 20px; height: 20px;
      border: 2px solid rgba(7,9,26,0.2);
      border-top-color: #07091a;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .card-footer { margin-top: 22px; text-align: center; }
    .card-footer a {
      font-size: 13px;
      color: #4f5fa8;
      text-decoration: none;
      transition: color 0.18s;
    }
    .card-footer a:hover { color: #818cf8; }
  `],
})
export class RegisterComponent {
  private readonly fb   = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errors  = signal<string[]>([]);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName:  ['', Validators.required],
    email:     ['', [Validators.required, Validators.email]],
    password:  ['', [Validators.required, Validators.minLength(6)]],
  });

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.errors.set([]);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/catalog']);
      },
      error: err => {
        this.loading.set(false);
        const body = err?.error;
        this.errors.set(Array.isArray(body) ? body : ['auth.register.failed']);
      },
    });
  }
}

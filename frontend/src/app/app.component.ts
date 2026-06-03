import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from './core/auth/auth.service';

const LANGUAGE_STORAGE_KEY = 'cinema.lang';
const SUPPORTED_LANGS = ['en', 'uk'] as const;
type SupportedLang = (typeof SUPPORTED_LANGS)[number];

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslateModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  private readonly translate = inject(TranslateService);
  readonly auth = inject(AuthService);
  readonly languages = SUPPORTED_LANGS;

  currentLang: SupportedLang;
  mobileOpen = false;

  constructor() {
    const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY) as SupportedLang | null;
    this.currentLang = stored && SUPPORTED_LANGS.includes(stored) ? stored : 'en';
    this.translate.addLangs([...SUPPORTED_LANGS]);
    this.translate.use(this.currentLang);
  }

  switchLanguage(lang: SupportedLang): void {
    this.currentLang = lang;
    this.translate.use(lang);
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
  }
}

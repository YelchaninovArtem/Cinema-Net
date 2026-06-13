import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from './core/auth/auth.service';
import { LanguageService, SupportedLang } from './core/services/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslateModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  private readonly translate = inject(TranslateService);
  private readonly language = inject(LanguageService);
  readonly auth = inject(AuthService);
  readonly languages = this.language.supportedLangs;

  currentLang: SupportedLang;
  mobileOpen = false;

  constructor() {
    this.currentLang = this.language.currentLang;
    this.translate.addLangs([...this.languages]);
    this.translate.use(this.currentLang);
  }

  switchLanguage(lang: SupportedLang): void {
    this.currentLang = lang;
    this.translate.use(lang);
    this.language.setLanguage(lang);
  }
}

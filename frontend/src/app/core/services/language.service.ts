import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type SupportedLang = 'en' | 'uk';

const LANGUAGE_STORAGE_KEY = 'cinema.lang';
const SUPPORTED_LANGS: readonly SupportedLang[] = ['en', 'uk'];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly langSubject = new BehaviorSubject<SupportedLang>(this.readInitialLang());

  readonly lang$ = this.langSubject.asObservable();
  readonly supportedLangs = SUPPORTED_LANGS;

  get currentLang(): SupportedLang {
    return this.langSubject.value;
  }

  setLanguage(lang: SupportedLang): void {
    this.langSubject.next(lang);
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
  }

  private readInitialLang(): SupportedLang {
    const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY) as SupportedLang | null;
    return stored && SUPPORTED_LANGS.includes(stored) ? stored : 'en';
  }
}

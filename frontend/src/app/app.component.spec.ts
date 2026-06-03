import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { provideTranslateService, TranslateLoader, TranslateModule, TranslationObject } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AppComponent } from './app.component';

class FakeLoader implements TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

describe('AppComponent', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [AppComponent, TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeLoader } }), RouterTestingModule],
      providers: [provideNoopAnimations(), provideHttpClient(), provideHttpClientTesting(), provideTranslateService(), provideRouter([])]
    }).compileComponents();
  });

  it('creates the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('defaults to English when no stored preference exists', () => {
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance.currentLang).toBe('en');
  });

  it('persists language selection to localStorage', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.componentInstance.switchLanguage('uk');
    expect(localStorage.getItem('cinema.lang')).toBe('uk');
    expect(fixture.componentInstance.currentLang).toBe('uk');
  });
});

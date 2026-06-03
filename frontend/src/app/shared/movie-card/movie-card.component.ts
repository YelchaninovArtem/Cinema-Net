import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MovieSummary } from '../../core/models/catalog.models';

@Component({
  selector: 'app-movie-card',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  template: `
    <a class="card" [routerLink]="['/catalog/movies', movie().id]">

      <!-- Постер -->
      <div class="poster-wrap">
        <img
          class="poster-img"
          [src]="movie().posterUrl ?? 'https://placehold.co/400x600/0d1422/4a6080?text=' + encodeTitle(movie().title)"
          [alt]="movie().title"
          loading="lazy" />

        <!-- Формати (поверх постера) -->
        <div class="format-row">
          @for (f of movie().availableFormats; track f) {
            <span class="fmt-badge" [attr.data-fmt]="f">{{ f }}</span>
          }
        </div>

        <!-- Ховер-оверлей -->
        <div class="overlay">
          <span class="overlay-cta">{{ 'catalog.buyTicket' | translate }}</span>
        </div>
      </div>

      <!-- Інфо -->
      <div class="card-body">
        <h3 class="card-title">{{ movie().title }}</h3>
        <p class="card-meta">
          <span>{{ movie().durationMinutes }} min</span>
          <span class="dot">·</span>
          <span class="age-badge">{{ movie().ageRating }}</span>
        </p>
        <div class="genre-row">
          @for (g of movie().genres; track g) {
            <span class="genre-chip">{{ g }}</span>
          }
        </div>
      </div>

    </a>
  `,
  styles: [`
    :host { display: block; height: 100%; }

    .card {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: #0d1422;
      border: 1px solid rgba(255,255,255,0.07);
      border-radius: 14px;
      overflow: hidden;
      text-decoration: none;
      color: inherit;
      transition: border-color 0.22s, transform 0.22s, box-shadow 0.22s;
      cursor: pointer;
    }
    .card:hover {
      border-color: rgba(212,168,83,0.3);
      transform: translateY(-4px);
      box-shadow: 0 16px 40px rgba(0,0,0,0.5), 0 0 0 1px rgba(212,168,83,0.12);
    }

    /* ── Постер ── */
    .poster-wrap {
      position: relative;
      aspect-ratio: 2/3;
      overflow: hidden;
      background: #060a12;
      flex-shrink: 0;
    }

    .poster-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
      transition: transform 0.35s ease;
    }
    .card:hover .poster-img { transform: scale(1.04); }

    /* Формати */
    .format-row {
      position: absolute;
      top: 10px;
      left: 10px;
      display: flex;
      gap: 5px;
      z-index: 2;
    }
    .fmt-badge {
      padding: 3px 8px;
      border-radius: 5px;
      font-size: 10px;
      font-weight: 800;
      font-family: 'Syne', sans-serif;
      letter-spacing: 0.06em;
      background: rgba(6,10,18,0.75);
      backdrop-filter: blur(4px);
      border: 1px solid rgba(255,255,255,0.12);
      color: #94a3b8;
    }
    .fmt-badge[data-fmt="IMAX"] {
      background: rgba(99,102,241,0.25);
      border-color: rgba(99,102,241,0.4);
      color: #a5b4fc;
    }
    .fmt-badge[data-fmt="3D"] {
      background: rgba(212,168,83,0.2);
      border-color: rgba(212,168,83,0.35);
      color: #d4a853;
    }

    /* Ховер-оверлей */
    .overlay {
      position: absolute;
      inset: 0;
      background: linear-gradient(to top,
        rgba(6,10,18,0.92) 0%,
        rgba(6,10,18,0.3) 50%,
        transparent 100%);
      display: flex;
      align-items: flex-end;
      justify-content: center;
      padding-bottom: 16px;
      opacity: 0;
      transition: opacity 0.22s;
      z-index: 3;
    }
    .card:hover .overlay { opacity: 1; }

    .overlay-cta {
      font-family: 'Syne', sans-serif;
      font-size: 13px;
      font-weight: 700;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      color: #d4a853;
      border: 1px solid rgba(212,168,83,0.5);
      border-radius: 8px;
      padding: 8px 18px;
      background: rgba(6,10,18,0.7);
      backdrop-filter: blur(4px);
    }

    /* ── Тіло картки ── */
    .card-body {
      padding: 12px 14px 14px;
      display: flex;
      flex-direction: column;
      gap: 6px;
      flex: 1;
    }

    .card-title {
      font-family: 'Syne', sans-serif;
      font-size: 14px;
      font-weight: 700;
      color: #f1f5f9;
      margin: 0;
      line-height: 1.3;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .card-meta {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 11px;
      color: #4a6080;
      margin: 0;
    }
    .dot { color: #2a3a50; }
    .age-badge {
      background: rgba(99,102,241,0.1);
      border: 1px solid rgba(99,102,241,0.2);
      border-radius: 4px;
      padding: 1px 5px;
      font-size: 10px;
      font-weight: 700;
      color: #818cf8;
      letter-spacing: 0.04em;
    }

    .genre-row {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
    }
    .genre-chip {
      font-size: 10px;
      font-weight: 600;
      color: #fb923c;
      background: rgba(251,146,60,0.12);
      border: 1px solid rgba(251,146,60,0.3);
      border-radius: 5px;
      padding: 2px 7px;
    }
  `],
})
export class MovieCardComponent {
  readonly movie = input.required<MovieSummary>();

  encodeTitle(title: string): string {
    return encodeURIComponent(title);
  }
}

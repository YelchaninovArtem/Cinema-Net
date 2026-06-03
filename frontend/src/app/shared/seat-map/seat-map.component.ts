import { Component, input, output, computed, inject, signal } from '@angular/core';
import { SeatCoord, SeatMap } from '../../core/models/ticket.models';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

type SeatState = 'available' | 'taken' | 'selected';

interface SeatCell {
  row: number;
  col: number;
  type: string;
  state: SeatState;
}

@Component({
  selector: 'app-seat-map',
  standalone: true,
  imports: [TranslateModule],
  template: `
    <div class="sm-shell">

      <!-- Legend -->
      <div class="sm-legend">
        <span class="leg"><span class="dot std"></span>{{ 'booking.legend.available' | translate }}</span>
        <span class="leg"><span class="dot vip"></span>{{ 'booking.legend.vip' | translate }}</span>
        <span class="leg"><span class="dot love"></span>{{ 'booking.legend.love' | translate }}</span>
        <span class="leg"><span class="dot sel"></span>{{ 'booking.legend.selected' | translate }}</span>
        <span class="leg"><span class="dot tkn"></span>{{ 'booking.legend.taken' | translate }}</span>
      </div>

      @if (seatMap().format === '3D') {
        <div class="sm-3d-note">{{ 'booking.threeDGlassesNote' | translate }}</div>
      }

      <!-- Screen arc -->
      <div class="sm-screen-wrap">
        <div class="sm-screen">
          <span class="sm-screen-txt">{{ 'booking.screen' | translate }}</span>
        </div>
        <div class="sm-screen-glow"></div>
      </div>

      <!-- Seat grid -->
      <div class="sm-grid">
        @for (row of grid(); track $index) {
          <div class="sm-row">
            <span class="sm-rnum">{{ $index + 1 }}</span>
            @for (cell of row; track cell.col) {
              <button
                class="seat"
                [class]="seatClass(cell)"
                [disabled]="cell.state === 'taken'"
                [title]="seatTitle(cell)"
                (click)="toggle(cell)">
              </button>
            }
          </div>
        }
      </div>

    </div>
  `,
  styles: [`
    :host { display: block; font-family: 'DM Sans', 'Roboto', sans-serif; }

    .sm-shell {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 24px;
      padding: 32px 28px;
      background: #0d1422;
      border-radius: 16px;
      border: 1px solid rgba(255,255,255,0.06);
    }

    /* Legend */
    .sm-legend {
      display: flex;
      flex-wrap: wrap;
      gap: 8px 20px;
      justify-content: center;
    }
    .leg {
      display: flex;
      align-items: center;
      gap: 7px;
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.02em;
      color: #6b7fa8;
    }
    .dot {
      width: 14px; height: 14px;
      border-radius: 3px;
      flex-shrink: 0;
    }
    .dot.std  { background: #22c55e; box-shadow: 0 0 8px rgba(34,197,94,0.55); }
    .dot.vip  { background: #f59e0b; box-shadow: 0 0 8px rgba(245,158,11,0.55); }
    .dot.love { background: #f43f8e; box-shadow: 0 0 8px rgba(244,63,142,0.55); }
    .dot.sel  { background: #818cf8; box-shadow: 0 0 8px rgba(129,140,248,0.65); }
    .dot.tkn  { background: #3a2030; border: 1px solid rgba(180,60,80,0.4); }

    /* Screen */
    .sm-screen-wrap {
      width: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
    }
    .sm-screen {
      width: min(460px, 92%);
      height: 8px;
      background: linear-gradient(90deg,
        transparent 0%,
        rgba(255,255,255,0.12) 12%,
        rgba(255,255,255,0.6) 38%,
        rgba(255,255,255,0.82) 50%,
        rgba(255,255,255,0.6) 62%,
        rgba(255,255,255,0.12) 88%,
        transparent 100%
      );
      border-radius: 50% 50% 0 0 / 100% 100% 0 0;
      position: relative;
      display: flex;
      justify-content: center;
    }
    .sm-screen-txt {
      position: absolute;
      bottom: -20px;
      font-size: 9px;
      font-weight: 700;
      letter-spacing: 0.3em;
      color: rgba(255,255,255,0.18);
      text-transform: uppercase;
    }
    .sm-screen-glow {
      width: min(300px, 65%);
      height: 48px;
      background: radial-gradient(ellipse at top,
        rgba(140,165,255,0.14) 0%,
        transparent 70%
      );
    }

    /* Grid */
    .sm-grid {
      display: flex;
      flex-direction: column;
      gap: 6px;
      margin-top: 4px;
    }
    .sm-row {
      display: flex;
      align-items: center;
      gap: 5px;
    }
    .sm-rnum {
      width: 20px;
      font-size: 10px;
      font-weight: 600;
      color: rgba(255,255,255,0.18);
      text-align: right;
      flex-shrink: 0;
      font-variant-numeric: tabular-nums;
    }

    /* Seat base */
    .seat {
      width: 30px;
      height: 26px;
      border-radius: 7px 7px 4px 4px;
      border: none;
      cursor: pointer;
      position: relative;
      transition:
        transform 0.18s cubic-bezier(0.34, 1.56, 0.64, 1),
        box-shadow 0.15s ease,
        filter 0.15s ease;
      outline: none;
    }
    /* backrest highlight */
    .seat::after {
      content: '';
      position: absolute;
      top: 3px;
      left: 18%;
      right: 18%;
      height: 3px;
      background: rgba(255,255,255,0.22);
      border-radius: 2px;
      pointer-events: none;
    }
    .seat:hover:not(:disabled) {
      transform: translateY(-3px) scale(1.1);
      filter: brightness(1.3);
    }
    .seat:active:not(:disabled) { transform: translateY(-1px) scale(1.03); }
    .seat:disabled { cursor: not-allowed; }

    /* Standard */
    .seat.std-available {
      background: linear-gradient(170deg, #2ecc71 0%, #1a9e52 100%);
      box-shadow: 0 3px 0 #0f7a35, 0 6px 14px rgba(34,197,94,0.18);
    }
    /* VIP */
    .seat.vip-available {
      background: linear-gradient(170deg, #fbbf24 0%, #d97706 100%);
      box-shadow: 0 3px 0 #92520a, 0 6px 14px rgba(245,158,11,0.22);
    }
    /* Love */
    .seat.love-available {
      background: linear-gradient(170deg, #fb7185 0%, #e0185e 100%);
      box-shadow: 0 3px 0 #981248, 0 6px 14px rgba(244,63,142,0.22);
    }
    /* Selected */
    .seat.selected {
      background: linear-gradient(170deg, #a5b4fc 0%, #6366f1 100%);
      box-shadow:
        0 3px 0 #3730a3,
        0 6px 20px rgba(99,102,241,0.45),
        0 0 28px rgba(99,102,241,0.28);
      animation: seat-pop 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
    }
    .sm-3d-note {
      width: 100%;
      max-width: 560px;
      padding: 10px 14px;
      border: 1px solid rgba(129,140,248,0.28);
      border-radius: 8px;
      background: rgba(99,102,241,0.1);
      color: #c7d2fe;
      font-size: 13px;
      line-height: 1.45;
      text-align: center;
    }
    /* Taken */
    .seat.taken {
      background: linear-gradient(170deg, #3a2030 0%, #2a1525 100%);
      box-shadow: 0 2px 0 #1a0d18, inset 0 1px 0 rgba(255,255,255,0.04);
      opacity: 0.75;
      border: 1px solid rgba(180,60,80,0.25);
    }
    /* замість підголовника - діагональна лінія "зайнято" */
    .seat.taken::after {
      content: '';
      position: absolute;
      top: 50%; left: 50%;
      width: 60%; height: 2px;
      background: rgba(180,60,80,0.55);
      border-radius: 1px;
      transform: translate(-50%, -50%) rotate(45deg);
      pointer-events: none;
    }

    @keyframes seat-pop {
      0%  { transform: scale(0.75); }
      55% { transform: scale(1.15); }
      100%{ transform: scale(1); }
    }
  `],
})
export class SeatMapComponent {
  private readonly translate = inject(TranslateService);

  readonly seatMap  = input.required<SeatMap>();
  readonly maxSeats = input<number>(10);
  readonly selected = output<SeatCoord[]>();

  private readonly _selected = signal<SeatCoord[]>([]);

  readonly grid = computed<SeatCell[][]>(() => {
    const map      = this.seatMap();
    const selected = this._selected();
    return Array.from({ length: map.rows }, (_, ri) =>
      Array.from({ length: map.cols }, (_, ci) => {
        const row = ri + 1, col = ci + 1;
        const taken    = map.takenSeats.some(t => t.row === row && t.col === col);
        const selState = selected.some(s => s.row === row && s.col === col);
        return {
          row, col,
          type:  map.layout[ri][ci],
          state: taken ? 'taken' : selState ? 'selected' : 'available',
        } as SeatCell;
      })
    );
  });

  toggle(cell: SeatCell): void {
    if (cell.state === 'taken') return;
    const current = this._selected();
    const idx = current.findIndex(s => s.row === cell.row && s.col === cell.col);
    let next: SeatCoord[];
    if (idx >= 0) {
      next = current.filter((_, i) => i !== idx);
    } else {
      if (current.length >= this.maxSeats()) return;
      next = [...current, { row: cell.row, col: cell.col }];
    }
    this._selected.set(next);
    this.selected.emit(next);
  }

  seatClass(cell: SeatCell): string {
    if (cell.state === 'taken')    return 'taken';
    if (cell.state === 'selected') return 'selected';
    const t = cell.type.toLowerCase();
    return t === 'vip' ? 'vip-available' : t === 'love' ? 'love-available' : 'std-available';
  }

  seatTitle(cell: SeatCell): string {
    const row = this.translate.instant('account.row');
    const seat = this.translate.instant('account.seat');
    return `${cell.type} · ${row} ${cell.row}, ${seat} ${cell.col}`;
  }
}

import { Component, OnInit, AfterViewInit, OnDestroy, signal, computed, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { lastValueFrom, Subscription } from 'rxjs';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, DateAdapter } from '@angular/material/core';
import { NgxMaterialTimepickerModule, NgxMaterialTimepickerTheme } from 'ngx-material-timepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { AdminService, CinemaAdminDto, HallAdminDto, MovieAdminDto, ShowtimeAdminDto, SalesReportItem, OccupancyReportItem, SeatTypeCode, PromoCodeDto, UserDto, StaffUserDto, CreateStaffUserRequest, AdminReviewDto } from '../../core/services/admin.service';
import { CinemaService } from '../../core/services/cinema.service';
import { MovieService } from '../../core/services/movie.service';
import { TmdbImportDialogComponent, TmdbMovieDetail } from './tmdb-import-dialog/tmdb-import-dialog.component';
import { LocalizedDatePipe } from '../../shared/localized-date.pipe';

type AdminTab = 'cinemas' | 'halls' | 'movies' | 'showtimes' | 'promos' | 'reports' | 'staff' | 'reviews';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    TranslateModule,
    LocalizedDatePipe,
    MatTabsModule,
    MatTableModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    NgxMaterialTimepickerModule,
    MatSnackBarModule,
    MatChipsModule,
    MatTooltipModule,
    MatCheckboxModule,
  ],
  providers: [provideNativeDateAdapter()],
  template: `
    <div class="admin-page">
      <div class="admin-container">
        <header class="admin-header">
          <h1 class="admin-title">{{ 'admin.title' | translate }}</h1>
        </header>

        <mat-tab-group class="admin-tabs" [(selectedIndex)]="selectedTabIndex" (selectedIndexChange)="onTabChange($event)">
        <mat-tab [label]="'admin.cinemas' | translate">
          <div class="tab-content">
            <div class="toolbar">
              <button mat-flat-button color="primary" (click)="openCinemaDialog()">
                <mat-icon>add</mat-icon>
                {{ 'admin.addCinema' | translate }}
              </button>
            </div>
            <table mat-table [dataSource]="sortedCinemas()" class="admin-table">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('cinemas','name')">{{ 'admin.name' | translate }}<span class="sort-icon">{{ sortDir('cinemas','name') === 'asc' ? '↑' : sortDir('cinemas','name') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let c" [class.sort-col]="sortDir('cinemas','name')">{{ c.name }}</td>
              </ng-container>
              <ng-container matColumnDef="city">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('cinemas','city')">{{ 'admin.city' | translate }}<span class="sort-icon">{{ sortDir('cinemas','city') === 'asc' ? '↑' : sortDir('cinemas','city') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let c" [class.sort-col]="sortDir('cinemas','city')">{{ c.city }}</td>
              </ng-container>
              <ng-container matColumnDef="address">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('cinemas','address')">{{ 'admin.address' | translate }}<span class="sort-icon">{{ sortDir('cinemas','address') === 'asc' ? '↑' : sortDir('cinemas','address') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let c" [class.sort-col]="sortDir('cinemas','address')">{{ c.address }}</td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let c">
                  <button mat-icon-button (click)="openCinemaDialog(c)" [matTooltip]="'admin.edit' | translate">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="deleteCinema(c.id)" [matTooltip]="'admin.delete' | translate">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>
              <ng-container matColumnDef="empty">
                <td mat-footer-cell *matFooterCellDef [attr.colspan]="cinemaColumns.length">
                  <span class="empty-state">{{ 'admin.noData' | translate }}</span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="cinemaColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: cinemaColumns;"></tr>
              <tr mat-footer-row *matFooterRowDef="['empty']" [class.empty-row-hidden]="cinemas().length > 0"></tr>
            </table>
          </div>
        </mat-tab>

        <mat-tab [label]="'admin.halls' | translate">
          <div class="tab-content">
            <div class="toolbar">
              <mat-form-field appearance="outline" class="filter-field">
                <mat-label>{{ 'admin.cinema' | translate }}</mat-label>
                <mat-select
                  [(ngModel)]="hallCinemaFilter"
                  (selectionChange)="loadHalls()"
                  (openedChange)="focusSelectSearch($event, hallFilterCinemaSearchInput)">
                  <div class="select-search" (click)="$event.stopPropagation()" (keydown)="$event.stopPropagation()">
                    <mat-icon>search</mat-icon>
                    <input
                      #hallFilterCinemaSearchInput
                      [(ngModel)]="hallFilterCinemaSearch"
                      [ngModelOptions]="{ standalone: true }"
                      [placeholder]="'admin.searchCinema' | translate"
                      autocomplete="off"
                      (keydown.enter)="$event.preventDefault()">
                  </div>
                  <mat-option [value]="null">{{ 'admin.allCinemas' | translate }}</mat-option>
                  @for (c of filteredHallFilterCinemas(); track c.id) {
                    <mat-option [value]="c.id">{{ c.name }}</mat-option>
                  }
                  @if (filteredHallFilterCinemas().length === 0) {
                    <mat-option disabled>{{ 'admin.noSearchResults' | translate }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <button mat-flat-button color="primary" (click)="openHallDialog()">
                <mat-icon>add</mat-icon>
                {{ 'admin.addHall' | translate }}
              </button>
            </div>
            <table mat-table [dataSource]="sortedHalls()" class="admin-table">
              <ng-container matColumnDef="hallName">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('halls','hallName')">{{ 'admin.hallName' | translate }}<span class="sort-icon">{{ sortDir('halls','hallName') === 'asc' ? '↑' : sortDir('halls','hallName') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let h" [class.sort-col]="sortDir('halls','hallName')">{{ h.hallName }}</td>
              </ng-container>
              <ng-container matColumnDef="cinemaName">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('halls','cinemaName')">{{ 'admin.cinema' | translate }}<span class="sort-icon">{{ sortDir('halls','cinemaName') === 'asc' ? '↑' : sortDir('halls','cinemaName') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let h" [class.sort-col]="sortDir('halls','cinemaName')">{{ h.cinemaName }}</td>
              </ng-container>
              <ng-container matColumnDef="size">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.size' | translate }}</th>
                <td mat-cell *matCellDef="let h">{{ h.rows }} × {{ h.cols }}</td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let h">
                  <button mat-icon-button (click)="openHallDialog(h)" [matTooltip]="'admin.edit' | translate">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="deleteHall(h.id)" [matTooltip]="'admin.delete' | translate">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>
              <ng-container matColumnDef="empty">
                <td mat-footer-cell *matFooterCellDef [attr.colspan]="hallColumns.length">
                  <span class="empty-state">{{ 'admin.noData' | translate }}</span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="hallColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: hallColumns;"></tr>
              <tr mat-footer-row *matFooterRowDef="['empty']" [class.empty-row-hidden]="halls().length > 0"></tr>
            </table>
          </div>
        </mat-tab>

        <mat-tab [label]="'admin.movies' | translate">
          <div class="tab-content">
            <div class="toolbar">
              <button mat-flat-button color="primary" (click)="openMovieDialog()">
                <mat-icon>add</mat-icon>
                {{ 'admin.addMovie' | translate }}
              </button>
              <button class="tmdb-import-btn" (click)="openTmdbImport()">
                <mat-icon>movie_filter</mat-icon>
                {{ 'tmdb.importButton' | translate }}
              </button>
            </div>
            <table mat-table [dataSource]="sortedMovies()" class="admin-table">
              <ng-container matColumnDef="title">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('movies','title')">{{ 'admin.title_field' | translate }}<span class="sort-icon">{{ sortDir('movies','title') === 'asc' ? '↑' : sortDir('movies','title') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let m" [class.sort-col]="sortDir('movies','title')">{{ m.title }}</td>
              </ng-container>
              <ng-container matColumnDef="duration">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('movies','durationMinutes')">{{ 'admin.duration' | translate }}<span class="sort-icon">{{ sortDir('movies','durationMinutes') === 'asc' ? '↑' : sortDir('movies','durationMinutes') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let m" [class.sort-col]="sortDir('movies','durationMinutes')">{{ m.durationMinutes }} min</td>
              </ng-container>
              <ng-container matColumnDef="ageRating">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('movies','ageRating')">{{ 'admin.ageRating' | translate }}<span class="sort-icon">{{ sortDir('movies','ageRating') === 'asc' ? '↑' : sortDir('movies','ageRating') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let m" [class.sort-col]="sortDir('movies','ageRating')">
                  <span class="age-badge">{{ m.ageRating }}</span>
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let m">
                  <button mat-icon-button (click)="openMovieDialog(m)" [matTooltip]="'admin.edit' | translate">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="deleteMovie(m.id)" [matTooltip]="'admin.delete' | translate">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>
              <ng-container matColumnDef="empty">
                <td mat-footer-cell *matFooterCellDef [attr.colspan]="movieColumns.length">
                  <span class="empty-state">{{ 'admin.noData' | translate }}</span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="movieColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: movieColumns;"></tr>
              <tr mat-footer-row *matFooterRowDef="['empty']" [class.empty-row-hidden]="movies().length > 0"></tr>
            </table>
          </div>
        </mat-tab>

        <mat-tab [label]="'admin.showtimes' | translate">
          <div class="tab-content">
            <div class="toolbar">
              <mat-form-field appearance="outline" class="filter-field">
                <mat-label>{{ 'admin.cinema' | translate }}</mat-label>
                <mat-select
                  [(ngModel)]="showtimeCinemaFilter"
                  (selectionChange)="loadShowtimes()"
                  (openedChange)="focusSelectSearch($event, showtimeFilterCinemaSearchInput)">
                  <div class="select-search" (click)="$event.stopPropagation()" (keydown)="$event.stopPropagation()">
                    <mat-icon>search</mat-icon>
                    <input
                      #showtimeFilterCinemaSearchInput
                      [(ngModel)]="showtimeFilterCinemaSearch"
                      [ngModelOptions]="{ standalone: true }"
                      [placeholder]="'admin.searchCinema' | translate"
                      autocomplete="off"
                      (keydown.enter)="$event.preventDefault()">
                  </div>
                  <mat-option [value]="null">{{ 'admin.allCinemas' | translate }}</mat-option>
                  @for (c of filteredShowtimeFilterCinemas(); track c.id) {
                    <mat-option [value]="c.id">{{ c.name }}</mat-option>
                  }
                  @if (filteredShowtimeFilterCinemas().length === 0) {
                    <mat-option disabled>{{ 'admin.noSearchResults' | translate }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <button mat-flat-button color="primary" (click)="openShowtimeDialog()">
                <mat-icon>add</mat-icon>
                {{ 'admin.addShowtime' | translate }}
              </button>
            </div>
            <table mat-table [dataSource]="sortedShowtimes()" class="admin-table">
              <ng-container matColumnDef="movieTitle">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('showtimes','movieTitle')">{{ 'admin.movie' | translate }}<span class="sort-icon">{{ sortDir('showtimes','movieTitle') === 'asc' ? '↑' : sortDir('showtimes','movieTitle') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let s" [class.sort-col]="sortDir('showtimes','movieTitle')">{{ s.movieTitle }}</td>
              </ng-container>
              <ng-container matColumnDef="hallName">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('showtimes','hallName')">{{ 'admin.hall' | translate }}<span class="sort-icon">{{ sortDir('showtimes','hallName') === 'asc' ? '↑' : sortDir('showtimes','hallName') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let s" [class.sort-col]="sortDir('showtimes','hallName')">{{ s.hallName }}</td>
              </ng-container>
              <ng-container matColumnDef="startUtc">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('showtimes','startUtc')">{{ 'admin.dateTime' | translate }}<span class="sort-icon">{{ sortDir('showtimes','startUtc') === 'asc' ? '↑' : sortDir('showtimes','startUtc') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let s" [class.sort-col]="sortDir('showtimes','startUtc')">{{ formatShowtimeTime(s.startUtc) }}</td>
              </ng-container>
              <ng-container matColumnDef="format">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('showtimes','format')">{{ 'admin.format' | translate }}<span class="sort-icon">{{ sortDir('showtimes','format') === 'asc' ? '↑' : sortDir('showtimes','format') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let s" [class.sort-col]="sortDir('showtimes','format')">{{ formatLabel(s.format) }}</td>
              </ng-container>
              <ng-container matColumnDef="price">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('showtimes','basePrice')">{{ 'admin.price' | translate }}<span class="sort-icon">{{ sortDir('showtimes','basePrice') === 'asc' ? '↑' : sortDir('showtimes','basePrice') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let s" [class.sort-col]="sortDir('showtimes','basePrice')">{{ s.basePrice | currency:'UAH':'symbol':'1.0-0' }}</td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let s">
                  <button mat-icon-button (click)="openShowtimeDialog(s)" [matTooltip]="'admin.edit' | translate">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="deleteShowtime(s.id)" [matTooltip]="'admin.delete' | translate">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>
              <ng-container matColumnDef="empty">
                <td mat-footer-cell *matFooterCellDef [attr.colspan]="showtimeColumns.length">
                  <span class="empty-state">{{ 'admin.noData' | translate }}</span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="showtimeColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: showtimeColumns;"></tr>
              <tr mat-footer-row *matFooterRowDef="['empty']" [class.empty-row-hidden]="showtimes().length > 0"></tr>
            </table>
          </div>
        </mat-tab>

        <mat-tab [label]="'admin.promoCodes' | translate">
          <div class="tab-content">
            <div class="toolbar">
              <button mat-flat-button color="primary" (click)="openPromoDialog()">
                <mat-icon>add</mat-icon>
                {{ 'admin.addPromo' | translate }}
              </button>
            </div>
            <table mat-table [dataSource]="sortedPromos()" class="admin-table">
              <ng-container matColumnDef="code">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('promos','code')">{{ 'admin.code' | translate }}<span class="sort-icon">{{ sortDir('promos','code') === 'asc' ? '↑' : sortDir('promos','code') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let p" [class.sort-col]="sortDir('promos','code')"><code>{{ p.code }}</code></td>
              </ng-container>
              <ng-container matColumnDef="discountType">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('promos','discountType')">{{ 'admin.discountType' | translate }}<span class="sort-icon">{{ sortDir('promos','discountType') === 'asc' ? '↑' : sortDir('promos','discountType') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let p" [class.sort-col]="sortDir('promos','discountType')">{{ p.discountType === 'Percent' ? ('admin.percent' | translate) : ('admin.fixed' | translate) }} {{ p.value }}{{ p.discountType === 'Percent' ? '%' : '₴' }}</td>
              </ng-container>
              <ng-container matColumnDef="validPeriod">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('promos','validFrom')">{{ 'admin.validFrom' | translate }} - {{ 'admin.validTo' | translate }}<span class="sort-icon">{{ sortDir('promos','validFrom') === 'asc' ? '↑' : sortDir('promos','validFrom') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let p" [class.sort-col]="sortDir('promos','validFrom')">{{ p.validFrom | localizedDate:'dd/MM/yyyy' }} - {{ p.validTo | localizedDate:'dd/MM/yyyy' }}</td>
              </ng-container>
              <ng-container matColumnDef="usage">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('promos','usageCount')">{{ 'admin.usageLimit' | translate }}<span class="sort-icon">{{ sortDir('promos','usageCount') === 'asc' ? '↑' : sortDir('promos','usageCount') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let p" [class.sort-col]="sortDir('promos','usageCount')">{{ p.usageLimit === 0 ? ('admin.unlimited' | translate) : p.usageCount + '/' + p.usageLimit }}</td>
              </ng-container>
              <ng-container matColumnDef="personal">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.personal' | translate }}</th>
                <td mat-cell *matCellDef="let p">
                  @if (p.isPersonal) {
                    <span class="chip chip-personal">{{ 'admin.yes' | translate }}</span>
                  } @else {
                    <span class="chip chip-public">{{ 'admin.no' | translate }}</span>
                  }
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let p">
                  <button mat-icon-button (click)="openPromoDialog(p)" [matTooltip]="'admin.edit' | translate">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="deletePromo(p.id)" [matTooltip]="'admin.delete' | translate">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>
              <ng-container matColumnDef="empty">
                <td mat-footer-cell *matFooterCellDef [attr.colspan]="promoColumns.length">
                  <span class="empty-state">{{ 'admin.noData' | translate }}</span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="promoColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: promoColumns;"></tr>
              <tr mat-footer-row *matFooterRowDef="['empty']" [class.empty-row-hidden]="promoCodes().length > 0"></tr>
            </table>
          </div>
        </mat-tab>

        <mat-tab [label]="'admin.reports' | translate">
          <div class="tab-content">
            <div class="toolbar report-toolbar">
              <div class="report-filter-grid">
                <mat-form-field appearance="outline">
                  <mat-label>{{ 'admin.startDate' | translate }}</mat-label>
                  <input matInput [matDatepicker]="startPicker" [(ngModel)]="reportStartDate" [max]="reportEndDate">
                  <mat-datepicker-toggle matIconSuffix [for]="startPicker"></mat-datepicker-toggle>
                  <mat-datepicker #startPicker></mat-datepicker>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>{{ 'admin.endDate' | translate }}</mat-label>
                  <input matInput [matDatepicker]="endPicker" [(ngModel)]="reportEndDate" [min]="reportStartDate">
                  <mat-datepicker-toggle matIconSuffix [for]="endPicker"></mat-datepicker-toggle>
                  <mat-datepicker #endPicker></mat-datepicker>
                </mat-form-field>
                <button type="button" class="report-generate-btn" (click)="loadReports()">
                  <mat-icon>bar_chart</mat-icon>
                  <span>{{ 'admin.generate' | translate }}</span>
                </button>
              </div>

              <div class="report-export-grid">
                <div class="export-group">
                  <span class="export-group-label">{{ 'admin.salesReport' | translate }}</span>
                  <div class="export-actions">
                    <button type="button" class="export-btn export-pdf" (click)="downloadReport('sales', 'pdf')">
                      <mat-icon>picture_as_pdf</mat-icon>
                      <span>PDF</span>
                    </button>
                    <button type="button" class="export-btn export-xlsx" (click)="downloadReport('sales', 'xlsx')">
                      <mat-icon>table_chart</mat-icon>
                      <span>Excel</span>
                    </button>
                  </div>
                </div>
                <div class="export-group">
                  <span class="export-group-label">{{ 'admin.occupancyReport' | translate }}</span>
                  <div class="export-actions">
                    <button type="button" class="export-btn export-pdf" (click)="downloadReport('occupancy', 'pdf')">
                      <mat-icon>picture_as_pdf</mat-icon>
                      <span>PDF</span>
                    </button>
                    <button type="button" class="export-btn export-xlsx" (click)="downloadReport('occupancy', 'xlsx')">
                      <mat-icon>table_chart</mat-icon>
                      <span>Excel</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <div class="reports-section">
              <section class="report-card">
                <header class="report-card-header">
                  <span class="report-card-icon"><mat-icon>payments</mat-icon></span>
                  <h3>{{ 'admin.salesReport' | translate }}</h3>
                </header>
                <div class="report-table-scroll">
                  <table mat-table [dataSource]="sortedSales()" class="admin-table report-table sales-table">
                    <ng-container matColumnDef="date">
                      <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('sales','date')">{{ 'admin.date' | translate }}<span class="sort-icon">{{ sortDir('sales','date') === 'asc' ? '↑' : sortDir('sales','date') === 'desc' ? '↓' : '↕' }}</span></th>
                      <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('sales','date')">{{ r.date | localizedDate:'dd.MM.yyyy' }}</td>
                    </ng-container>
                    <ng-container matColumnDef="totalBookings">
                      <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('sales','totalBookings')">{{ 'admin.totalBookings' | translate }}<span class="sort-icon">{{ sortDir('sales','totalBookings') === 'asc' ? '↑' : sortDir('sales','totalBookings') === 'desc' ? '↓' : '↕' }}</span></th>
                      <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('sales','totalBookings')">{{ r.totalBookings }}</td>
                    </ng-container>
                    <ng-container matColumnDef="totalRevenue">
                      <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('sales','totalRevenue')">{{ 'admin.totalRevenue' | translate }}<span class="sort-icon">{{ sortDir('sales','totalRevenue') === 'asc' ? '↑' : sortDir('sales','totalRevenue') === 'desc' ? '↓' : '↕' }}</span></th>
                      <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('sales','totalRevenue')">{{ r.totalRevenue | currency:'UAH':'symbol':'1.0-0' }}</td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="salesColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: salesColumns;"></tr>
                  </table>
                </div>
                @if (salesReport().length === 0) {
                  <div class="no-data">
                    <mat-icon>inbox</mat-icon>
                    <span>{{ 'admin.noData' | translate }}</span>
                  </div>
                }
              </section>

              <section class="report-card">
                <header class="report-card-header">
                  <span class="report-card-icon"><mat-icon>event_seat</mat-icon></span>
                  <h3>{{ 'admin.occupancyReport' | translate }}</h3>
                </header>
                <div class="report-table-scroll">
                  <table mat-table [dataSource]="sortedOccupancy()" class="admin-table report-table occupancy-table">
                    <ng-container matColumnDef="hallName">
                      <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('occupancy','hallName')">{{ 'admin.hall' | translate }}<span class="sort-icon">{{ sortDir('occupancy','hallName') === 'asc' ? '↑' : sortDir('occupancy','hallName') === 'desc' ? '↓' : '↕' }}</span></th>
                      <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('occupancy','hallName')">{{ r.hallName }}</td>
                    </ng-container>
                    <ng-container matColumnDef="movieTitle">
                      <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('occupancy','movieTitle')">{{ 'admin.movie' | translate }}<span class="sort-icon">{{ sortDir('occupancy','movieTitle') === 'asc' ? '↑' : sortDir('occupancy','movieTitle') === 'desc' ? '↓' : '↕' }}</span></th>
                      <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('occupancy','movieTitle')">{{ r.movieTitle }}</td>
                    </ng-container>
                    <ng-container matColumnDef="date">
                      <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('occupancy','date')">{{ 'admin.date' | translate }}<span class="sort-icon">{{ sortDir('occupancy','date') === 'asc' ? '↑' : sortDir('occupancy','date') === 'desc' ? '↓' : '↕' }}</span></th>
                      <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('occupancy','date')">{{ r.date | localizedDate:'dd.MM.yyyy' }}</td>
                    </ng-container>
                    <ng-container matColumnDef="occupancy">
                      <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('occupancy','occupancyPercent')">{{ 'admin.occupancy' | translate }}<span class="sort-icon">{{ sortDir('occupancy','occupancyPercent') === 'asc' ? '↑' : sortDir('occupancy','occupancyPercent') === 'desc' ? '↓' : '↕' }}</span></th>
                      <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('occupancy','occupancyPercent')">
                        <div class="occupancy-value">
                          <span>{{ r.occupiedSeats }}/{{ r.totalSeats }} ({{ r.occupancyPercent }}%)</span>
                          <span class="occupancy-meter"><span [style.width.%]="r.occupancyPercent"></span></span>
                        </div>
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="occupancyColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: occupancyColumns;"></tr>
                  </table>
                </div>
                @if (occupancyReport().length === 0) {
                  <div class="no-data">
                    <mat-icon>inbox</mat-icon>
                    <span>{{ 'admin.noData' | translate }}</span>
                  </div>
                }
              </section>
            </div>
          </div>
        </mat-tab>

        <mat-tab [label]="'admin.staff' | translate">
          <div class="tab-content">
            <div class="toolbar">
              <button mat-flat-button color="primary" (click)="openStaffDialog()">
                <mat-icon>person_add</mat-icon>
                {{ 'admin.addStaff' | translate }}
              </button>
            </div>
            <table mat-table [dataSource]="sortedStaff()" class="admin-table">
              <ng-container matColumnDef="email">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('staff','email')">{{ 'admin.email' | translate }}<span class="sort-icon">{{ sortDir('staff','email') === 'asc' ? '↑' : sortDir('staff','email') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let u" [class.sort-col]="sortDir('staff','email')">{{ u.email }}</td>
              </ng-container>
              <ng-container matColumnDef="firstName">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('staff','firstName')">{{ 'admin.firstName' | translate }}<span class="sort-icon">{{ sortDir('staff','firstName') === 'asc' ? '↑' : sortDir('staff','firstName') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let u" [class.sort-col]="sortDir('staff','firstName')">{{ u.firstName }}</td>
              </ng-container>
              <ng-container matColumnDef="lastName">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('staff','lastName')">{{ 'admin.lastName' | translate }}<span class="sort-icon">{{ sortDir('staff','lastName') === 'asc' ? '↑' : sortDir('staff','lastName') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let u" [class.sort-col]="sortDir('staff','lastName')">{{ u.lastName }}</td>
              </ng-container>
              <ng-container matColumnDef="role">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('staff','role')">{{ 'admin.role' | translate }}<span class="sort-icon">{{ sortDir('staff','role') === 'asc' ? '↑' : sortDir('staff','role') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let u" [class.sort-col]="sortDir('staff','role')">
                  <span class="role-badge" [class.role-admin]="u.role === 'Admin'" [class.role-cashier]="u.role === 'Cashier'">{{ u.role }}</span>
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let u">
                  <button mat-icon-button color="warn" (click)="deleteStaff(u.id)" [matTooltip]="'admin.delete' | translate">
                    <mat-icon>delete</mat-icon>
                  </button>
                </td>
              </ng-container>
              <ng-container matColumnDef="empty">
                <td mat-footer-cell *matFooterCellDef [attr.colspan]="staffColumns.length">
                  <span class="empty-state">{{ 'admin.noData' | translate }}</span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="staffColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: staffColumns;"></tr>
              <tr mat-footer-row *matFooterRowDef="['empty']" [class.empty-row-hidden]="staffUsers().length > 0"></tr>
            </table>
          </div>
        </mat-tab>

        <mat-tab [label]="'admin.reviews' | translate">
          <div class="tab-content">
            <table mat-table [dataSource]="sortedReviews()" class="admin-table">
              <ng-container matColumnDef="movieTitle">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('reviews','movieTitle')">{{ 'admin.movie' | translate }}<span class="sort-icon">{{ sortDir('reviews','movieTitle') === 'asc' ? '↑' : sortDir('reviews','movieTitle') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('reviews','movieTitle')">
                  <a class="review-movie-link" [routerLink]="['/catalog/movies', r.movieId]">{{ r.movieTitle }}</a>
                </td>
              </ng-container>
              <ng-container matColumnDef="userName">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('reviews','userName')">{{ 'admin.user' | translate }}<span class="sort-icon">{{ sortDir('reviews','userName') === 'asc' ? '↑' : sortDir('reviews','userName') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('reviews','userName')">{{ r.userName }}</td>
              </ng-container>
              <ng-container matColumnDef="rating">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('reviews','rating')">{{ 'admin.rating' | translate }}<span class="sort-icon">{{ sortDir('reviews','rating') === 'asc' ? '↑' : sortDir('reviews','rating') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('reviews','rating')">{{ r.rating }}/10</td>
              </ng-container>
              <ng-container matColumnDef="comment">
                <th mat-header-cell *matHeaderCellDef>{{ 'admin.comment' | translate }}</th>
                <td mat-cell *matCellDef="let r" class="comment-cell">
                  @if (r.comment.length <= 60) {
                    {{ r.comment }}
                  } @else {
                    <span class="comment-short">{{ expandedComments().has(r.id) ? r.comment : (r.comment | slice:0:60) + '…' }}</span>
                    <button class="comment-toggle" (click)="toggleComment(r.id)">
                      {{ expandedComments().has(r.id) ? ('admin.showLess' | translate) : ('admin.showMore' | translate) }}
                    </button>
                  }
                </td>
              </ng-container>
              <ng-container matColumnDef="createdUtc">
                <th mat-header-cell *matHeaderCellDef class="sortable" (click)="setSort('reviews','createdUtc')">{{ 'admin.date' | translate }}<span class="sort-icon">{{ sortDir('reviews','createdUtc') === 'asc' ? '↑' : sortDir('reviews','createdUtc') === 'desc' ? '↓' : '↕' }}</span></th>
                <td mat-cell *matCellDef="let r" [class.sort-col]="sortDir('reviews','createdUtc')">{{ r.createdUtc | localizedDate:'dd.MM.yyyy' }}</td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let r">
                  <button mat-icon-button color="warn" (click)="deleteReview(r.id)" [matTooltip]="'admin.delete' | translate">
                    <mat-icon>cancel</mat-icon>
                  </button>
                </td>
              </ng-container>
              <ng-container matColumnDef="empty">
                <td mat-footer-cell *matFooterCellDef [attr.colspan]="reviewColumns.length">
                  <span class="empty-state">{{ 'admin.noReviews' | translate }}</span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="reviewColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: reviewColumns;"></tr>
              <tr mat-footer-row *matFooterRowDef="['empty']" [class.empty-row-hidden]="pendingReviews().length > 0"></tr>
            </table>
          </div>
        </mat-tab>
      </mat-tab-group>
      </div>

      @if (showStaffDialog()) {
        <div class="dialog-overlay" (click)="closeStaffDialog()">
          <div class="dialog-content" (click)="$event.stopPropagation()">
            <button class="dialog-close" type="button" (click)="closeStaffDialog()" [attr.aria-label]="'admin.close' | translate" [matTooltip]="'admin.close' | translate">
              <mat-icon>close</mat-icon>
            </button>
            <h2>{{ 'admin.addStaff' | translate }}</h2>
            <form (ngSubmit)="saveStaff()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.email' | translate }}</mat-label>
                <input matInput [(ngModel)]="staffForm.email" name="staffEmail" required type="email">
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.password' | translate }}</mat-label>
                <input matInput [(ngModel)]="staffForm.password" name="staffPassword" required type="password">
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.firstName' | translate }}</mat-label>
                <input matInput [(ngModel)]="staffForm.firstName" name="staffFirstName" required>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.lastName' | translate }}</mat-label>
                <input matInput [(ngModel)]="staffForm.lastName" name="staffLastName" required>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.role' | translate }}</mat-label>
                <mat-select [(ngModel)]="staffForm.role" name="staffRole" required>
                  <mat-option value="Admin">{{ 'nav.admin' | translate }}</mat-option>
                  <mat-option value="Cashier">{{ 'nav.cashier' | translate }}</mat-option>
                </mat-select>
              </mat-form-field>
              <div class="dialog-actions">
                <button mat-button type="button" (click)="closeStaffDialog()">{{ 'admin.cancel' | translate }}</button>
                <button mat-flat-button color="primary" type="submit">{{ 'admin.save' | translate }}</button>
              </div>
            </form>
          </div>
        </div>
      }

      @if (showCinemaDialog()) {
        <div class="dialog-overlay" (click)="closeCinemaDialog()">
          <div class="dialog-content" (click)="$event.stopPropagation()">
            <button class="dialog-close" type="button" (click)="closeCinemaDialog()" [attr.aria-label]="'admin.close' | translate" [matTooltip]="'admin.close' | translate">
              <mat-icon>close</mat-icon>
            </button>
            <h2>{{ editingCinema() ? ('admin.editCinema' | translate) : ('admin.addCinema' | translate) }}</h2>
            <form (ngSubmit)="saveCinema()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.name' | translate }}</mat-label>
                <input matInput [(ngModel)]="cinemaForm.name" name="name" required>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.city' | translate }}</mat-label>
                <input matInput [(ngModel)]="cinemaForm.city" name="city" required>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.address' | translate }}</mat-label>
                <input matInput [(ngModel)]="cinemaForm.address" name="address" required>
              </mat-form-field>
              <div class="dialog-actions">
                <button mat-button type="button" (click)="closeCinemaDialog()">{{ 'admin.cancel' | translate }}</button>
                <button mat-flat-button color="primary" type="submit">{{ 'admin.save' | translate }}</button>
              </div>
            </form>
          </div>
        </div>
      }

      @if (showHallDialog()) {
        <div class="dialog-overlay" (click)="closeHallDialog()">
          <div class="dialog-content dialog-wide" (click)="$event.stopPropagation()">
            <button class="dialog-close" type="button" (click)="closeHallDialog()" [attr.aria-label]="'admin.close' | translate" [matTooltip]="'admin.close' | translate">
              <mat-icon>close</mat-icon>
            </button>
            <h2>{{ editingHall() ? ('admin.editHall' | translate) : ('admin.addHall' | translate) }}</h2>
            <form (ngSubmit)="saveHall()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.cinema' | translate }}</mat-label>
                <mat-select
                  [(ngModel)]="hallForm.cinemaBranchId"
                  name="cinemaBranchId"
                  required
                  [disabled]="editingHall() !== null"
                  (openedChange)="focusSelectSearch($event, hallCinemaSearchInput)">
                  <div class="select-search" (click)="$event.stopPropagation()" (keydown)="$event.stopPropagation()">
                    <mat-icon>search</mat-icon>
                    <input
                      #hallCinemaSearchInput
                      [(ngModel)]="hallCinemaSearch"
                      [ngModelOptions]="{ standalone: true }"
                      [placeholder]="'admin.searchCinema' | translate"
                      autocomplete="off"
                      (keydown.enter)="$event.preventDefault()">
                  </div>
                  @for (c of filteredDialogCinemas(); track c.id) {
                    <mat-option [value]="c.id">{{ c.name }}</mat-option>
                  }
                  @if (filteredDialogCinemas().length === 0) {
                    <mat-option disabled>{{ 'admin.noSearchResults' | translate }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.hallName' | translate }}</mat-label>
                <input matInput [(ngModel)]="hallForm.name" name="name" required>
              </mat-form-field>
              <div class="layout-section">
                <h3>{{ 'admin.seatLayout' | translate }}</h3>
                <div class="layout-controls">
                  <mat-form-field appearance="outline">
                    <mat-label>{{ 'admin.rows' | translate }}</mat-label>
                    <input matInput type="number" [(ngModel)]="hallForm.rows" name="rows" min="1" max="20" (change)="regenerateLayout()">
                  </mat-form-field>
                  <mat-form-field appearance="outline">
                    <mat-label>{{ 'admin.cols' | translate }}</mat-label>
                    <input matInput type="number" [(ngModel)]="hallForm.cols" name="cols" min="1" max="20" (change)="regenerateLayout()">
                  </mat-form-field>
                </div>
                <div class="type-palette">
                  <span class="palette-label">{{ 'admin.seatLayout' | translate }}:</span>
                  <button type="button" class="type-btn type-standard"
                    [class.active]="activeSeatType === 1"
                    (click)="setSeatType(1)">
                    <span class="type-dot"></span>{{ 'admin.standard' | translate }}
                  </button>
                  <button type="button" class="type-btn type-vip"
                    [class.active]="activeSeatType === 2"
                    (click)="setSeatType(2)">
                    <span class="type-dot"></span>{{ 'admin.vip' | translate }}
                  </button>
                  <button type="button" class="type-btn type-love"
                    [class.active]="activeSeatType === 3"
                    (click)="setSeatType(3)">
                    <span class="type-dot"></span>{{ 'admin.loveseat' | translate }}
                  </button>
                </div>
                <div class="seat-grid" (mouseleave)="stopPainting()" (touchend)="stopPainting()">
                  <div class="screen-wrap">
                    <div class="screen-bar"></div>
                    <span class="screen-txt">{{ 'booking.screen' | translate }}</span>
                    <div class="screen-glow"></div>
                  </div>
                  @for (row of hallForm.layout; track $index; let rowIdx = $index) {
                    <div class="seat-row">
                      <span class="row-label">{{ rowIdx + 1 }}</span>
                      @for (seat of row; track $index; let colIdx = $index) {
                        <button
                          type="button"
                          [class]="getSeatClass(seat)"
                          [attr.data-row]="rowIdx"
                          [attr.data-col]="colIdx"
                          [title]="(rowIdx + 1) + ',' + (colIdx + 1)"
                          (mousedown)="onSeatMouseDown(rowIdx, colIdx)"
                          (mouseenter)="onSeatMouseEnter(rowIdx, colIdx)"
                          (touchend)="stopPainting()">
                          {{ colIdx + 1 }}
                        </button>
                      }
                    </div>
                  }
                </div>
              </div>
              <div class="dialog-actions">
                <button mat-button type="button" (click)="closeHallDialog()">{{ 'admin.cancel' | translate }}</button>
                <button mat-flat-button color="primary" type="submit">{{ 'admin.save' | translate }}</button>
              </div>
            </form>
          </div>
        </div>
      }

      @if (showMovieDialog()) {
        <div class="dialog-overlay" (click)="closeMovieDialog()">
          <div class="dialog-content" (click)="$event.stopPropagation()">
            <button class="dialog-close" type="button" (click)="closeMovieDialog()" [attr.aria-label]="'admin.close' | translate" [matTooltip]="'admin.close' | translate">
              <mat-icon>close</mat-icon>
            </button>
            <h2>{{ editingMovie() ? ('admin.editMovie' | translate) : ('admin.addMovie' | translate) }}</h2>
            <form (ngSubmit)="saveMovie()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.title_field' | translate }}</mat-label>
                <input matInput [(ngModel)]="movieForm.title" name="title" required>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.description' | translate }}</mat-label>
                <textarea matInput [(ngModel)]="movieForm.description" name="description" rows="3" required></textarea>
              </mat-form-field>
              <div class="form-row">
                <mat-form-field appearance="outline">
                  <mat-label>{{ 'admin.duration' | translate }}</mat-label>
                  <input matInput type="number" [(ngModel)]="movieForm.durationMinutes" name="durationMinutes" min="1" required>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>{{ 'admin.ageRating' | translate }}</mat-label>
                  <mat-select [(ngModel)]="movieForm.ageRating" name="ageRating" required>
                    <mat-option value="G">G</mat-option>
                    <mat-option value="PG">PG</mat-option>
                    <mat-option value="PG-13">PG-13</mat-option>
                    <mat-option value="R">R</mat-option>
                    <mat-option value="NC-17">NC-17</mat-option>
                  </mat-select>
                </mat-form-field>
              </div>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.releaseDate' | translate }}</mat-label>
                <input matInput [matDatepicker]="releasePicker" [(ngModel)]="movieForm.releaseDate" name="releaseDate" required>
                <mat-datepicker-toggle matSuffix [for]="releasePicker"></mat-datepicker-toggle>
                <mat-datepicker #releasePicker></mat-datepicker>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.posterUrl' | translate }}</mat-label>
                <input matInput [(ngModel)]="movieForm.posterUrl" name="posterUrl">
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.trailerUrl' | translate }}</mat-label>
                <input matInput [(ngModel)]="movieForm.trailerUrl" name="trailerUrl">
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.genres' | translate }}</mat-label>
                <mat-select [(ngModel)]="movieGenreIds" name="genreIds" multiple>
                  @for (g of genres; track g.id) {
                    <mat-option [value]="g.id">{{ g.name }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <div class="dialog-actions">
                <button mat-button type="button" (click)="closeMovieDialog()">{{ 'admin.cancel' | translate }}</button>
                <button mat-flat-button color="primary" type="submit">{{ 'admin.save' | translate }}</button>
              </div>
            </form>
          </div>
        </div>
      }

      @if (showShowtimeDialog()) {
        <div class="dialog-overlay" (click)="closeShowtimeDialog()">
          <div class="dialog-content" (click)="$event.stopPropagation()">
            <button class="dialog-close" type="button" (click)="closeShowtimeDialog()" [attr.aria-label]="'admin.close' | translate" [matTooltip]="'admin.close' | translate">
              <mat-icon>close</mat-icon>
            </button>
            <h2>{{ editingShowtime() ? ('admin.editShowtime' | translate) : ('admin.addShowtime' | translate) }}</h2>
            <form (ngSubmit)="saveShowtime()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.movie' | translate }}</mat-label>
                <mat-select
                  [(ngModel)]="showtimeForm.movieId"
                  name="movieId"
                  required
                  (openedChange)="focusSelectSearch($event, showtimeMovieSearchInput)">
                  <div class="select-search" (click)="$event.stopPropagation()" (keydown)="$event.stopPropagation()">
                    <mat-icon>search</mat-icon>
                    <input
                      #showtimeMovieSearchInput
                      [(ngModel)]="showtimeMovieSearch"
                      [ngModelOptions]="{ standalone: true }"
                      [placeholder]="'admin.searchMovie' | translate"
                      autocomplete="off"
                      (keydown.enter)="$event.preventDefault()">
                  </div>
                  @for (m of filteredDialogMovies(); track m.id) {
                    <mat-option [value]="m.id">{{ m.title }}</mat-option>
                  }
                  @if (filteredDialogMovies().length === 0) {
                    <mat-option disabled>{{ 'admin.noSearchResults' | translate }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.hall' | translate }}</mat-label>
                <mat-select
                  [(ngModel)]="showtimeForm.hallId"
                  name="hallId"
                  required
                  (openedChange)="focusSelectSearch($event, showtimeHallSearchInput)">
                  <div class="select-search" (click)="$event.stopPropagation()" (keydown)="$event.stopPropagation()">
                    <mat-icon>search</mat-icon>
                    <input
                      #showtimeHallSearchInput
                      [(ngModel)]="showtimeHallSearch"
                      [ngModelOptions]="{ standalone: true }"
                      [placeholder]="'admin.searchHall' | translate"
                      autocomplete="off"
                      (keydown.enter)="$event.preventDefault()">
                  </div>
                  @for (h of filteredDialogHalls(); track h.id) {
                    <mat-option [value]="h.id">{{ h.hallName }} ({{ h.cinemaName }})</mat-option>
                  }
                  @if (filteredDialogHalls().length === 0) {
                    <mat-option disabled>{{ 'admin.noSearchResults' | translate }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.date' | translate }}</mat-label>
                <input matInput [matDatepicker]="datePicker" [(ngModel)]="showtimeDate" name="date" required>
                <mat-datepicker-toggle matIconSuffix [for]="datePicker"></mat-datepicker-toggle>
                <mat-datepicker #datePicker></mat-datepicker>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'validation.time' | translate }}</mat-label>
                <input
                  matInput
                  readonly
                  [(ngModel)]="showtimeTime"
                  [ngxTimepicker]="showtimePicker"
                  name="time"
                  required>
                <mat-icon matSuffix (click)="showtimePicker.open()">schedule</mat-icon>
              </mat-form-field>
              <ngx-material-timepicker
                #showtimePicker
                [format]="24"
                [minutesGap]="1"
                [theme]="timepickerTheme"
                timepickerClass="showtime-minute-picker"
                (opened)="startMinuteFaceStyling()"
                (closed)="stopMinuteFaceStyling()">
              </ngx-material-timepicker>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.format' | translate }}</mat-label>
                <mat-select [(ngModel)]="showtimeForm.format" name="format" required>
                  <mat-option value="TwoD">2D</mat-option>
                  <mat-option value="ThreeD">3D</mat-option>
                  <mat-option value="Imax">IMAX</mat-option>
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.price' | translate }}</mat-label>
                <input matInput type="number" [(ngModel)]="showtimeForm.basePrice" name="basePrice" min="1" required>
              </mat-form-field>
              @if (showtimeError()) {
                <div class="error-message">{{ showtimeError() | translate }}</div>
              }
              <div class="dialog-actions">
                <button mat-button type="button" (click)="closeShowtimeDialog()">{{ 'admin.cancel' | translate }}</button>
                <button mat-flat-button color="primary" type="submit" [disabled]="showtimeLoading()">
                  @if (showtimeLoading()) {
                    <span class="saving-label">…</span>
                  } @else {
                    {{ 'admin.save' | translate }}
                  }
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      @if (showPromoDialog()) {
        <div class="dialog-overlay" (click)="closePromoDialog()">
          <div class="dialog-content" (click)="$event.stopPropagation()">
            <button class="dialog-close" type="button" (click)="closePromoDialog()" [attr.aria-label]="'admin.close' | translate" [matTooltip]="'admin.close' | translate">
              <mat-icon>close</mat-icon>
            </button>
            <h2>{{ editingPromo() ? ('admin.editPromo' | translate) : ('admin.addPromo' | translate) }}</h2>
            <form (ngSubmit)="savePromo()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.code' | translate }}</mat-label>
                <input matInput [(ngModel)]="promoForm.code" name="code" required [attr.disabled]="editingPromo() !== null ? true : null">
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.discountType' | translate }}</mat-label>
                <mat-select [(ngModel)]="promoForm.discountType" name="discountType" required>
                  <mat-option value="Percent">{{ 'admin.percent' | translate }}</mat-option>
                  <mat-option value="Fixed">{{ 'admin.fixed' | translate }}</mat-option>
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.discountValue' | translate }}</mat-label>
                <input matInput type="number" [(ngModel)]="promoForm.value" name="value" min="1" [max]="promoForm.discountType === 'Percent' ? 100 : 10000" required>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.validFrom' | translate }}</mat-label>
                <input matInput [matDatepicker]="promoFromPicker" [(ngModel)]="promoForm.validFrom" name="validFrom" required>
                <mat-datepicker-toggle matIconSuffix [for]="promoFromPicker"></mat-datepicker-toggle>
                <mat-datepicker #promoFromPicker></mat-datepicker>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.validTo' | translate }}</mat-label>
                <input matInput [matDatepicker]="promoToPicker" [(ngModel)]="promoForm.validTo" name="validTo" required>
                <mat-datepicker-toggle matIconSuffix [for]="promoToPicker"></mat-datepicker-toggle>
                <mat-datepicker #promoToPicker></mat-datepicker>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.usageLimit' | translate }} ({{ 'admin.unlimited' | translate }} = 0)</mat-label>
                <input matInput type="number" [(ngModel)]="promoForm.usageLimit" name="usageLimit" min="0">
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'admin.perUserLimit' | translate }}</mat-label>
                <input matInput type="number" [(ngModel)]="promoForm.perUserLimit" name="perUserLimit" min="1">
              </mat-form-field>
              <mat-checkbox [(ngModel)]="promoForm.isPersonal" name="isPersonal">{{ 'admin.personal' | translate }}</mat-checkbox>
              @if (promoForm.isPersonal) {
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>{{ 'admin.assignToUser' | translate }}</mat-label>
                  <mat-select [(ngModel)]="promoForm.ownerUserId" name="ownerUserId" [required]="promoForm.isPersonal">
                    <mat-option [value]="null">--</mat-option>
                    @for (u of users(); track u.id) {
                      <mat-option [value]="u.id">{{ u.email }} ({{ u.firstName }} {{ u.lastName }})</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              }
              <div class="dialog-actions">
                <button mat-button type="button" (click)="closePromoDialog()">{{ 'admin.cancel' | translate }}</button>
                <button mat-flat-button color="primary" type="submit">{{ 'admin.save' | translate }}</button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,  styles: [`
    .admin-page {
      min-height: var(--app-content-height);
      box-sizing: border-box;
      background: #070b13;
      padding: 24px 16px;
      font-family: 'DM Sans', sans-serif;
    }

    .admin-container {
      max-width: 1200px;
      margin: 0 auto;
    }

    .admin-header {
      margin-bottom: 28px;
    }

    .admin-title {
      margin: 0;
      font-size: 30px;
      font-weight: 800;
      font-family: 'Syne', sans-serif;
      color: #f8fafc;
      letter-spacing: -0.02em;
    }

    .admin-tabs {
      background: transparent;
    }

    ::ng-deep .mat-mdc-tab-labels {
      background: transparent;
      border-bottom: 1px solid rgba(255,255,255,0.08);
    }

    ::ng-deep .mat-mdc-tab .mdc-tab__content,
    ::ng-deep .mat-mdc-tab .mdc-tab__text-label,
    ::ng-deep .mat-mdc-tab span,
    ::ng-deep .mat-mdc-tab div[role="tab"] {
      font-family: 'DM Sans', sans-serif !important;
      font-weight: 500 !important;
      font-size: 14px !important;
      color: #a1a1aa !important;
      opacity: 1 !important;
      transition: color 0.2s ease !important;
      background: transparent !important;
    }

    ::ng-deep .mat-mdc-tab:hover:not(.mdc-tab--active) .mdc-tab__text-label,
    ::ng-deep .mat-mdc-tab:hover:not(.mdc-tab--active) span,
    ::ng-deep .mat-mdc-tab:hover:not(.mdc-tab--active) div[role="tab"] {
      color: #e5e5e5 !important;
      background: transparent !important;
    }

    ::ng-deep .mat-mdc-tab.mdc-tab--active .mdc-tab__content,
    ::ng-deep .mat-mdc-tab.mdc-tab--active .mdc-tab__text-label,
    ::ng-deep .mat-mdc-tab.mdc-tab--active span,
    ::ng-deep .mat-mdc-tab.mdc-tab--active div[role="tab"] {
      color: #ffffff !important;
      font-weight: 600 !important;
      text-shadow: 0 0 12px rgba(165, 180, 252, 0.5);
    }

    /* Legacy selector backup */
    ::ng-deep .mat-mdc-tab {
      font-family: 'DM Sans', sans-serif !important;
      font-weight: 500 !important;
      font-size: 14px !important;
      color: #a1a1aa !important;
      opacity: 1 !important;
      transition: color 0.2s ease !important;
      background: transparent !important;
    }

    ::ng-deep .mat-mdc-tab:hover:not(.mdc-tab--active) {
      color: #e5e5e5 !important;
      background: transparent !important;
    }

    ::ng-deep .mat-mdc-tab.mdc-tab--active {
      color: #ffffff !important;
      font-weight: 600 !important;
      text-shadow: 0 0 12px rgba(165, 180, 252, 0.5);
    }

    ::ng-deep .mdc-tab-indicator__content--underline {
      border-color: #6366f1 !important;
    }

    ::ng-deep .mat-mdc-tab-body-content {
      padding: 24px 0;
    }

    .tab-content {
      animation: fadeIn 300ms ease-out;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(8px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .toolbar {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      margin-bottom: 20px;
      align-items: center;
    }

    .toolbar ::ng-deep .mat-mdc-unelevated-button,
    .toolbar ::ng-deep .mat-mdc-outlined-button {
      height: 50px;
      padding: 0 20px;
      border-radius: 8px;
    }

    .toolbar .report-generate-btn,
    .toolbar .tmdb-import-btn {
      height: 50px;
    }


    .filter-field {
      min-width: 160px;
      --mdc-outlined-text-field-label-text-color: #64748b;
      --mdc-outlined-text-field-input-text-color: #e2e8f0;
      --mdc-outlined-text-field-outline-color: rgba(255,255,255,0.1);
      --mdc-outlined-text-field-hover-outline-color: rgba(255,255,255,0.2);
      --mdc-outlined-text-field-focus-outline-color: #6366f1;
      --mdc-outlined-text-field-focus-label-text-color: #818cf8;
      --mdc-outlined-text-field-container-shape: 8px;
      --mat-select-enabled-arrow-color: #64748b;
      --mat-select-enabled-trigger-text-color: #e2e8f0;
      --mat-select-placeholder-text-color: #64748b;
      --mat-form-field-container-height: 50px;
    }

    .filter-field ::ng-deep .mat-mdc-form-field-infix {
      min-height: 50px;
      padding-top: 13px;
      padding-bottom: 13px;
    }

    .filter-field ::ng-deep .mat-mdc-select-trigger {
      height: 24px;
      align-items: center;
    }

    ::ng-deep .mat-mdc-button,
    ::ng-deep .mat-mdc-outlined-button,
    ::ng-deep .mat-mdc-unelevated-button {
      font-family: 'DM Sans', sans-serif !important;
      font-weight: 500 !important;
    }

    ::ng-deep .mat-mdc-unelevated-button.mat-primary {
      background: #6366f1 !important;
    }

    ::ng-deep .mat-mdc-unelevated-button.mat-primary:hover {
      background: #4f46e5 !important;
    }

    ::ng-deep .mat-mdc-icon-button {
      color: #64748b !important;
    }

    ::ng-deep .mat-mdc-icon-button:hover {
      color: #e2e8f0 !important;
    }

    ::ng-deep .mat-mdc-icon-button.mat-warn {
      color: #ef4444 !important;
    }

    .admin-table {
      width: 100%;
      background: transparent !important;
    }

    .sortable {
      cursor: pointer;
      user-select: none;
      white-space: nowrap;
    }
    .sortable:hover { color: #a5b4fc; }

    .sort-icon {
      margin-left: 5px;
      font-size: 11px;
      opacity: 0.45;
      vertical-align: middle;
    }
    .sortable:hover .sort-icon { opacity: 0.8; }
    th.sortable .sort-icon:not(:empty) { opacity: 0.7; }

    ::ng-deep td.sort-col {
      background: rgba(99, 102, 241, 0.07) !important;
    }

    ::ng-deep .mat-mdc-table {
      background: transparent !important;
    }

    ::ng-deep .mat-mdc-header-row {
      background: rgba(255,255,255,0.02) !important;
    }

    ::ng-deep .mat-mdc-header-cell {
      color: #9ca3af !important;
      font-family: 'DM Sans', sans-serif !important;
      font-weight: 500 !important;
      font-size: 11px !important;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      background: transparent !important;
      padding: 14px 16px !important;
    }

    ::ng-deep .mat-mdc-cell {
      color: #e2e8f0 !important;
      font-family: 'DM Sans', sans-serif !important;
      font-size: 14px !important;
      background: transparent !important;
      border-bottom-color: rgba(255,255,255,0.08) !important;
      padding: 14px 16px !important;
    }

    ::ng-deep .mat-mdc-row {
      background: transparent !important;
    }

    ::ng-deep .mat-mdc-row:hover {
      background: rgba(99, 102, 241, 0.05) !important;
    }

    .age-badge {
      background: rgba(99, 102, 241, 0.15);
      color: #818cf8;
      padding: 4px 10px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 600;
    }

    .tmdb-import-btn {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      padding: 0 20px;
      height: 42px;
      border-radius: 10px;
      border: 1px solid rgba(251, 191, 36, 0.35);
      background: linear-gradient(135deg, rgba(251,191,36,0.12), rgba(245,158,11,0.08));
      color: #fbbf24;
      font-family: 'DM Sans', sans-serif;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.18s, border-color 0.18s, transform 0.18s, box-shadow 0.18s;
    }
    .tmdb-import-btn mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }
    .tmdb-import-btn:hover {
      background: linear-gradient(135deg, rgba(251,191,36,0.22), rgba(245,158,11,0.16));
      border-color: rgba(251,191,36,0.6);
      box-shadow: 0 0 12px rgba(251,191,36,0.2);
      transform: translateY(-1px);
    }

    .dialog-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.7);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      animation: fadeIn 200ms ease-out;
    }

    .dialog-content {
      position: relative;
      background: #1e293b;
      border-radius: 16px;
      padding: 28px;
      width: 100%;
      max-width: 480px;
      max-height: 90vh;
      overflow-y: auto;
    }

    .dialog-close {
      position: absolute;
      top: 16px;
      right: 16px;
      z-index: 2;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      padding: 0;
      border: 1px solid rgba(148, 163, 184, 0.22);
      border-radius: 10px;
      background: rgba(15, 23, 42, 0.45);
      color: #94a3b8;
      cursor: pointer;
      transition: color 150ms ease, background 150ms ease, border-color 150ms ease, transform 150ms ease, box-shadow 150ms ease;
    }

    .dialog-close mat-icon {
      width: 20px;
      height: 20px;
      font-size: 20px;
    }

    .dialog-close:hover {
      color: #fff;
      background: rgba(99, 102, 241, 0.2);
      border-color: rgba(129, 140, 248, 0.65);
      box-shadow: 0 0 14px rgba(99, 102, 241, 0.2);
      transform: translateY(-1px);
    }

    .dialog-close:focus-visible {
      outline: 2px solid #818cf8;
      outline-offset: 2px;
    }

    .dialog-wide {
      max-width: 640px;
    }

    .dialog-content h2 {
      color: #f8fafc;
      font-family: 'Syne', sans-serif;
      font-size: 20px;
      font-weight: 700;
      margin: 0 48px 24px 0;
    }

    .full-width {
      width: 100%;
    }

    .form-row {
      display: flex;
      gap: 16px;
    }

    .form-row mat-form-field {
      flex: 1;
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      margin-top: 24px;
      padding-top: 20px;
      border-top: 1px solid rgba(255,255,255,0.1);
    }

    ::ng-deep .mat-mdc-form-field {
      --mdc-filled-text-field-container-color: transparent !important;
      --mdc-filled-text-field-container-opacity: 0.05 !important;
      --mdc-filled-text-field-label-text-color: #94a3b8 !important;
      --mdc-filled-text-field-label-text-color: #94a3b8 !important;
      --mdc-filled-text-field-input-text-color: #ffffff !important;
      --mdc-outlined-text-field-outline-color: rgba(255,255,255,0.25) !important;
      --mdc-outlined-text-field-hover-outline-color: rgba(255,255,255,0.4) !important;
      --mdc-outlined-text-field-focus-outline-color: #818cf8 !important;
      --mdc-outlined-text-field-focus-label-text-color: #818cf8 !important;
      --mdc-filled-text-field-focus-label-text-color: #818cf8 !important;
      --mat-form-field-focus-select-arrow-color: #818cf8 !important;
      --mat-form-field-enabled-select-arrow-color: #94a3b8 !important;
    }

    ::ng-deep .mat-mdc-text-field-wrapper {
      background-color: transparent !important;
    }

    ::ng-deep .mat-mdc-form-field input::placeholder,
    ::ng-deep .mat-mdc-text-field-wrapper input::placeholder {
      color: #9ca3af !important;
      opacity: 1 !important;
    }

    ::ng-deep .mat-mdc-form-field input,
    ::ng-deep .mat-mdc-form-field .mat-mdc-input-element,
    ::ng-deep .mat-mdc-text-field-wrapper input,
    ::ng-deep .mat-mdc-text-field-wrapper textarea {
      color: #ffffff !important;
      caret-color: #818cf8 !important;
    }

    ::ng-deep .mat-mdc-form-field .mat-mdc-floating-label,
    ::ng-deep .mat-mdc-floating-label {
      color: #94a3b8 !important;
    }

    ::ng-deep .mat-mdc-form-field.mat-focused .mat-mdc-floating-label {
      color: #818cf8 !important;
    }

    ::ng-deep .mdc-text-field--outlined:not(.mdc-text-field--disabled):not(.mdc-text-field--focused):hover .mat-mdc-notched-outline-leading,
    ::ng-deep .mdc-text-field--outlined:not(.mdc-text-field--disabled):not(.mdc-text-field--focused):hover .mat-mdc-notched-outline-trailing,
    ::ng-deep .mdc-text-field--outlined:not(.mdc-text-field--disabled):not(.mdc-text-field--focused):hover .mat-mdc-notched-outline-notch {
      border-color: rgba(255,255,255,0.4) !important;
    }

    ::ng-deep .mdc-text-field--outlined .mat-mdc-notched-outline-leading,
    ::ng-deep .mdc-text-field--outlined .mat-mdc-notched-outline-trailing,
    ::ng-deep .mdc-text-field--outlined .mat-mdc-notched-outline-notch {
      border-color: rgba(255,255,255,0.25) !important;
    }

    ::ng-deep .mdc-text-field--outlined.mat-focused .mat-mdc-notched-outline-leading,
    ::ng-deep .mdc-text-field--outlined.mat-focused .mat-mdc-notched-outline-trailing,
    ::ng-deep .mdc-text-field--outlined.mat-focused .mat-mdc-notched-outline-notch {
      border-color: #818cf8 !important;
    }

    ::ng-deep .mat-mdc-select-value-text {
      color: #ffffff !important;
    }

    ::ng-deep .mat-mdc-button.mat-button,
    ::ng-deep .mat-mdc-outlined-button.mat-button,
    button.mat-button {
      color: #e5e5e5 !important;
      border-color: rgba(255,255,255,0.3) !important;
      background: rgba(255,255,255,0.08) !important;
    }

    ::ng-deep .mat-mdc-button.mat-button:hover,
    ::ng-deep .mat-mdc-outlined-button.mat-button:hover {
      color: #ffffff !important;
      border-color: rgba(255,255,255,0.5) !important;
      background: rgba(255,255,255,0.12) !important;
    }

    ::ng-deep .mat-mdc-select-panel {
      background: #1e293b !important;
    }

    ::ng-deep .mat-mdc-select-panel .select-search {
      position: sticky;
      top: 0;
      z-index: 1;
      display: flex;
      align-items: center;
      gap: 10px;
      margin: 8px;
      padding: 10px 12px;
      border: 1px solid rgba(255,255,255,0.2);
      border-radius: 8px;
      background: #172033;
      color: #94a3b8;
    }

    ::ng-deep .mat-mdc-select-panel .select-search mat-icon {
      width: 20px;
      height: 20px;
      font-size: 20px;
    }

    ::ng-deep .mat-mdc-select-panel .select-search input {
      width: 100%;
      min-width: 0;
      border: 0;
      outline: 0;
      background: transparent;
      color: #f8fafc;
      font: inherit;
    }

    ::ng-deep .mat-mdc-select-panel .select-search input::placeholder {
      color: #64748b;
    }

    ::ng-deep .mat-mdc-option {
      color: #e2e8f0 !important;
      font-family: 'DM Sans', sans-serif;
    }

    ::ng-deep .mat-mdc-option:hover:not(.mdc-list-item--disabled) {
      background: rgba(99, 102, 241, 0.1) !important;
    }

    ::ng-deep .mat-mdc-option.mdc-list-item--selected {
      background: rgba(99, 102, 241, 0.2) !important;
    }

    .layout-section {
      margin: 24px 0;
      padding: 20px;
      background: rgba(255,255,255,0.02);
      border-radius: 12px;
    }

    .layout-section h3 {
      color: #94a3b8;
      font-family: 'DM Sans', sans-serif;
      font-size: 14px;
      font-weight: 600;
      margin-bottom: 16px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .layout-controls {
      display: flex;
      gap: 12px;
      margin-bottom: 16px;
    }

    .layout-controls mat-form-field {
      width: 100px;
    }

    .type-palette {
      display: flex;
      align-items: center;
      gap: 6px;
      margin-bottom: 14px;
      flex-wrap: wrap;
    }

    .palette-label {
      font-size: 11px;
      font-weight: 600;
      color: #475569;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      margin-right: 4px;
    }

    .type-btn {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 6px 12px;
      border-radius: 8px;
      border: 1.5px solid transparent;
      cursor: pointer;
      font-size: 13px;
      font-weight: 600;
      font-family: 'DM Sans', sans-serif;
      background: transparent;
      transition: all 150ms ease;
    }

    .type-dot {
      width: 10px;
      height: 10px;
      border-radius: 3px;
      display: inline-block;
    }

    .type-btn.type-standard { color: #6ee7a0; border-color: rgba(46,204,113,0.15); }
    .type-btn.type-standard .type-dot { background: linear-gradient(170deg, #2ecc71, #1a9e52); box-shadow: 0 0 6px rgba(34,197,94,0.5); }
    .type-btn.type-standard.active,
    .type-btn.type-standard:hover {
      background: rgba(46,204,113,0.12);
      border-color: #2ecc71;
      color: #a7f3c8;
    }

    .type-btn.type-vip { color: #fbbf24; border-color: rgba(251,191,36,0.15); }
    .type-btn.type-vip .type-dot { background: linear-gradient(170deg, #fbbf24, #d97706); box-shadow: 0 0 6px rgba(245,158,11,0.5); }
    .type-btn.type-vip.active,
    .type-btn.type-vip:hover {
      background: rgba(251,191,36,0.12);
      border-color: #fbbf24;
      color: #fde68a;
    }

    .type-btn.type-love { color: #fb7185; border-color: rgba(251,113,133,0.15); }
    .type-btn.type-love .type-dot { background: linear-gradient(170deg, #fb7185, #e0185e); box-shadow: 0 0 6px rgba(244,63,142,0.5); }
    .type-btn.type-love.active,
    .type-btn.type-love:hover {
      background: rgba(251,113,133,0.12);
      border-color: #fb7185;
      color: #fda4af;
    }

    .seat-grid {
      display: flex;
      flex-direction: column;
      gap: 5px;
      padding: 20px 16px 16px;
      background: rgba(0,0,0,0.3);
      border-radius: 12px;
      overflow-x: auto;
      align-items: center;
    }

    .screen-wrap {
      display: flex;
      flex-direction: column;
      align-items: center;
      width: 100%;
      margin-bottom: 16px;
      position: relative;
    }

    .screen-bar {
      width: min(360px, 92%);
      height: 8px;
      background: linear-gradient(90deg, transparent 0%, rgba(255,255,255,0.12) 12%, rgba(255,255,255,0.6) 38%, rgba(255,255,255,0.82) 50%, rgba(255,255,255,0.6) 62%, rgba(255,255,255,0.12) 88%, transparent 100%);
      border-radius: 50% 50% 0 0 / 100% 100% 0 0;
    }

    .screen-txt {
      font-size: 9px;
      font-weight: 700;
      letter-spacing: 0.3em;
      color: rgba(255,255,255,0.18);
      text-transform: uppercase;
      margin-top: 6px;
    }

    .screen-glow {
      width: min(240px, 65%);
      height: 40px;
      background: radial-gradient(ellipse at top, rgba(140,165,255,0.12) 0%, transparent 70%);
    }

    .seat-row {
      display: flex;
      gap: 5px;
      align-items: center;
    }

    .row-label {
      width: 20px;
      min-width: 20px;
      text-align: right;
      font-size: 10px;
      font-weight: 600;
      color: rgba(255,255,255,0.18);
      padding-right: 4px;
      font-family: 'DM Sans', sans-serif;
      font-variant-numeric: tabular-nums;
    }

    .seat-btn {
      width: 30px;
      height: 26px;
      border: none;
      border-radius: 7px 7px 4px 4px;
      cursor: crosshair;
      user-select: none;
      font-size: 11px;
      font-weight: 500;
      color: rgba(255,255,255,0.7);
      transition: transform 0.18s cubic-bezier(0.34, 1.56, 0.64, 1), box-shadow 0.15s ease, filter 0.15s ease;
      position: relative;
    }

    .seat-btn::after {
      content: '';
      position: absolute;
      top: 3px;
      left: 18%;
      right: 18%;
      height: 3px;
      background: rgba(255,255,255,0.22);
      border-radius: 2px;
    }

    .seat-btn:hover {
      transform: translateY(-3px) scale(1.1);
      filter: brightness(1.3);
    }

    .seat-btn:active {
      transform: translateY(-1px) scale(1.03);
    }

    .seat-btn.type-1 {
      background: linear-gradient(170deg, #2ecc71 0%, #1a9e52 100%);
      box-shadow: 0 3px 0 #0f7a35, 0 6px 14px rgba(34,197,94,0.18);
      color: rgba(255,255,255,0.85);
    }

    .seat-btn.type-2 {
      background: linear-gradient(170deg, #fbbf24 0%, #d97706 100%);
      box-shadow: 0 3px 0 #92520a, 0 6px 14px rgba(245,158,11,0.22);
      color: rgba(255,255,255,0.9);
    }

    .seat-btn.type-3 {
      background: linear-gradient(170deg, #fb7185 0%, #e0185e 100%);
      box-shadow: 0 3px 0 #981248, 0 6px 14px rgba(244,63,142,0.22);
      color: rgba(255,255,255,0.9);
    }

    .error-message {
      color: #f87171;
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.3);
      padding: 12px;
      border-radius: 8px;
      margin-bottom: 16px;
      font-size: 13px;
    }

    .chip {
      display: inline-flex;
      align-items: center;
      padding: 3px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
      letter-spacing: 0.02em;
    }

    .chip-personal {
      background: rgba(99, 102, 241, 0.15);
      color: #818cf8;
      border: 1px solid rgba(99, 102, 241, 0.25);
    }

    .chip-public {
      background: rgba(100, 116, 139, 0.15);
      color: #94a3b8;
      border: 1px solid rgba(100, 116, 139, 0.2);
    }

    .empty-state {
      display: block;
      text-align: center;
      padding: 36px 16px;
      color: #475569;
      font-size: 14px;
    }

    .role-badge {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
      &.role-admin { background: #fde68a; color: #92400e; }
      &.role-cashier { background: #bfdbfe; color: #1e3a5f; }
    }

    .comment-cell {
      max-width: 300px;
    }
    .review-movie-link {
      color: inherit;
      text-decoration: none;
      font-weight: 600;
      cursor: pointer;
    }
    .review-movie-link:hover {
      color: #a5b4fc;
      text-decoration: underline;
    }
    .comment-short {
      word-break: break-word;
      line-height: 1.4;
    }
    .comment-toggle {
      display: block;
      margin-top: 4px;
      padding: 0;
      background: none;
      border: none;
      color: #818cf8;
      font-size: 12px;
      cursor: pointer;
      &:hover { color: #a5b4fc; text-decoration: underline; }
    }

    ::ng-deep .mat-mdc-footer-row {
      background: transparent !important;
    }

    ::ng-deep .mat-mdc-footer-row.empty-row-hidden {
      display: none !important;
      height: 0 !important;
      min-height: 0 !important;
    }

    ::ng-deep .mat-mdc-footer-cell {
      border-bottom: none !important;
      background: transparent !important;
    }

    .saving-label {
      font-size: 18px;
      line-height: 1;
    }

    /* Custom Scrollbar - Dark Theme */
    ::-webkit-scrollbar {
      width: 8px;
      height: 8px;
    }

    ::-webkit-scrollbar-track {
      background: rgba(255, 255, 255, 0.02);
      border-radius: 4px;
    }

    ::-webkit-scrollbar-thumb {
      background: rgba(255, 255, 255, 0.15);
      border-radius: 4px;
    }

    ::-webkit-scrollbar-thumb:hover {
      background: rgba(255, 255, 255, 0.25);
    }

    ::-webkit-scrollbar-corner {
      background: transparent;
    }

    /* Date Picker Calendar Icon - Light theme */
    ::-webkit-calendar-picker-indicator {
      filter: invert(0.85);
      cursor: pointer;
      opacity: 0.7;
      transition: opacity 150ms ease;
    }

    ::-webkit-calendar-picker-indicator:hover {
      opacity: 1;
    }

    /* Time Picker Icon */
    .time-picker-icon {
      color: #94a3b8 !important;
      cursor: pointer;
      transition: color 150ms ease;
    }

    .time-picker-icon:hover {
      color: #e2e8f0 !important;
    }

    /* Ngx Material Timepicker Dark Theme */
    ::ng-deep .timepicker-dialog {
      background: #0f172a !important;
      z-index: 10000 !important;
    }

    ::ng-deep .timepicker__dialog-backdrop {
      z-index: 9999 !important;
    }

    ::ng-deep .timepicker__container {
      z-index: 10001 !important;
    }

    ::ng-deep .timepicker__dialog__header {
      background: linear-gradient(135deg, #1e293b, #0f172a) !important;
    }

    ::ng-deep .timepicker__dialog__header-date {
      color: #f8fafc !important;
    }

    ::ng-deep .timepicker-24h {
      color: #e2e8f0 !important;
    }

    ::ng-deep .timepicker__circle--active {
      background: #6366f1 !important;
      box-shadow: 0 0 12px rgba(99, 102, 241, 0.5) !important;
    }

    ::ng-deep .timepicker__pointer {
      background: #818cf8 !important;
    }

    ::ng-deep .timepicker--current,
    ::ng-deep .timepicker__active {
      color: #fff !important;
    }

    ::ng-deep .timepicker__day-body,
    ::ng-deep .timepicker__outer__circle {
      color: #e2e8f0 !important;
    }

    ::ng-deep .timepicker__day-body:hover,
    ::ng-deep .timepicker__outer__circle:hover {
      background: rgba(99, 102, 241, 0.2) !important;
    }

    ::ng-deep .timepicker__dialog-actions {
      display: flex;
      justify-content: space-between;
    }

    ::ng-deep .timepicker__dialog-action {
      color: #818cf8 !important;
      background: transparent !important;
      border: none !important;
      cursor: pointer !important;
      padding: 12px 24px !important;
      font-weight: 600 !important;
    }

    ::ng-deep .timepicker__dialog-action:hover {
      background: rgba(99, 102, 241, 0.2) !important;
    }

    /* Remove Angular Material Timepicker styles since we're using ngx */
    /* Angular Material Timepicker Popup Dark Theme */
    ::ng-deep .mat-timepicker-panel {
      background: #0f172a !important;
      border: 1px solid rgba(99, 102, 241, 0.3) !important;
      border-radius: 12px !important;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.5), 0 0 20px rgba(99, 102, 241, 0.15) !important;
    }

    ::ng-deep .mat-timepicker-table {
      font-family: 'DM Sans', sans-serif !important;
    }

    ::ng-deep .mat-timepicker-table-header {
      color: #64748b !important;
    }

    ::ng-deep .mat-timepicker-table-header th {
      color: #64748b !important;
      font-weight: 500 !important;
    }

    ::ng-deep .mat-timepicker-cell {
      color: #e2e8f0 !important;
      font-weight: 500 !important;
    }

    ::ng-deep .mat-timepicker-cell:hover:not(.mat-timepicker-cell-disabled) {
      background: rgba(99, 102, 241, 0.15) !important;
    }

    ::ng-deep .mat-timepicker-cell.mat-timepicker-cell-selected {
      background: linear-gradient(135deg, #6366f1, #4f46e5) !important;
      color: #fff !important;
      box-shadow: 0 0 12px rgba(99, 102, 241, 0.5) !important;
    }

    ::ng-deep .mat-timepicker-cell.mat-timepicker-cell-disabled {
      color: #475569 !important;
      opacity: 0.5;
    }

    ::ng-deep .mat-timepicker-period-scroller {
      color: #e2e8f0 !important;
    }

    ::ng-deep .mat-timepicker-period-button {
      color: #e2e8f0 !important;
    }

    ::ng-deep .mat-timepicker-period-button:hover {
      background: rgba(99, 102, 241, 0.1) !important;
      border-radius: 20px;
    }

    ::ng-deep .mat-timepicker-clock-hand {
      background: #6366f1 !important;
    }

    ::ng-deep .mat-timepicker-clock-hand::after {
      background: #6366f1 !important;
    }

    ::ng-deep .mat-timepicker-tick {
      background: #475569 !important;
    }

    ::ng-deep .mat-timepicker-tick.mat-timepicker-tick-selected {
      background: #818cf8 !important;
    }

    ::ng-deep .mat-timepicker-masked-input {
      color: #e2e8f0 !important;
    }

    /* Angular Material Datepicker Popup Dark Theme */
    ::ng-deep .mat-datepicker-content {
      background: #0f172a !important;
      border: 1px solid rgba(99, 102, 241, 0.3) !important;
      border-radius: 12px !important;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.5), 0 0 20px rgba(99, 102, 241, 0.15) !important;
    }

    ::ng-deep .mat-calendar {
      background: transparent !important;
      font-family: 'DM Sans', sans-serif !important;
    }

    ::ng-deep .mat-calendar-header {
      background: transparent !important;
    }

    ::ng-deep .mat-calendar-header-background {
      background: linear-gradient(135deg, #1e293b, #0f172a) !important;
    }

    ::ng-deep .mat-calendar-title {
      color: #f8fafc !important;
      font-family: 'Syne', sans-serif !important;
      font-weight: 600 !important;
    }

    ::ng-deep .mat-calendar-table-header th {
      color: #64748b !important;
      font-weight: 500 !important;
    }

    ::ng-deep .mat-calendar-body-cell-content {
      color: #e2e8f0 !important;
      font-weight: 500 !important;
    }

    ::ng-deep .mat-calendar-body-selected {
      background: linear-gradient(135deg, #6366f1, #4f46e5) !important;
      color: #fff !important;
      box-shadow: 0 0 12px rgba(99, 102, 241, 0.5) !important;
    }

    ::ng-deep .mat-calendar-body-today:not(.mat-calendar-body-selected) {
      border-color: #818cf8 !important;
      color: #818cf8 !important;
    }

    ::ng-deep .mat-calendar-body-hover:not(.mat-calendar-body-selected):not(.mat-calendar-body-today) {
      background: rgba(99, 102, 241, 0.15) !important;
    }

    ::ng-deep .mat-calendar-body-disabled > .mat-calendar-body-cell-content {
      color: #475569 !important;
      opacity: 0.5;
    }

    ::ng-deep .mat-calendar-arrow {
      fill: #94a3b8 !important;
    }

    ::ng-deep .mat-calendar-previous-button,
    ::ng-deep .mat-calendar-next-button {
      color: #94a3b8 !important;
    }

    ::ng-deep .mat-calendar-previous-button:hover,
    ::ng-deep .mat-calendar-next-button:hover {
      color: #e2e8f0 !important;
      background: rgba(99, 102, 241, 0.1) !important;
      border-radius: 50%;
    }

    ::ng-deep .mat-calendar-period-button {
      color: #f8fafc !important;
      font-weight: 600 !important;
    }

    ::ng-deep .mat-calendar-period-button:hover {
      background: rgba(99, 102, 241, 0.1) !important;
      border-radius: 20px;
    }

    ::ng-deep .mat-calendar-body-in-range::before {
      background: rgba(99, 102, 241, 0.2) !important;
    }

    ::ng-deep .mat-calendar-body-start-container,
    ::ng-deep .mat-calendar-body-end-container {
      background: rgba(99, 102, 241, 0.3) !important;
      border-radius: 50% 0 0 50%;
    }

    ::ng-deep .mat-calendar-body-end-container {
      border-radius: 0 50% 50% 0;
    }

    ::ng-deep .mat-calendar-body-selected {
      font-weight: 600;
    }

    /* Clock Time Picker */
    .time-picker-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 16px;
      padding: 20px;
      background: linear-gradient(145deg, rgba(30, 41, 59, 0.8), rgba(15, 23, 42, 0.9));
      border-radius: 16px;
      border: 1px solid rgba(99, 102, 241, 0.2);
    }

    .time-picker-label {
      font-family: 'DM Sans', sans-serif;
      font-size: 13px;
      font-weight: 500;
      color: #94a3b8;
      text-transform: uppercase;
      letter-spacing: 0.1em;
    }

    .clock-picker {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 16px;
    }

    .clock-face {
      position: relative;
      width: 140px;
      height: 140px;
      border-radius: 50%;
      background: radial-gradient(circle at 30% 30%, #334155, #0f172a);
      border: 2px solid rgba(99, 102, 241, 0.3);
      box-shadow: 
        0 0 20px rgba(99, 102, 241, 0.15),
        inset 0 0 30px rgba(0, 0, 0, 0.4);
    }

    .clock-center {
      position: absolute;
      top: 50%;
      left: 50%;
      width: 8px;
      height: 8px;
      background: #6366f1;
      border-radius: 50%;
      transform: translate(-50%, -50%);
      box-shadow: 0 0 8px rgba(99, 102, 241, 0.8);
    }

    .clock-hand {
      position: absolute;
      top: 50%;
      left: 50%;
      width: 3px;
      height: 45px;
      background: linear-gradient(to top, #6366f1, #818cf8);
      transform-origin: bottom center;
      border-radius: 2px;
      margin-left: -1.5px;
      margin-top: -45px;
      box-shadow: 0 0 8px rgba(99, 102, 241, 0.6);
    }

    .clock-hour {
      position: absolute;
      top: 50%;
      left: 50%;
      width: 24px;
      height: 24px;
      margin: -12px 0 0 -12px;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      transition: all 150ms ease;
    }

    .clock-hour span {
      font-family: 'Syne', sans-serif;
      font-size: 11px;
      font-weight: 600;
      color: #64748b;
      transition: all 150ms ease;
    }

    .clock-hour:hover span {
      color: #e2e8f0;
      transform: scale(1.15);
    }

    .clock-hour.selected span {
      color: #fff;
      text-shadow: 0 0 8px rgba(99, 102, 241, 0.8);
    }

    .clock-hour.selected::before {
      content: '';
      position: absolute;
      width: 20px;
      height: 20px;
      background: rgba(99, 102, 241, 0.3);
      border-radius: 50%;
      z-index: -1;
    }

    .clock-minute-dot {
      position: absolute;
      top: 50%;
      left: 50%;
      width: 6px;
      height: 6px;
      margin: -3px 0 0 -3px;
      background: #475569;
      border-radius: 50%;
      cursor: pointer;
      transition: all 150ms ease;
    }

    .clock-minute-dot:hover {
      background: #94a3b8;
      transform: scale(1.3);
    }

    .clock-minute-dot.selected {
      background: #818cf8;
      box-shadow: 0 0 6px rgba(99, 102, 241, 0.8);
    }

    .clock-am-pm {
      display: flex;
      gap: 8px;
    }

    .clock-am-pm button {
      padding: 8px 20px;
      border: 1px solid rgba(99, 102, 241, 0.3);
      border-radius: 8px;
      background: transparent;
      color: #64748b;
      font-family: 'DM Sans', sans-serif;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
      transition: all 150ms ease;
    }

    .clock-am-pm button:hover {
      border-color: rgba(99, 102, 241, 0.5);
      color: #e2e8f0;
    }

    .clock-am-pm button.active {
      background: linear-gradient(135deg, #6366f1, #4f46e5);
      border-color: transparent;
      color: #fff;
      box-shadow: 0 0 12px rgba(99, 102, 241, 0.4);
    }
  `],
})
export class AdminComponent implements OnInit, AfterViewInit, OnDestroy {
  selectedTabIndex = 0;
  activeTab = signal<AdminTab>('cinemas');

  cinemas = signal<CinemaAdminDto[]>([]);
  halls = signal<HallAdminDto[]>([]);
  movies = signal<MovieAdminDto[]>([]);
  showtimes = signal<ShowtimeAdminDto[]>([]);
  salesReport = signal<SalesReportItem[]>([]);
  occupancyReport = signal<OccupancyReportItem[]>([]);

  promoCodes = signal<PromoCodeDto[]>([]);
  users = signal<UserDto[]>([]);
  showPromoDialog = signal(false);

  staffUsers = signal<StaffUserDto[]>([]);
  pendingReviews = signal<AdminReviewDto[]>([]);
  expandedComments = signal<Set<number>>(new Set());
  toggleComment(id: number) {
    this.expandedComments.update(s => {
      const next = new Set(s);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  // ── Sort state per table ──
  private readonly sortState = signal<Record<string, { col: string; dir: 'asc' | 'desc' }>>({
    showtimes: { col: 'startUtc', dir: 'desc' },
  });

  private sorted<T>(key: string, rows: T[]): T[] {
    const s = this.sortState()[key];
    if (!s) return rows;
    return [...rows].sort((a, b) => {
      const av = (a as any)[s.col] ?? '';
      const bv = (b as any)[s.col] ?? '';
      const cmp = typeof av === 'number' && typeof bv === 'number'
        ? av - bv
        : String(av).localeCompare(String(bv));
      return s.dir === 'asc' ? cmp : -cmp;
    });
  }

  sortedCinemas    = computed(() => this.sorted('cinemas',    this.cinemas()));
  sortedHalls      = computed(() => this.sorted('halls',      this.halls()));
  sortedMovies     = computed(() => this.sorted('movies',     this.movies()));
  sortedShowtimes  = computed(() => this.sorted('showtimes',  this.showtimes()));
  sortedPromos     = computed(() => this.sorted('promos',     this.promoCodes()));
  sortedStaff      = computed(() => this.sorted('staff',      this.staffUsers()));
  sortedSales      = computed(() => this.sorted('sales',      this.salesReport()));
  sortedOccupancy  = computed(() => this.sorted('occupancy',  this.occupancyReport()));
  sortedReviews    = computed(() => this.sorted('reviews',    this.pendingReviews()));

  setSort(table: string, col: string): void {
    const cur = this.sortState()[table];
    const dir: 'asc' | 'desc' = cur?.col === col && cur.dir === 'asc' ? 'desc' : 'asc';
    this.sortState.update(s => ({ ...s, [table]: { col, dir } }));
  }

  sortDir(table: string, col: string): 'asc' | 'desc' | null {
    const s = this.sortState()[table];
    return s?.col === col ? s.dir : null;
  }
  showStaffDialog = signal(false);
  staffForm = { email: '', password: '', firstName: '', lastName: '', role: 'Cashier' as 'Admin' | 'Cashier' };
  staffColumns = ['email', 'firstName', 'lastName', 'role', 'actions'];
  reviewColumns = ['movieTitle', 'userName', 'rating', 'comment', 'createdUtc', 'actions'];
  editingPromo = signal<PromoCodeDto | null>(null);
  promoForm = {
    code: '',
    discountType: 'Percent' as 'Percent' | 'Fixed',
    value: 10,
    validFrom: new Date(),
    validTo: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
    usageLimit: 0,
    perUserLimit: 1,
    isPersonal: false,
    ownerUserId: null as string | null
  };
  promoColumns = ['code', 'discountType', 'validPeriod', 'usage', 'personal', 'actions'];

  hallCinemaFilter: number | null = null;
  showtimeCinemaFilter: number | null = null;
  hallFilterCinemaSearch = '';
  showtimeFilterCinemaSearch = '';

  reportStartDate = new Date();
  reportEndDate = new Date();

  showCinemaDialog = signal(false);
  editingCinema = signal<CinemaAdminDto | null>(null);
  cinemaForm = { name: '', city: '', address: '' };

  showHallDialog = signal(false);
  editingHall = signal<HallAdminDto | null>(null);
  hallForm = { cinemaBranchId: 0, name: '', rows: 5, cols: 8, layout: this.createLayout(5, 8) };
  hallCinemaSearch = '';

  showMovieDialog = signal(false);
  editingMovie = signal<MovieAdminDto | null>(null);
  movieForm = { title: '', description: '', durationMinutes: 120, ageRating: 'PG-13', releaseDate: new Date(), posterUrl: '', trailerUrl: '' };

  showShowtimeDialog = signal(false);
  editingShowtime = signal<ShowtimeAdminDto | null>(null);
  showtimeForm = { movieId: 0, hallId: 0, startUtc: '', format: 'TwoD', basePrice: 200 };
  showtimeDate = new Date();
  showtimeTime = '12:00';
  showtimeMovieSearch = '';
  showtimeHallSearch = '';
  hours = 12;
  minutes = 0;
  isAM = true;
  clockHours = [
    { value: 12, label: '12' }, { value: 1, label: '1' }, { value: 2, label: '2' },
    { value: 3, label: '3' }, { value: 4, label: '4' }, { value: 5, label: '5' },
    { value: 6, label: '6' }, { value: 7, label: '7' }, { value: 8, label: '8' },
    { value: 9, label: '9' }, { value: 10, label: '10' }, { value: 11, label: '11' }
  ];
  clockMinutes = [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55];
  showtimeLoading = signal(false);
  timepickerTheme: NgxMaterialTimepickerTheme = {
    container: { bodyBackgroundColor: '#0f172a', buttonColor: '#6366f1' }
  };
  private timepickerRef: any;
  private minuteFaceObserver?: MutationObserver;
  private _onTouchMove!: (e: TouchEvent) => void;
  private _onTouchStart!: (e: TouchEvent) => void;

  setTimepickerRef(ref: any) {
    this.timepickerRef = ref;
  }

  openTimepicker() {
    if (this.timepickerRef) {
      this.timepickerRef.open();
    }
  }

  startMinuteFaceStyling(): void {
    this.stopMinuteFaceStyling();
    this.minuteFaceObserver = new MutationObserver(() => this.styleMinuteFace());
    this.minuteFaceObserver.observe(document.body, { childList: true, subtree: true });
    setTimeout(() => this.styleMinuteFace());
  }

  stopMinuteFaceStyling(): void {
    this.minuteFaceObserver?.disconnect();
    this.minuteFaceObserver = undefined;
  }

  private styleMinuteFace(): void {
    const minuteLabels = document.querySelectorAll<HTMLElement>(
      '.showtime-minute-picker ngx-material-timepicker-minutes-face .clock-face__number > span'
    );

    minuteLabels.forEach(label => {
      if (label.dataset['minuteStyled']) return;

      const minute = Number(label.textContent?.trim());
      if (!Number.isInteger(minute)) return;

      label.dataset['minuteStyled'] = 'true';
      if (minute % 5 !== 0) {
        label.textContent = '';
        label.classList.add('minute-dot-label');
        label.setAttribute('aria-label', minute.toString().padStart(2, '0'));
      }
    });
  }

  setHours(h: number) {
    this.hours = h;
    this.updateShowtimeTime();
  }
  setMinutes(m: number) {
    this.minutes = m;
    this.updateShowtimeTime();
  }
  setAMPM(am: boolean) {
    this.isAM = am;
    this.updateShowtimeTime();
  }
  private updateShowtimeTime() {
    let h = this.hours;
    if (!this.isAM && h !== 12) h += 12;
    if (this.isAM && h === 12) h = 0;
    this.showtimeTime = `${h.toString().padStart(2, '0')}:${this.minutes.toString().padStart(2, '0')}`;
  }
  showtimeError = signal<string | null>(null);

  cinemaColumns = ['name', 'city', 'address', 'actions'];
  hallColumns = ['hallName', 'cinemaName', 'size', 'actions'];
  movieColumns = ['title', 'duration', 'ageRating', 'actions'];
  showtimeColumns = ['movieTitle', 'hallName', 'startUtc', 'format', 'price', 'actions'];
  salesColumns = ['date', 'totalBookings', 'totalRevenue'];
  occupancyColumns = ['hallName', 'movieTitle', 'date', 'occupancy'];

  genres: { id: number; name: string }[] = [];
  movieGenreIds: number[] = [];

  constructor(
    private adminSvc: AdminService,
    private cinemaSvc: CinemaService,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    private el: ElementRef,
    private dialog: MatDialog,
    private dateAdapter: DateAdapter<Date>,
  ) {
    this.dateAdapter.setLocale(this.dateLocale());
    this.langSubscription = this.translate.onLangChange.subscribe(({ lang }) =>
      this.dateAdapter.setLocale(this.dateLocale(lang)));
  }

  private readonly langSubscription: Subscription;

  private dateLocale(lang = this.translate.currentLang): string {
    return lang === 'uk' ? 'uk-UA' : 'en-US';
  }

  private showMessage(key: string) {
    this.snackBar.open(this.translate.instant(key), '', { duration: 2000 });
  }

  ngOnInit() {
    this.loadCinemas();
    this.cinemaSvc.getGenres().subscribe(g => this.genres = g);
  }

  ngAfterViewInit() {
    const el = this.el.nativeElement as HTMLElement;
    this._onTouchMove = (e: TouchEvent) => this.onSeatTouchMove(e);
    this._onTouchStart = (e: TouchEvent) => {
      const target = e.target as HTMLElement;
      const r = target.getAttribute('data-row');
      const c = target.getAttribute('data-col');
      if (r !== null && c !== null) {
        this.onSeatTouchStart(e, +r, +c);
      }
    };
    el.addEventListener('touchmove', this._onTouchMove, { passive: false });
    el.addEventListener('touchstart', this._onTouchStart, { passive: false });
  }

  ngOnDestroy() {
    const el = this.el.nativeElement as HTMLElement;
    el.removeEventListener('touchmove', this._onTouchMove);
    el.removeEventListener('touchstart', this._onTouchStart);
    this.langSubscription.unsubscribe();
  }

  onTabChange(index: number) {
    const tabs: AdminTab[] = ['cinemas', 'halls', 'movies', 'showtimes', 'promos', 'reports', 'staff', 'reviews'];
    this.activeTab.set(tabs[index]);
    switch (tabs[index]) {
      case 'cinemas': this.loadCinemas(); break;
      case 'halls': this.loadHalls(); break;
      case 'movies': this.loadMovies(); break;
      case 'showtimes': this.loadShowtimes(); break;
      case 'promos': this.loadPromos(); this.loadUsers(); break;
      case 'reports': break;
      case 'staff': this.loadStaff(); break;
      case 'reviews': this.loadPendingReviews(); break;
    }
  }

  loadPromos() { this.adminSvc.getPromoCodes().subscribe({ next: data => this.promoCodes.set(data), error: () => this.showMessage('admin.loadError') }); }
  loadUsers() { this.adminSvc.getUsers().subscribe({ next: data => this.users.set(data), error: () => {} }); }

  loadStaff() { this.adminSvc.getStaff().subscribe({ next: data => this.staffUsers.set(data), error: () => this.showMessage('admin.loadError') }); }
  openStaffDialog() {
    this.staffForm = { email: '', password: '', firstName: '', lastName: '', role: 'Cashier' };
    this.showStaffDialog.set(true);
  }
  closeStaffDialog() { this.showStaffDialog.set(false); }
  saveStaff() {
    if (!this.staffForm.email.trim() || !this.staffForm.password || !this.staffForm.firstName.trim() || !this.staffForm.lastName.trim()) {
      this.showMessage('validation.requiredFields');
      return;
    }
    const req: CreateStaffUserRequest = {
      email: this.staffForm.email.trim(),
      password: this.staffForm.password,
      firstName: this.staffForm.firstName.trim(),
      lastName: this.staffForm.lastName.trim(),
      role: this.staffForm.role
    };
    this.adminSvc.createStaff(req).subscribe({
      next: () => { this.closeStaffDialog(); this.loadStaff(); this.showMessage('admin.staffCreated'); },
      error: (err: any) => this.showMessage(err?.error?.error || 'admin.networkError')
    });
  }
  deleteStaff(id: string) {
    if (confirm(this.translate.instant('admin.confirmDelete'))) {
      this.adminSvc.deleteStaff(id).subscribe({
        next: () => { this.loadStaff(); this.showMessage('admin.staffDeleted'); },
        error: () => this.showMessage('admin.networkError')
      });
    }
  }

  loadPendingReviews() { this.adminSvc.getReviews().subscribe({ next: data => this.pendingReviews.set(data), error: () => this.showMessage('admin.loadError') }); }
  deleteReview(id: number) {
    if (confirm(this.translate.instant('admin.confirmDelete'))) {
      this.adminSvc.deleteReview(id).subscribe({
        next: () => { this.loadPendingReviews(); this.showMessage('admin.reviewDeleted'); },
        error: () => this.showMessage('admin.networkError')
      });
    }
  }

  loadCinemas() { this.adminSvc.getCinemas().subscribe({ next: data => this.cinemas.set(data), error: () => this.showMessage('admin.loadError') }); }
  loadHalls() {
    if (this.hallCinemaFilter) {
      this.adminSvc.getHallsByCinema(this.hallCinemaFilter).subscribe({ next: data => this.halls.set(data), error: () => this.showMessage('admin.loadError') });
    } else {
      this.adminSvc.getHalls().subscribe({ next: data => this.halls.set(data), error: () => this.showMessage('admin.loadError') });
    }
  }
  loadMovies() { this.adminSvc.getMovies().subscribe({ next: data => this.movies.set(data), error: () => this.showMessage('admin.loadError') }); }
  loadShowtimes() {
    if (this.showtimeCinemaFilter) {
      this.adminSvc.getShowtimesByCinema(this.showtimeCinemaFilter).subscribe({ next: data => this.showtimes.set(data), error: () => this.showMessage('admin.loadError') });
    } else {
      this.adminSvc.getShowtimes().subscribe({ next: data => this.showtimes.set(data), error: () => this.showMessage('admin.loadError') });
    }
  }
  loadReports() {
    const start = this.reportStartDate.toISOString().split('T')[0];
    const end = this.reportEndDate.toISOString().split('T')[0];
    this.adminSvc.getSalesReport(start, end).subscribe({
      next: data => this.salesReport.set(data),
      error: () => this.showMessage('admin.loadError')
    });
    this.adminSvc.getOccupancyReport(start, end).subscribe({
      next: data => { this.occupancyReport.set(data); this.showMessage('admin.reportGenerated'); },
      error: () => this.showMessage('admin.loadError')
    });
  }

  downloadReport(type: 'sales' | 'occupancy', format: 'pdf' | 'xlsx') {
    const start = this.reportStartDate.toISOString().split('T')[0];
    const end = this.reportEndDate.toISOString().split('T')[0];
    this.adminSvc.downloadReport(type, format, start, end).subscribe({
      next: blob => {
        const filename = `${type}-report-${start}--${end}.${format}`;
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = filename;
        a.click();
        setTimeout(() => URL.revokeObjectURL(a.href), 100);
      },
      error: () => this.showMessage('admin.downloadError'),
    });
  }

  formatShowtimeTime(utc: string): string {
    const date = new Date(utc);
    return date.toLocaleString(this.dateLocale(), { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
  }

  formatLabel(format: string): string {
    return ({ TwoD: '2D', ThreeD: '3D', Imax: 'IMAX' } as Record<string, string>)[format] ?? format;
  }

  filteredHalls(): HallAdminDto[] {
    if (!this.showtimeCinemaFilter) return this.halls();
    return this.halls().filter(h => h.cinemaBranchId === this.showtimeCinemaFilter);
  }

  filteredDialogCinemas(): CinemaAdminDto[] {
    const query = this.hallCinemaSearch.trim().toLocaleLowerCase();
    return query
      ? this.cinemas().filter(cinema =>
          cinema.id === this.hallForm.cinemaBranchId
          || cinema.name.toLocaleLowerCase().includes(query))
      : this.cinemas();
  }

  filteredHallFilterCinemas(): CinemaAdminDto[] {
    return this.filterCinemas(this.hallFilterCinemaSearch, this.hallCinemaFilter);
  }

  filteredShowtimeFilterCinemas(): CinemaAdminDto[] {
    return this.filterCinemas(this.showtimeFilterCinemaSearch, this.showtimeCinemaFilter);
  }

  private filterCinemas(queryValue: string, selectedId: number | null): CinemaAdminDto[] {
    const query = queryValue.trim().toLocaleLowerCase();
    return query
      ? this.cinemas().filter(cinema =>
          cinema.id === selectedId
          || cinema.name.toLocaleLowerCase().includes(query))
      : this.cinemas();
  }

  filteredDialogMovies(): MovieAdminDto[] {
    const query = this.showtimeMovieSearch.trim().toLocaleLowerCase();
    return query
      ? this.movies().filter(movie =>
          movie.id === this.showtimeForm.movieId
          || movie.title.toLocaleLowerCase().includes(query))
      : this.movies();
  }

  filteredDialogHalls(): HallAdminDto[] {
    const query = this.showtimeHallSearch.trim().toLocaleLowerCase();
    const halls = this.filteredHalls();
    return query
      ? halls.filter(hall =>
          hall.id === this.showtimeForm.hallId
          || hall.hallName.toLocaleLowerCase().includes(query)
          || hall.cinemaName.toLocaleLowerCase().includes(query))
      : halls;
  }

  focusSelectSearch(opened: boolean, input: HTMLInputElement): void {
    if (!opened) return;
    setTimeout(() => input.focus());
  }

  // Cinema
  openCinemaDialog(c?: CinemaAdminDto) {
    if (c) {
      this.editingCinema.set(c);
      this.cinemaForm = { name: c.name, city: c.city, address: c.address };
    } else {
      this.editingCinema.set(null);
      this.cinemaForm = { name: '', city: '', address: '' };
    }
    this.showCinemaDialog.set(true);
  }
  closeCinemaDialog() { this.showCinemaDialog.set(false); }
  saveCinema() {
    if (!this.cinemaForm.name.trim()) {
      this.showMessage('validation.nameRequired');
      return;
    }
    if (!this.cinemaForm.city.trim()) {
      this.showMessage('validation.cityRequired');
      return;
    }
    if (!this.cinemaForm.address.trim()) {
      this.showMessage('validation.addressRequired');
      return;
    }
    const isEdit = this.editingCinema() !== null;
    const obs = isEdit
      ? this.adminSvc.updateCinema(this.editingCinema()!.id, { name: this.cinemaForm.name, city: this.cinemaForm.city, address: this.cinemaForm.address })
      : this.adminSvc.createCinema({ name: this.cinemaForm.name, city: this.cinemaForm.city, address: this.cinemaForm.address });
    (obs as any).subscribe({
      next: () => {
        this.closeCinemaDialog();
        this.loadCinemas();
        this.showMessage(isEdit ? 'admin.cinemaUpdated' : 'admin.cinemaCreated');
      },
      error: () => this.showMessage('admin.networkError'),
    });
  }
  deleteCinema(id: number) {
    if (confirm(this.translate.instant('admin.confirmDelete'))) {
      this.adminSvc.deleteCinema(id).subscribe({
        next: () => { this.loadCinemas(); this.showMessage('admin.cinemaDeleted'); },
        error: () => this.showMessage('admin.networkError')
      });
    }
  }

  // Hall
  openHallDialog(h?: HallAdminDto) {
    if (this.cinemas().length === 0) this.loadCinemas();
    this.hallCinemaSearch = '';
    if (h) {
      this.editingHall.set(h);
      this.hallForm = { cinemaBranchId: h.cinemaBranchId, name: h.hallName, rows: h.rows, cols: h.cols, layout: JSON.parse(JSON.stringify(h.layout)) };
    } else {
      this.editingHall.set(null);
      this.hallForm = {
        cinemaBranchId: this.hallCinemaFilter ?? this.cinemas()[0]?.id ?? 0,
        name: '',
        rows: 5,
        cols: 8,
        layout: this.createLayout(5, 8)
      };
    }
    this.showHallDialog.set(true);
  }
  closeHallDialog() { this.showHallDialog.set(false); }
  createLayout(rows: number, cols: number): SeatTypeCode[][] {
    return Array.from({ length: rows }, () => Array(cols).fill(1));
  }
  regenerateLayout() {
    const rows = Math.min(20, Math.max(1, this.hallForm.rows));
    const cols = Math.min(20, Math.max(1, this.hallForm.cols));
    this.hallForm.layout = this.createLayout(rows, cols);
  }
  // Paintbrush state
  activeSeatType: SeatTypeCode = 1;
  isPainting = false;
  paintedCoords = new Set<string>();

  setSeatType(type: SeatTypeCode) {
    this.activeSeatType = type;
  }

  onSeatMouseDown(row: number, col: number) {
    this.isPainting = true;
    this.paintedCoords.clear();
    this.applySeatType(row, col);
  }

  onSeatMouseEnter(row: number, col: number) {
    if (!this.isPainting) return;
    this.applySeatType(row, col);
  }

  @HostListener('document:mouseup')
  stopPainting() {
    this.isPainting = false;
    this.paintedCoords.clear();
  }

  onSeatTouchStart(e: TouchEvent, row: number, col: number) {
    e.preventDefault();
    this.isPainting = true;
    this.paintedCoords.clear();
    this.applySeatType(row, col);
  }

  onSeatTouchMove(e: TouchEvent) {
    if (!this.isPainting) return;
    e.preventDefault();
    const touch = e.touches[0];
    if (!touch) return;
    const el = document.elementFromPoint(touch.clientX, touch.clientY) as HTMLElement | null;
    if (!el) return;
    const r = el.getAttribute('data-row');
    const c = el.getAttribute('data-col');
    if (r !== null && c !== null) {
      this.applySeatType(+r, +c);
    }
  }

  private applySeatType(row: number, col: number) {
    const key = `${row},${col}`;
    if (this.paintedCoords.has(key)) return;
    this.paintedCoords.add(key);
    const layout = this.hallForm.layout.map(r => [...r]);
    layout[row][col] = this.activeSeatType;
    this.hallForm.layout = layout;
  }

  getSeatClass(seat: SeatTypeCode): string {
    const map: Record<number, string> = { 1: 'seat-btn type-1', 2: 'seat-btn type-2', 3: 'seat-btn type-3' };
    return map[seat] ?? 'seat-btn type-1';
  }

saveHall() {
    if (!this.hallForm.name.trim()) {
      this.showMessage('validation.hallNameRequired');
      return;
    }
    if (!this.hallForm.cinemaBranchId) {
      this.showMessage('validation.cinemaRequired');
      return;
    }
    if (this.hallForm.rows < 1 || this.hallForm.cols < 1) {
      this.showMessage('validation.invalidSize');
      return;
    }
    const isEdit = this.editingHall() !== null;
    const data = { name: this.hallForm.name, rows: this.hallForm.rows, cols: this.hallForm.cols, layout: this.hallForm.layout };
    const obs = isEdit
      ? this.adminSvc.updateHall(this.editingHall()!.id, data)
      : this.adminSvc.createHall(this.hallForm);
    (obs as any).subscribe({
      next: () => { this.closeHallDialog(); this.loadHalls(); isEdit ? this.showMessage('admin.hallUpdated') : this.showMessage('admin.hallCreated') },
      error: () => this.showMessage('admin.networkError')
    });
  }
  deleteHall(id: number) {
    if (confirm(this.translate.instant('admin.confirmDelete'))) {
      this.adminSvc.deleteHall(id).subscribe({
        next: () => { this.loadHalls(); this.showMessage('admin.hallDeleted'); },
        error: () => this.showMessage('admin.networkError')
      });
    }
  }

  // Movie
  openMovieDialog(m?: MovieAdminDto) {
    if (m) {
      this.editingMovie.set(m);
      this.movieForm = { title: m.title, description: m.description, durationMinutes: m.durationMinutes, ageRating: m.ageRating, releaseDate: new Date(m.releaseDateUtc), posterUrl: m.posterUrl || '', trailerUrl: m.trailerUrl || '' };
      this.movieGenreIds = m.genreIds ? [...m.genreIds] : [];
    } else {
      this.editingMovie.set(null);
      this.movieForm = { title: '', description: '', durationMinutes: 120, ageRating: 'PG-13', releaseDate: new Date(), posterUrl: '', trailerUrl: '' };
      this.movieGenreIds = [];
    }
    this.showMovieDialog.set(true);
  }
  closeMovieDialog() { this.showMovieDialog.set(false); }

  openTmdbImport() {
    const ref = this.dialog.open(TmdbImportDialogComponent, { width: '520px' });
    ref.afterClosed().subscribe((detail: TmdbMovieDetail | undefined) => {
      if (!detail) return;
      // Reverse map ageRating enum name → display value used by the form
      const ratingDisplay: Record<string, string> = { G: 'G', PG: 'PG', PG13: 'PG-13', R: 'R', NC17: 'NC-17' };
      this.editingMovie.set(null);
      this.movieForm = {
        title: detail.title,
        description: detail.description,
        durationMinutes: detail.durationMinutes,
        ageRating: ratingDisplay[detail.ageRating] ?? 'PG-13',
        releaseDate: new Date(detail.releaseDateUtc),
        posterUrl: detail.posterUrl ?? '',
        trailerUrl: detail.trailerUrl ?? '',
      };
      if (detail.genres.length > 0) {
        this.adminSvc.ensureGenres(detail.genres).subscribe(ensured => {
          // Оновлюємо локальний список жанрів щоб відображати нові жанри в select
          ensured.forEach(eg => {
            if (!this.genres.some(g => g.id === eg.id)) this.genres.push(eg);
          });
          this.genres.sort((a, b) => a.name.localeCompare(b.name));
          this.movieGenreIds = ensured.map(g => g.id);
          this.showMovieDialog.set(true);
        });
      } else {
        this.movieGenreIds = [];
        this.showMovieDialog.set(true);
      }
    });
  }
  saveMovie() {
    if (!this.movieForm.title.trim()) {
      this.showMessage('validation.titleRequired');
      return;
    }
    if (this.movieForm.durationMinutes < 1) {
      this.showMessage('validation.durationRequired');
      return;
    }
    const releaseDate = new Date(this.movieForm.releaseDate);
    if (isNaN(releaseDate.getTime())) {
      this.showMessage('validation.invalidDate');
      return;
    }
    const ageRatingMap: Record<string, string> = { 'G': 'G', 'PG': 'PG', 'PG-13': 'PG13', 'R': 'R', 'NC-17': 'NC17' };
    const ageRating = ageRatingMap[this.movieForm.ageRating] || 'PG13';
    const isEdit = this.editingMovie() !== null;
    const data = { title: this.movieForm.title, description: this.movieForm.description, durationMinutes: this.movieForm.durationMinutes, ageRating, releaseDateUtc: releaseDate.toISOString(), genreIds: [...this.movieGenreIds], posterUrl: this.movieForm.posterUrl || undefined, trailerUrl: this.movieForm.trailerUrl || undefined };
    const obs = isEdit
      ? this.adminSvc.updateMovie(this.editingMovie()!.id, data)
      : this.adminSvc.createMovie(data);
    (obs as any).subscribe({
      next: () => { this.closeMovieDialog(); this.loadMovies(); isEdit ? this.showMessage('admin.movieUpdated') : this.showMessage('admin.movieCreated'); },
      error: (err: any) => this.showMessage(err?.error?.error || 'admin.networkError')
    });
  }
  deleteMovie(id: number) {
    if (confirm(this.translate.instant('admin.confirmDelete'))) {
      this.adminSvc.deleteMovie(id).subscribe({
        next: () => { this.loadMovies(); this.showMessage('admin.movieDeleted'); },
        error: () => this.showMessage('admin.networkError')
      });
    }
  }

  // Showtime
  openShowtimeDialog(s?: ShowtimeAdminDto) {
    if (this.movies().length === 0) this.loadMovies();
    if (this.halls().length === 0) this.loadHalls();
    this.showtimeMovieSearch = '';
    this.showtimeHallSearch = '';
    if (s) {
      this.editingShowtime.set(s);
      const date = new Date(s.startUtc);
      this.showtimeDate = date;
      this.showtimeTime = date.toLocaleTimeString(this.dateLocale(), { hour: '2-digit', minute: '2-digit', hour12: false });
      this.showtimeForm = { movieId: s.movieId, hallId: s.hallId, startUtc: '', format: s.format, basePrice: s.basePrice };
    } else {
      this.editingShowtime.set(null);
      this.showtimeDate = new Date();
      this.showtimeTime = '12:00';
      this.showtimeForm = { movieId: this.movies()[0]?.id || 0, hallId: 0, startUtc: '', format: 'TwoD', basePrice: 200 };
    }
    this.showtimeError.set(null);
    this.showShowtimeDialog.set(true);
  }
  closeShowtimeDialog() { this.showShowtimeDialog.set(false); }

  /** Парсить час у форматах "HH:mm" та "h:mm AM/PM" → [hours, minutes] */
  private parseTime(value: string): [number, number] {
    const ampm = value.match(/(\d{1,2}):(\d{2})\s*(AM|PM)/i);
    if (ampm) {
      let h = parseInt(ampm[1], 10);
      const m = parseInt(ampm[2], 10);
      if (ampm[3].toUpperCase() === 'AM' && h === 12) h = 0;
      if (ampm[3].toUpperCase() === 'PM' && h !== 12) h += 12;
      return [h, m];
    }
    const parts = value.split(':').map(Number);
    return [parts[0] ?? 0, parts[1] ?? 0];
  }

  async saveShowtime() {
    if (!this.showtimeForm.movieId) {
      this.showMessage('validation.movieRequired');
      return;
    }
    if (!this.showtimeForm.hallId) {
      this.showMessage('validation.hallRequired');
      return;
    }
    if (!this.showtimeDate || isNaN(this.showtimeDate.getTime())) {
      this.showMessage('validation.dateRequired');
      return;
    }
    if (!this.showtimeTime) {
      this.showMessage('validation.timeRequired');
      return;
    }
    if (this.showtimeForm.basePrice < 1) {
      this.showMessage('validation.priceRequired');
      return;
    }
    this.showtimeLoading.set(true);
    this.showtimeError.set(null);
    const combined = new Date(this.showtimeDate);
    const [hours, minutes] = this.parseTime(this.showtimeTime);
    combined.setHours(hours, minutes, 0, 0);
    const startUtc = combined.toISOString();
    const movie = this.movies().find(m => m.id === this.showtimeForm.movieId);
    const duration = movie?.durationMinutes || 120;
    const endUtc = new Date(new Date(startUtc).getTime() + duration * 60000).toISOString();
    try {
      const conflict = await lastValueFrom(this.adminSvc.checkShowtimeConflict(this.showtimeForm.hallId, startUtc, endUtc, this.editingShowtime()?.id));
      if (conflict?.hasConflict) {
        const conflictTime = conflict.conflictingStartUtc ? this.formatShowtimeTime(conflict.conflictingStartUtc) : '';
        const conflictMsg = this.translate.instant('admin.conflict') + ': ' + (conflict.conflictingMovieTitle || '') + (conflictTime ? ' (' + conflictTime + ')' : '');
        this.showtimeError.set(conflictMsg);
        this.showtimeLoading.set(false);
        return;
      }
    } catch (e) {
      this.showtimeError.set('admin.networkError');
      this.showtimeLoading.set(false);
      return;
    }
    const isEdit = this.editingShowtime() !== null;
    const data = { startUtc, format: this.showtimeForm.format, basePrice: this.showtimeForm.basePrice };
    const obs = isEdit
      ? this.adminSvc.updateShowtime(this.editingShowtime()!.id, data)
      : this.adminSvc.createShowtime({ ...this.showtimeForm, startUtc });
    (obs as any).subscribe({
      next: () => { this.showtimeLoading.set(false); this.closeShowtimeDialog(); this.loadShowtimes(); isEdit ? this.showMessage('admin.showtimeUpdated') : this.showMessage('admin.showtimeCreated'); },
      error: (err: any) => { this.showtimeLoading.set(false); this.showtimeError.set(err?.error?.error || 'admin.error'); },
    });
  }
  deleteShowtime(id: number) {
    if (confirm(this.translate.instant('admin.confirmDelete'))) {
      this.adminSvc.deleteShowtime(id).subscribe({
        next: () => { this.loadShowtimes(); this.showMessage('admin.showtimeDeleted'); },
        error: () => this.showMessage('admin.networkError')
      });
    }
  }

  // Promo
  openPromoDialog(p?: PromoCodeDto) {
    if (p) {
      this.editingPromo.set(p);
      this.promoForm = {
        code: p.code,
        discountType: p.discountType,
        value: p.value,
        validFrom: new Date(p.validFrom),
        validTo: new Date(p.validTo),
        usageLimit: p.usageLimit,
        perUserLimit: p.perUserLimit,
        isPersonal: p.isPersonal,
        ownerUserId: p.ownerUserId
      };
    } else {
      this.editingPromo.set(null);
      this.promoForm = {
        code: '',
        discountType: 'Percent',
        value: 10,
        validFrom: new Date(),
        validTo: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
        usageLimit: 0,
        perUserLimit: 1,
        isPersonal: false,
        ownerUserId: null
      };
    }
    this.showPromoDialog.set(true);
  }
  closePromoDialog() { this.showPromoDialog.set(false); }
  savePromo() {
    if (!this.promoForm.code.trim()) {
      this.showMessage('validation.promoCodeRequired');
      return;
    }
    if (this.promoForm.value <= 0) {
      this.showMessage('validation.promoValueRequired');
      return;
    }
    if (this.promoForm.validTo <= this.promoForm.validFrom) {
      this.showMessage('validation.invalidDate');
      return;
    }
    const isEdit = this.editingPromo() !== null;
    const ownerUserId = this.promoForm.isPersonal && this.promoForm.ownerUserId
      ? this.promoForm.ownerUserId
      : undefined;
    const data = {
      code: this.promoForm.code.trim().toUpperCase(),
      discountType: this.promoForm.discountType,
      value: this.promoForm.value,
      validFrom: this.promoForm.validFrom.toISOString(),
      validTo: this.promoForm.validTo.toISOString(),
      usageLimit: this.promoForm.usageLimit,
      perUserLimit: this.promoForm.perUserLimit,
      isPersonal: this.promoForm.isPersonal,
      ownerUserId
    };
    const obs = isEdit
      ? this.adminSvc.updatePromoCode(this.editingPromo()!.id, data)
      : this.adminSvc.createPromoCode(data);
    (obs as any).subscribe({
      next: () => { this.closePromoDialog(); this.loadPromos(); isEdit ? this.showMessage('admin.promoUpdated') : this.showMessage('admin.promoCreated'); },
      error: (err: any) => this.showMessage(err?.error?.error || 'admin.networkError')
    });
  }
  deletePromo(id: number) {
    if (confirm(this.translate.instant('admin.confirmDelete'))) {
      this.adminSvc.deletePromoCode(id).subscribe({
        next: () => { this.loadPromos(); this.showMessage('admin.promoDeleted'); },
        error: () => this.showMessage('admin.networkError')
      });
    }
  }
}

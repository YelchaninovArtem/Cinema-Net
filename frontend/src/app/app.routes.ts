import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login.component').then(m => m.LoginComponent),
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/register/register.component').then(m => m.RegisterComponent),
      },
      { path: '', redirectTo: 'login', pathMatch: 'full' },
    ],
  },
  {
    path: 'catalog',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/catalog/catalog.component').then(m => m.CatalogComponent),
      },
      {
        path: 'movies/:id',
        loadComponent: () =>
          import('./features/catalog/movie-detail/movie-detail.component')
            .then(m => m.MovieDetailComponent),
      },
    ],
  },
  {
    path: 'buy',
    children: [
      {
        path: 'success',
        loadComponent: () =>
          import('./features/buy/buy-success/buy-success.component').then(m => m.BuySuccessComponent),
      },
      {
        path: 'paypal-return',
        loadComponent: () =>
          import('./features/buy/paypal-return/paypal-return.component').then(m => m.BuyPaypalReturnComponent),
      },
      {
        path: ':showtimeId',
        loadComponent: () =>
          import('./features/buy/buy-confirm/buy-confirm.component').then(m => m.BuyConfirmComponent),
      },
    ],
  },
  { path: 'booking/:showtimeId', redirectTo: 'buy/:showtimeId', pathMatch: 'full' },
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/account/account.component').then(m => m.AccountComponent),
  },
  {
    path: 'admin',
    canActivate: [roleGuard('Admin')],
    loadComponent: () =>
      import('./features/admin/admin.component').then(m => m.AdminComponent),
  },
  {
    path: 'cashier',
    canActivate: [roleGuard('Cashier')],
    loadComponent: () =>
      import('./features/cashier/cashier.component').then(m => m.CashierComponent),
  },
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  { path: '**', redirectTo: 'catalog' },
];

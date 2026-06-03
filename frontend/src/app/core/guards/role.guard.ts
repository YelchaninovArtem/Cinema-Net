import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

/** Фабрика guard-а що перевіряє наявність ролі. */
export const roleGuard = (...roles: string[]): CanActivateFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isLoggedIn()) return router.createUrlTree(['/auth/login']);
    if (roles.includes(auth.currentRole() ?? '')) return true;
    return router.createUrlTree(['/']);
  };

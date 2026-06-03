import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { environment } from '../../../environments/environment';

const isNgrokApiRequest = (url: string): boolean =>
  environment.apiUrl.includes('.ngrok-free.') && url.startsWith(environment.apiUrl);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const auth = inject(AuthService);
  const token = auth.getAccessToken();

  const headers: Record<string, string> = {};

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  if (isNgrokApiRequest(req.url)) {
    headers['ngrok-skip-browser-warning'] = 'true';
  }

  const authorizedReq = Object.keys(headers).length
    ? req.clone({ setHeaders: headers })
    : req;

  return next(authorizedReq).pipe(
    catchError((err: unknown) => {
      // якщо 401 і це не запит на оновлення токена - спробуємо refresh
      if (
        err instanceof HttpErrorResponse &&
        err.status === 401 &&
        !!token &&
        !req.url.includes('/auth/refresh') &&
        !req.url.includes('/auth/login')
      ) {
        return auth.refresh().pipe(
          switchMap(resp => {
            const retried = req.clone({
              setHeaders: {
                Authorization: `Bearer ${resp.accessToken}`,
                ...(isNgrokApiRequest(req.url) ? { 'ngrok-skip-browser-warning': 'true' } : {}),
              },
            });
            return next(retried);
          }),
          catchError(refreshErr => {
            auth.logout();
            return throwError(() => refreshErr);
          })
        );
      }
      return throwError(() => err);
    })
  );
};

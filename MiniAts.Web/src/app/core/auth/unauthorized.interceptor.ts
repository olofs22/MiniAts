import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { MeService } from '../me.service';

/**
 * A 401 from our API means the Supabase session is gone or no longer valid (expired,
 * revoked, etc.). Without this, a signed-out user just sees whatever generic
 * "Could not load X" error the calling feature happens to show, with no way back to /login.
 */
export const unauthorizedInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const meService = inject(MeService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401 && router.url !== '/login') {
        meService.clear();
        auth.signOut().finally(() => router.navigateByUrl('/login'));
      }
      return throwError(() => err);
    }),
  );
};

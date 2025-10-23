import {
  HttpClient,
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  catchError,
  filter,
  switchMap,
  take,
  throwError,
} from 'rxjs';

let isRefreshing = false;
let refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

const TOKEN_KEY = 'jwt_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const TOKEN_EXPIRES_KEY = 'token_expires';

function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

function isTokenValid(): boolean {
  const expiresAt = localStorage.getItem(TOKEN_EXPIRES_KEY);
  if (!expiresAt) return false;

  const expirationDate = new Date(expiresAt);
  const now = new Date();
  return expirationDate.getTime() > now.getTime();
}

function setToken(
  token: string,
  expiresAt: string,
  refreshToken?: string
): void {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(TOKEN_EXPIRES_KEY, expiresAt);
  if (refreshToken) {
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }
}

function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(TOKEN_EXPIRES_KEY);
  localStorage.removeItem('user_email');
}

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const router = inject(Router);

  // Skip auth for login, register, and refresh requests
  if (
    req.url.includes('/auth/login') ||
    req.url.includes('/auth/register') ||
    req.url.includes('/auth/refresh')
  ) {
    return next(req);
  }

  // Check if token is about to expire and try to refresh preemptively
  const token = getToken();
  const expiresAt = localStorage.getItem(TOKEN_EXPIRES_KEY);

  if (token && expiresAt) {
    const expirationDate = new Date(expiresAt);
    const now = new Date();
    const timeUntilExpiry = expirationDate.getTime() - now.getTime();

    // More aggressive preemptive refresh for short-lived tokens
    const isShortToken = timeUntilExpiry < 120000; // Less than 2 minutes
    const refreshThreshold = isShortToken ? 10000 : 5000; // 10s for short tokens, 5s for longer ones

    if (timeUntilExpiry < refreshThreshold && timeUntilExpiry > 0) {
      console.log(
        `Token expires in ${Math.round(
          timeUntilExpiry / 1000
        )}s, triggering preemptive refresh`
      );
      return handlePreemptiveRefresh(req, next, router);
    }
  }

  // Add auth token to request if valid
  if (token && isTokenValid()) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token) {
        console.log('Received 401 error, attempting token refresh...');
        return handle401Error(req, next, router);
      }
      return throwError(() => error);
    })
  );
};

function handlePreemptiveRefresh(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  router: Router
): Observable<HttpEvent<unknown>> {
  console.log('Token about to expire, attempting preemptive refresh...');

  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    const http = inject(HttpClient);
    const refreshToken = getRefreshToken();

    if (!refreshToken) {
      isRefreshing = false;
      clearToken();
      router.navigate(['/login']);
      return throwError(() => new Error('No refresh token available'));
    }

    return http
      .post<any>('http://localhost:5176/api/auth/refresh', {
        refreshToken: refreshToken,
      })
      .pipe(
        switchMap((response: any) => {
          isRefreshing = false;
          console.log('Preemptive refresh successful');

          if (response.token) {
            // Don't set tokens here - let AuthService handle it through its refresh method
            refreshTokenSubject.next(response.token);

            req = req.clone({
              setHeaders: {
                Authorization: `Bearer ${response.token}`,
              },
            });
            return next(req);
          }
          clearToken();
          router.navigate(['/login']);
          return throwError(() => new Error('Preemptive refresh failed'));
        }),
        catchError((error) => {
          isRefreshing = false;
          console.error('Preemptive refresh failed:', error);
          clearToken();
          router.navigate(['/login']);
          return throwError(() => error);
        })
      );
  } else {
    // Wait for ongoing refresh
    return refreshTokenSubject.pipe(
      filter((token) => token != null),
      take(1),
      switchMap((token) => {
        req = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
          },
        });
        return next(req);
      })
    );
  }
}

function handle401Error(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  router: Router
): Observable<HttpEvent<unknown>> {
  console.log('Handling 401 error, checking refresh state...');

  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    // Create a new HttpClient instance to avoid circular dependency
    const http = inject(HttpClient);
    const refreshToken = getRefreshToken();

    if (!refreshToken) {
      console.error('No refresh token available for 401 error handling');
      isRefreshing = false;
      clearToken();
      router.navigate(['/login']);
      return throwError(() => new Error('No refresh token available'));
    }

    console.log('Attempting token refresh after 401 error...');
    // Make refresh request with refresh token in body (no Authorization header)
    return http
      .post<any>('http://localhost:5176/api/auth/refresh', {
        refreshToken: refreshToken,
      })
      .pipe(
        switchMap((response: any) => {
          isRefreshing = false;

          if (response.token) {
            // Don't set tokens here - let AuthService handle it through its refresh method
            refreshTokenSubject.next(response.token);

            req = req.clone({
              setHeaders: {
                Authorization: `Bearer ${response.token}`,
              },
            });
            return next(req);
          } // If refresh failed, redirect to login
          clearToken();
          router.navigate(['/login']);
          return throwError(() => new Error('Token refresh failed'));
        }),
        catchError((error) => {
          isRefreshing = false;
          clearToken();
          router.navigate(['/login']);
          return throwError(() => error);
        })
      );
  } else {
    // Wait for the refresh to complete
    return refreshTokenSubject.pipe(
      filter((token) => token != null),
      take(1),
      switchMap((token) => {
        req = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
          },
        });
        return next(req);
      })
    );
  }
}

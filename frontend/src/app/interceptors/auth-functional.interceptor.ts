import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn, HttpEvent, HttpClient } from '@angular/common/http';
import { Observable, catchError, switchMap, throwError, BehaviorSubject, filter, take } from 'rxjs';
import { Router } from '@angular/router';

let isRefreshing = false;
let refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

const TOKEN_KEY = 'jwt_token';
const TOKEN_EXPIRES_KEY = 'token_expires';

function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

function isTokenValid(): boolean {
  const expiresAt = localStorage.getItem(TOKEN_EXPIRES_KEY);
  if (!expiresAt) return false;
  
  const expirationDate = new Date(expiresAt);
  const now = new Date();
  return expirationDate.getTime() > now.getTime();
}

function setToken(token: string, expiresAt: string): void {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(TOKEN_EXPIRES_KEY, expiresAt);
}

function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(TOKEN_EXPIRES_KEY);
  localStorage.removeItem('user_email');
}

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const router = inject(Router);

  // Skip auth for login and register requests
  if (req.url.includes('/auth/login') || req.url.includes('/auth/register')) {
    return next(req);
  }

  // Add auth token to request
  const token = getToken();
  if (token && isTokenValid()) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token && !req.url.includes('/auth/refresh')) {
        return handle401Error(req, next, router);
      }
      return throwError(() => error);
    })
  );
};

function handle401Error(req: HttpRequest<unknown>, next: HttpHandlerFn, router: Router): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    // Create a new HttpClient instance to avoid circular dependency
    const http = inject(HttpClient);
    
    return http.post<any>('http://localhost:5176/api/auth/refresh', {}).pipe(
      switchMap((response: any) => {
        isRefreshing = false;
        
        if (response.token) {
          setToken(response.token, response.expiresAt);
          refreshTokenSubject.next(response.token);
          
          req = req.clone({
            setHeaders: {
              Authorization: `Bearer ${response.token}`
            }
          });
          return next(req);
        }
        
        // If refresh failed, redirect to login
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
      filter(token => token != null),
      take(1),
      switchMap(token => {
        req = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        });
        return next(req);
      })
    );
  }
}
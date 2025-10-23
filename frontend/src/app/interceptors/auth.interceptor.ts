import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(
    null
  );

  constructor(private authService: AuthService, private router: Router) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    // Add auth header if user is logged in and token exists
    if (this.authService.isLoggedIn()) {
      req = this.addTokenHeader(req, this.authService.getToken());
    }

    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && this.authService.isLoggedIn()) {
          return this.handle401Error(req, next);
        }

        return throwError(() => error);
      })
    );
  }

  private addTokenHeader(
    request: HttpRequest<any>,
    token: string | null
  ): HttpRequest<any> {
    if (token) {
      return request.clone({
        headers: request.headers.set('Authorization', `Bearer ${token}`),
      });
    }
    return request;
  }

  private handle401Error(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      const token = this.authService.getToken();
      if (token) {
        return this.authService.refreshToken().pipe(
          switchMap((tokenResponse: any) => {
            this.isRefreshing = false;
            this.refreshTokenSubject.next(tokenResponse.token);

            return next.handle(
              this.addTokenHeader(request, tokenResponse.token)
            );
          }),
          catchError((error) => {
            this.isRefreshing = false;

            // Redirect to login page
            this.router.navigate(['/login']);
            return throwError(() => error);
          })
        );
      }
    }

    return this.refreshTokenSubject.pipe(
      filter((token) => token !== null),
      take(1),
      switchMap((token) => next.handle(this.addTokenHeader(request, token)))
    );
  }
}

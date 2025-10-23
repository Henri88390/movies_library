import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  User,
} from '../models/auth.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = 'http://localhost:5176/api/auth';
  private tokenKey = 'jwt_token';
  private refreshTokenKey = 'refresh_token';
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();
  private refreshTokenTimeout?: any;
  private loadingSubject = new BehaviorSubject<boolean>(true);
  public loading$ = this.loadingSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadStoredUser();
  }

  login(loginRequest: LoginRequest): Observable<AuthResponse> {
    this.loadingSubject.next(true);
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, loginRequest)
      .pipe(
        tap((response) => {
          this.setSession(response);
          this.loadingSubject.next(false);
        }),
        catchError((error) => {
          console.error('Login error:', error);
          this.loadingSubject.next(false);
          return throwError(() => error);
        })
      );
  }

  register(registerRequest: RegisterRequest): Observable<AuthResponse> {
    this.loadingSubject.next(true);
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, registerRequest)
      .pipe(
        tap((response) => {
          this.setSession(response);
          this.loadingSubject.next(false);
        }),
        catchError((error) => {
          console.error('Register error:', error);
          this.loadingSubject.next(false);
          return throwError(() => error);
        })
      );
  }

  logout(): Observable<any> {
    return this.http.post(`${this.apiUrl}/logout`, {}).pipe(
      tap(() => {
        this.clearSession();
      }),
      catchError((error) => {
        // Even if logout fails on server, clear local session
        this.clearSession();
        return throwError(() => error);
      })
    );
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap((user) => {
        this.userSubject.next(user);
      }),
      catchError((error) => {
        if (error.status === 401) {
          this.clearSession();
        }
        return throwError(() => error);
      })
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem(this.refreshTokenKey);
    const currentToken = localStorage.getItem(this.tokenKey);

    if (!refreshToken) {
      console.warn('❌ No refresh token available, clearing session');
      this.clearSession();
      return throwError(() => new Error('No refresh token available'));
    }

    console.log('🔄 Attempting to refresh token...');
    console.log(
      'Current JWT:',
      currentToken ? currentToken.substring(0, 20) + '...' : 'none'
    );
    console.log('Using refresh token:', refreshToken.substring(0, 20) + '...');
    console.log('Refresh token length:', refreshToken.length);
    console.log('Sending request to:', `${this.apiUrl}/refresh`);

    const requestBody = { refreshToken: refreshToken };
    console.log('Request body:', requestBody);

    return this.http
      .post<AuthResponse>(`${this.apiUrl}/refresh`, requestBody)
      .pipe(
        tap((response) => {
          console.log('✅ Token refresh successful, updating session');
          console.log('Response JWT:', response.token.substring(0, 20) + '...');
          this.setSession(response);
        }),
        catchError((error) => {
          console.error('❌ Token refresh failed:', error);
          console.error('Error status:', error.status);
          console.error(
            'Error message:',
            error.error?.message || error.message
          );
          console.warn('🧹 Clearing session due to refresh failure');
          this.clearSession();
          return throwError(() => error);
        })
      );
  }

  private setSession(authResponse: AuthResponse): void {
    const oldToken = localStorage.getItem(this.tokenKey);
    const oldTokenPreview = oldToken
      ? oldToken.substring(0, 30) + '...'
      : 'none';
    const newTokenPreview = authResponse.token.substring(0, 30) + '...';

    console.log('🔄 Setting session with new JWT token');
    console.log('Old JWT token:', oldTokenPreview);
    console.log('New JWT token:', newTokenPreview);
    console.log('Tokens are same?', oldToken === authResponse.token);

    localStorage.setItem(this.tokenKey, authResponse.token);
    localStorage.setItem('user_email', authResponse.email);
    localStorage.setItem('token_expires', authResponse.expiresAt);

    // Store refresh token only on first login/register (not on refresh)
    if (
      authResponse.refreshToken &&
      !localStorage.getItem(this.refreshTokenKey)
    ) {
      localStorage.setItem(this.refreshTokenKey, authResponse.refreshToken);
      console.log('✅ Initial refresh token stored');
    } else if (authResponse.refreshToken) {
      console.log('✅ Refresh token confirmed (no rotation)');
    }

    // Verify the token was actually stored
    const storedToken = localStorage.getItem(this.tokenKey);
    const storedTokenPreview = storedToken
      ? storedToken.substring(0, 30) + '...'
      : 'none';
    console.log('Stored JWT token:', storedTokenPreview);

    if (storedToken !== authResponse.token) {
      console.error(
        "❌ Token storage failed! Stored token doesn't match response token"
      );
      console.error('Expected:', newTokenPreview);
      console.error('Got:', storedTokenPreview);
    } else {
      console.log('✅ JWT token stored successfully');
    }

    // Set up automatic token refresh only if we have a refresh token
    if (authResponse.refreshToken) {
      this.startRefreshTokenTimer();
    } else {
      console.warn('No refresh token available, automatic refresh disabled');
    }

    // Load current user data
    this.getCurrentUser().subscribe({
      error: (error) => {
        console.error('Failed to load current user:', error);
      },
    });
  }
  private clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem('user_email');
    localStorage.removeItem('token_expires');
    this.userSubject.next(null);
    this.stopRefreshTokenTimer();
  }

  private loadStoredUser(): void {
    const token = this.getToken();
    if (token && this.isTokenValid()) {
      this.getCurrentUser().subscribe({
        next: () => this.loadingSubject.next(false),
        error: () => {
          this.clearSession();
          this.loadingSubject.next(false);
        },
      });
    } else {
      this.clearSession();
      this.loadingSubject.next(false);
    }
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    return this.getToken() !== null && this.isTokenValid();
  }

  private isTokenValid(): boolean {
    const expiresAt = localStorage.getItem('token_expires');
    if (!expiresAt) return false;

    const expirationDate = new Date(expiresAt);
    const now = new Date();
    return expirationDate.getTime() > now.getTime();
  }

  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    return new HttpHeaders({
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    });
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  private startRefreshTokenTimer(): void {
    // Clear any existing timer
    this.stopRefreshTokenTimer();

    const expiresAt = localStorage.getItem('token_expires');
    const refreshToken = localStorage.getItem(this.refreshTokenKey);

    if (!expiresAt) {
      console.warn('No token expiry time found, cannot set refresh timer');
      return;
    }

    if (!refreshToken) {
      console.warn('No refresh token found, cannot set refresh timer');
      return;
    }

    const expirationDate = new Date(expiresAt);
    const now = new Date();
    const timeUntilExpiry = expirationDate.getTime() - now.getTime();

    if (timeUntilExpiry <= 0) {
      console.warn('Token already expired, attempting immediate refresh');
      this.refreshToken().subscribe();
      return;
    }

    // For very short tokens, refresh at 70% of lifetime (more aggressive)
    // For longer tokens (>2 minutes), refresh 1 minute before expiry
    const isShortToken = timeUntilExpiry < 120000; // Less than 2 minutes
    const refreshTime = isShortToken
      ? Math.max(timeUntilExpiry * 0.7, 2000) // 70% of lifetime, minimum 2 seconds
      : Math.max(timeUntilExpiry - 60000, 2000); // 1 minute before expiry, minimum 2 seconds

    console.log(
      `Token expires in ${Math.round(
        timeUntilExpiry / 1000
      )}s, refreshing in ${Math.round(refreshTime / 1000)}s`
    );

    this.refreshTokenTimeout = setTimeout(() => {
      console.log('Automatic token refresh triggered');
      this.refreshToken().subscribe({
        next: () =>
          console.log('Automatic token refresh completed successfully'),
        error: (error) => {
          console.error('Automatic token refresh failed:', error);
          // clearSession is already called in refreshToken() error handler
        },
      });
    }, refreshTime);
  }

  private stopRefreshTokenTimer(): void {
    if (this.refreshTokenTimeout) {
      clearTimeout(this.refreshTokenTimeout);
      this.refreshTokenTimeout = undefined;
    }
  }
}

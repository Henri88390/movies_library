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
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadStoredUser();
  }

  login(loginRequest: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, loginRequest)
      .pipe(
        tap((response) => {
          this.setSession(response);
        }),
        catchError((error) => {
          console.error('Login error:', error);
          return throwError(() => error);
        })
      );
  }

  register(registerRequest: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, registerRequest)
      .pipe(
        tap((response) => {
          this.setSession(response);
        }),
        catchError((error) => {
          console.error('Register error:', error);
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
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, {}).pipe(
      tap((response) => {
        this.setSession(response);
      }),
      catchError((error) => {
        this.clearSession();
        return throwError(() => error);
      })
    );
  }

  private setSession(authResponse: AuthResponse): void {
    localStorage.setItem(this.tokenKey, authResponse.token);
    localStorage.setItem('user_email', authResponse.email);
    localStorage.setItem('token_expires', authResponse.expiresAt);

    // Load current user data
    this.getCurrentUser().subscribe();
  }

  private clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem('user_email');
    localStorage.removeItem('token_expires');
    this.userSubject.next(null);
  }

  private loadStoredUser(): void {
    const token = this.getToken();
    if (token && this.isTokenValid()) {
      this.getCurrentUser().subscribe();
    } else {
      this.clearSession();
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
}

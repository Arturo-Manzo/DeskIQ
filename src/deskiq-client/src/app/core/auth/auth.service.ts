import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { AuthUserDto, LoginRequest, LoginResponse } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly tokenSignal = signal<string | null>(localStorage.getItem('authToken'));
  private readonly userSignal = signal<AuthUserDto | null>(this.readUserFromStorage());

  readonly token = computed(() => this.tokenSignal());
  readonly user = computed(() => this.userSignal());
  readonly isAuthenticated = computed(() => !!this.tokenSignal());

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_BASE_URL}/api/auth/login`, payload)
      .pipe(tap((response) => this.setSession(response)));
  }

  logout(redirect = true): void {
    this.tokenSignal.set(null);
    this.userSignal.set(null);
    localStorage.removeItem('authToken');
    localStorage.removeItem('authUser');

    if (redirect) {
      this.router.navigateByUrl('/login');
    }
  }

  private setSession(response: LoginResponse): void {
    this.tokenSignal.set(response.token);
    this.userSignal.set(response.user);
    localStorage.setItem('authToken', response.token);
    localStorage.setItem('authUser', JSON.stringify(response.user));
  }

  private readUserFromStorage(): AuthUserDto | null {
    const raw = localStorage.getItem('authUser');
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUserDto;
    } catch {
      localStorage.removeItem('authUser');
      return null;
    }
  }
}

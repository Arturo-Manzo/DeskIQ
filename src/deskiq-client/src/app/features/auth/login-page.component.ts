import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeMode, ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.component.html',
})
export class LoginPageComponent {
  private static readonly EMAIL_HINT = 'usuario@correo.com';
  private static readonly PASSWORD_HINT = 'Ingresa tu contraseña';

  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly theme = inject(ThemeService);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly emailPlaceholder = signal(LoginPageComponent.EMAIL_HINT);
  readonly passwordPlaceholder = signal(LoginPageComponent.PASSWORD_HINT);
  readonly themeMode = this.theme.mode;

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  clearPlaceholderOnFocus(field: 'email' | 'password'): void {
    if (field === 'email') {
      this.emailPlaceholder.set('');
      return;
    }

    this.passwordPlaceholder.set('');
  }

  restorePlaceholderOnBlur(field: 'email' | 'password'): void {
    if (field === 'email' && !this.form.controls.email.value) {
      this.emailPlaceholder.set(LoginPageComponent.EMAIL_HINT);
      return;
    }

    if (field === 'password' && !this.form.controls.password.value) {
      this.passwordPlaceholder.set(LoginPageComponent.PASSWORD_HINT);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Credenciales inválidas. Verifica email y contraseña.');
      },
    });
  }

  onThemeModeChange(value: string): void {
    if (value === 'system' || value === 'light' || value === 'dark') {
      this.theme.setMode(value as ThemeMode);
    }
  }
}

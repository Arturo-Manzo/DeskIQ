import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type ThemeMode = 'system' | 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private static readonly STORAGE_KEY = 'deskiq-theme-mode';

  private readonly document = inject(DOCUMENT);
  private readonly mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

  readonly mode = signal<ThemeMode>('system');
  readonly activeTheme = signal<'light' | 'dark'>('dark');

  init(): void {
    const savedMode = this.readStoredMode();
    this.mode.set(savedMode);
    this.applyTheme(savedMode);

    this.mediaQuery.addEventListener('change', () => {
      if (this.mode() === 'system') {
        this.applyTheme('system');
      }
    });
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
    window.localStorage.setItem(ThemeService.STORAGE_KEY, mode);
    this.applyTheme(mode);
  }

  private readStoredMode(): ThemeMode {
    const raw = window.localStorage.getItem(ThemeService.STORAGE_KEY);
    if (raw === 'light' || raw === 'dark' || raw === 'system') {
      return raw;
    }

    return 'system';
  }

  private applyTheme(mode: ThemeMode): void {
    const theme = mode === 'system' ? (this.mediaQuery.matches ? 'dark' : 'light') : mode;
    this.activeTheme.set(theme);

    const root = this.document.documentElement;
    root.classList.toggle('theme-light', theme === 'light');
    root.classList.toggle('theme-dark', theme === 'dark');
    root.style.colorScheme = theme;
  }
}

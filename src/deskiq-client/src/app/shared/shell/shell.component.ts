
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
})
export class ShellComponent {
  private readonly auth = inject(AuthService);
  readonly user = this.auth.user;
  readonly isMenuOpen = signal(false);
  readonly isSidebarCollapsed = signal(false);
  readonly isDropdownOpen = signal(false);
  readonly displayName = computed(() => this.user()?.name ?? 'User');

  toggleMenu(): void {
    this.isMenuOpen.update((current) => !current);
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed.update((current) => !current);
  }

  logout(): void {
    this.auth.logout(true);
  }
}

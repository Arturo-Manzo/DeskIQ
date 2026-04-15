
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PermissionService } from '../../core/services/permission.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
})
export class ShellComponent {
  private readonly auth = inject(AuthService);
  private readonly permissions = inject(PermissionService);
  readonly user = this.auth.user;
  readonly isMenuOpen = signal(false);
  readonly isSidebarCollapsed = signal(false);
  readonly isDropdownOpen = signal(false);
  readonly displayName = computed(() => this.user()?.name ?? 'User');
  readonly userInitials = computed(() => {
    const name = this.user()?.name ?? 'U';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  });

  readonly showDashboard = computed(() => this.permissions.canViewDashboard());
  readonly showTickets = computed(() => true); // All authenticated users can view tickets
  readonly showDepartments = computed(() => this.permissions.canViewDepartments());
  readonly showUsers = computed(() => this.permissions.canViewUsers());
  readonly showCreateTicketButton = computed(() => this.permissions.canCreateTicket());

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

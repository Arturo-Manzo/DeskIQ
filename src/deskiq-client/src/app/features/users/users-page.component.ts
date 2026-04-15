import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { User } from '../../core/models/ticket.models';
import { UserService } from '../../core/services/user.service';
import { UserRole } from '../../core/models/auth.models';
import { ButtonDirective } from 'ui-design-system';

@Component({
  selector: 'app-users-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonDirective],
  templateUrl: './users-page.component.html',
})
export class UsersPageComponent {
  private readonly usersApi = inject(UserService);

  readonly loading = signal(true);
  readonly users = signal<User[]>([]);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly filteredUsers = computed(() => {
    const query = this.search().trim().toLowerCase();
    if (!query) {
      return this.users();
    }

    return this.users().filter((user) => {
      const values = [
        user.name,
        user.email,
        user.id,
        this.getRoleLabel(user.role),
      ];

      return values.some((value) => value.toLowerCase().includes(query));
    });
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.usersApi.getUsers(undefined, false).subscribe({
      next: (result) => {
        this.users.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar los usuarios.');
        this.loading.set(false);
      },
    });
  }

  onSearchChange(value: string): void {
    this.search.set(value);
  }

  getRoleLabel(role: number): string {
    switch (role) {
      case UserRole.Cliente:
        return 'Cliente';
      case UserRole.ClienteSupervisor:
        return 'Cliente Supervisor';
      case UserRole.Operador:
        return 'Operador';
      case UserRole.OperadorSupervisor:
        return 'Operador Supervisor';
      case UserRole.SupervisorGeneral:
        return 'Supervisor General';
      case UserRole.Auditor:
        return 'Auditor';
      case UserRole.Administrador:
        return 'Administrador';
      default:
        return 'Desconocido';
    }
  }
}

import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { User } from '../../core/models/ticket.models';
import { UserService } from '../../core/services/user.service';
import { UserRole } from '../../core/models/auth.models';
import { ButtonDirective } from 'ui-design-system';

@Component({
  selector: 'app-user-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonDirective],
  templateUrl: './user-detail-page.component.html',
})
export class UserDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly usersApi = inject(UserService);

  readonly loading = signal(true);
  readonly user = signal<User | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    const userId = this.route.snapshot.paramMap.get('id');
    if (userId) {
      this.load(userId);
    }
  }

  load(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.usersApi.getUser(id).subscribe({
      next: (result) => {
        this.user.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el usuario.');
        this.loading.set(false);
      },
    });
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

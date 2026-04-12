
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { DepartmentService } from '../../core/services/department.service';
import { Department } from '../../core/models/ticket.models';
import { UserRole } from '../../core/models/auth.models';

@Component({
  selector: 'app-departments-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './departments-page.component.html',
})
export class DepartmentsPageComponent {
  private readonly departmentService = inject(DepartmentService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly departments = signal<Department[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly deleting = signal<string | null>(null);

  constructor() {
    this.checkAdminPrivileges();
    this.loadDepartments();
  }

  checkAdminPrivileges(): void {
    const user = this.authService.user();
    if (!user || user.role !== UserRole.Admin) {
      this.router.navigate(['/dashboard']);
    }
  }

  loadDepartments(): void {
    this.loading.set(true);
    this.error.set(null);

    this.departmentService.getDepartments().subscribe({
      next: (departments) => {
        console.log('Departments loaded:', departments);
        this.departments.set(departments);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading departments:', err);
        this.error.set('No se pudieron cargar los departamentos. Intente nuevamente.');
        this.loading.set(false);
      },
    });
  }

  editDepartment(id: string): void {
    this.router.navigate(['/departments', id, 'edit']);
  }

  deleteDepartment(id: string): void {
    if (!confirm('¿Está seguro de que desea desactivar este departamento? Esta acción puede afectar a los tickets y usuarios asociados.')) {
      return;
    }

    this.deleting.set(id);
    this.departmentService.deleteDepartment(id).subscribe({
      next: () => {
        this.deleting.set(null);
        this.loadDepartments();
      },
      error: () => {
        this.deleting.set(null);
        this.error.set('No se pudo desactivar el departamento. Intente nuevamente.');
      },
    });
  }
}

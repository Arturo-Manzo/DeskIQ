import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { DepartmentService } from '../../core/services/department.service';
import { UserService, UpdateUserRequest } from '../../core/services/user.service';
import { Department, User } from '../../core/models/ticket.models';
import { UserRole } from '../../core/models/auth.models';
import { ButtonDirective } from 'ui-design-system';

@Component({
  selector: 'app-user-edit-page',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonDirective],
  templateUrl: './user-edit-page.component.html',
})
export class UserEditPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly departmentService = inject(DepartmentService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly departments = signal<Department[]>([]);
  readonly user = signal<User | null>(null);

  readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    departmentId: ['', Validators.required],
    role: [UserRole.Cliente, Validators.required],
    isActive: [true],
  });

  constructor() {
    const currentUser = this.authService.user();
    if (currentUser?.role !== UserRole.Administrador) {
      this.router.navigate(['/dashboard']);
    }

    const userId = this.route.snapshot.paramMap.get('id');
    if (userId) {
      this.loadUser(userId);
      this.loadDepartments();
    }
  }

  loadUser(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.userService.getUser(id).subscribe({
      next: (user) => {
        this.user.set(user);
        this.form.patchValue({
          name: user.name,
          email: user.email,
          departmentId: user.departmentId,
          role: user.role,
          isActive: user.isActive,
        });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el usuario.');
        this.loading.set(false);
      },
    });
  }

  loadDepartments(): void {
    this.loading.set(true);
    this.error.set(null);

    this.departmentService.getDepartments().subscribe({
      next: (departments) => {
        this.departments.set(departments);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar los departamentos.');
        this.loading.set(false);
      },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const formValue = this.form.value;
    const userId = this.user()?.id;

    if (!userId) {
      this.error.set('Usuario no encontrado.');
      this.submitting.set(false);
      return;
    }

    const request: UpdateUserRequest = {
      name: formValue.name,
      email: formValue.email,
      password: formValue.password || undefined,
      departmentId: formValue.departmentId,
      role: formValue.role,
      isActive: formValue.isActive,
    };

    this.userService.updateUser(userId, request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/users', userId]);
      },
      error: (err) => {
        const errorMessage = err.error?.message || 'No se pudo actualizar el usuario. Intente nuevamente.';
        this.error.set(errorMessage);
        this.submitting.set(false);
      },
    });
  }

  cancel(): void {
    const userId = this.user()?.id;
    if (userId) {
      this.router.navigate(['/users', userId]);
    } else {
      this.router.navigate(['/users']);
    }
  }

  get nameControl() {
    return this.form.get('name');
  }

  get emailControl() {
    return this.form.get('email');
  }

  get passwordControl() {
    return this.form.get('password');
  }

  get departmentIdControl() {
    return this.form.get('departmentId');
  }

  get roleControl() {
    return this.form.get('role');
  }

  get isActiveControl() {
    return this.form.get('isActive');
  }
}

import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { DepartmentService } from '../../core/services/department.service';
import { UserService, CreateUserRequest } from '../../core/services/user.service';
import { Department } from '../../core/models/ticket.models';
import { UserRole } from '../../core/models/auth.models';
import { ButtonDirective } from 'ui-design-system';

@Component({
  selector: 'app-user-create-page',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonDirective],
  templateUrl: './user-create-page.component.html',
})
export class UserCreatePageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly departmentService = inject(DepartmentService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly departments = signal<Department[]>([]);

  readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.minLength(6)]],
    departmentId: ['', Validators.required],
    role: [UserRole.Cliente, Validators.required],
    extId: [''],
    departmentPendingAssign: [false],
  });

  readonly isSSOUser = signal(false);

  constructor() {
    // Check if current user is admin
    const currentUser = this.authService.user();
    if (currentUser?.role !== UserRole.Administrador) {
      this.router.navigate(['/dashboard']);
      return;
    }
  }

  ngOnInit(): void {
    this.loadDepartments();
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

  onSSOToggle(): void {
    this.isSSOUser.update(v => !v);
    const passwordControl = this.form.get('password');
    const extIdControl = this.form.get('extId');
    const deptPendingControl = this.form.get('departmentPendingAssign');

    if (this.isSSOUser()) {
      passwordControl?.clearValidators();
      passwordControl?.setValue('');
      extIdControl?.setValidators([Validators.required]);
    } else {
      passwordControl?.setValidators([Validators.minLength(6)]);
      extIdControl?.clearValidators();
      extIdControl?.setValue('');
    }

    passwordControl?.updateValueAndValidity();
    extIdControl?.updateValueAndValidity();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const formValue = this.form.value;

    const request: CreateUserRequest = {
      name: formValue.name,
      email: formValue.email,
      password: formValue.password || undefined,
      departmentId: formValue.departmentId,
      role: formValue.role,
      extId: formValue.extId || undefined,
      departmentPendingAssign: formValue.departmentPendingAssign,
    };

    this.userService.createUser(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        const errorMessage = err.error?.message || 'No se pudo crear el usuario. Intente nuevamente.';
        this.error.set(errorMessage);
        this.submitting.set(false);
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/dashboard']);
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

  get extIdControl() {
    return this.form.get('extId');
  }
}


import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { DepartmentService, CreateDepartmentRequest, UpdateDepartmentRequest } from '../../core/services/department.service';
import { Department } from '../../core/models/ticket.models';
import { UserRole } from '../../core/models/auth.models';
import { ButtonDirective } from 'ui-design-system';

@Component({
  selector: 'app-department-create-page',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonDirective],
  templateUrl: './department-create-page.component.html',
})
export class DepartmentCreatePageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly departmentService = inject(DepartmentService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);
  readonly existingDepartments = signal<Department[]>([]);
  readonly similarDepartmentWarning = signal<string | null>(null);
  readonly codeAvailabilityMessage = signal<string | null>(null);
  readonly codeAvailable = signal<boolean | null>(null);
  readonly isEditMode = signal(false);
  readonly pageTitle = signal('Crear Nuevo Departamento');

  readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    code: ['', [Validators.required, Validators.pattern(/^[A-Z0-9]{2,4}$/)]],
    description: ['', [Validators.required, Validators.minLength(10)]],
    autoAssignRules: [''],
  });

  constructor() {
    this.checkAdminPrivileges();
    this.checkEditMode();
    this.loadDepartments();
  }

  checkEditMode(): void {
    const departmentId = this.route.snapshot.paramMap.get('id');
    if (departmentId) {
      this.isEditMode.set(true);
      this.pageTitle.set('Editar Departamento');
      this.loadDepartment(departmentId);
    }
  }

  loadDepartment(id: string): void {
    this.departmentService.getDepartment(id).subscribe({
      next: (department) => {
        this.form.patchValue({
          name: department.name,
          code: department.code,
          description: department.description,
          autoAssignRules: department.autoAssignRules,
        });
      },
      error: () => {
        this.error.set('No se pudo cargar el departamento.');
      },
    });
  }

  checkAdminPrivileges(): void {
    const user = this.authService.user();
    if (!user || user.role !== UserRole.Administrador) {
      this.router.navigate(['/dashboard']);
    }
  }

  loadDepartments(): void {
    this.departmentService.getDepartments().subscribe({
      next: (departments) => {
        this.existingDepartments.set(departments);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  checkSimilarity(name: string): void {
    if (!name || name.length < 3) {
      this.similarDepartmentWarning.set(null);
      return;
    }

    const lowerName = name.toLowerCase().trim();
    const similar = this.existingDepartments().find(
      (dept) => dept.name.toLowerCase().trim() === lowerName || 
                dept.name.toLowerCase().includes(lowerName) ||
                lowerName.includes(dept.name.toLowerCase())
    );

    if (similar) {
      this.similarDepartmentWarning.set(`Ya existe un departamento con nombre similar: "${similar.name}"`);
    } else {
      this.similarDepartmentWarning.set(null);
    }
  }

  onNameChange(): void {
    const name = this.form.get('name')?.value as string;
    this.checkSimilarity(name);

    if (this.isEditMode()) {
      return;
    }

    const codeControl = this.form.get('code');
    if (!codeControl) {
      return;
    }

    const hasManualValue = typeof codeControl.value === 'string' && codeControl.value.trim().length > 0;
    if (hasManualValue && codeControl.dirty) {
      return;
    }

    if (!name || name.trim().length < 2) {
      return;
    }

    this.departmentService.suggestCode(name).subscribe({
      next: (suggestedCode) => {
        const normalized = this.normalizeCode(suggestedCode);
        codeControl.setValue(normalized, { emitEvent: false });
        codeControl.markAsPristine();
        this.validateCodeAvailability(normalized);
      },
    });
  }

  onCodeInput(): void {
    const codeControl = this.form.get('code');
    if (!codeControl) {
      return;
    }

    const normalized = this.normalizeCode(codeControl.value);
    if (normalized !== codeControl.value) {
      codeControl.setValue(normalized, { emitEvent: false });
    }

    this.validateCodeAvailability(normalized);
  }

  suggestCode(): void {
    const name = (this.form.get('name')?.value as string) ?? '';
    if (!name || name.trim().length < 2) {
      return;
    }

    this.departmentService.suggestCode(name).subscribe({
      next: (code) => {
        const normalized = this.normalizeCode(code);
        this.form.get('code')?.setValue(normalized);
        this.validateCodeAvailability(normalized);
      },
    });
  }

  validateCodeAvailability(code: string): void {
    const normalized = this.normalizeCode(code);
    if (!/^[A-Z0-9]{2,4}$/.test(normalized)) {
      this.codeAvailable.set(null);
      this.codeAvailabilityMessage.set('El código debe tener entre 2 y 4 caracteres alfanuméricos.');
      return;
    }

    const departmentId = this.route.snapshot.paramMap.get('id') ?? undefined;
    this.departmentService.checkCodeAvailability(normalized, departmentId).subscribe({
      next: (available) => {
        this.codeAvailable.set(available);
        this.codeAvailabilityMessage.set(
          available
            ? 'Código disponible.'
            : 'Este código ya está en uso por otro departamento.',
        );
      },
      error: () => {
        this.codeAvailable.set(null);
        this.codeAvailabilityMessage.set('No se pudo validar la disponibilidad del código.');
      },
    });
  }

  private normalizeCode(code: unknown): string {
    return String(code ?? '').trim().toUpperCase();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.value;
    const lowerName = formValue.name.toLowerCase().trim();
    const normalizedCode = this.normalizeCode(formValue.code);
    const departmentId = this.route.snapshot.paramMap.get('id');

    // Check for duplicate names (exclude current department when editing)
    const exactMatch = this.existingDepartments().find(
      (dept) => dept.name.toLowerCase().trim() === lowerName && dept.id !== departmentId
    );

    if (exactMatch) {
      this.error.set('Ya existe un departamento con ese nombre exacto.');
      return;
    }

    if (!/^[A-Z0-9]{2,4}$/.test(normalizedCode)) {
      this.error.set('El código del departamento debe tener entre 2 y 4 caracteres alfanuméricos.');
      return;
    }

    if (!this.isEditMode() && this.codeAvailable() === false) {
      this.error.set('El código del departamento ya está en uso.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    if (this.isEditMode() && departmentId) {
      // Update existing department
      const request: UpdateDepartmentRequest = {
        name: formValue.name,
        code: normalizedCode,
        description: formValue.description,
        autoAssignRules: formValue.autoAssignRules || undefined,
      };

      this.departmentService.updateDepartment(departmentId, request).subscribe({
        next: () => {
          this.submitting.set(false);
          this.router.navigate(['/departments']);
        },
        error: () => {
          this.error.set('No se pudo actualizar el departamento. Intente nuevamente.');
          this.submitting.set(false);
        },
      });
    } else {
      // Create new department
      const request: CreateDepartmentRequest = {
        name: formValue.name,
        code: normalizedCode,
        description: formValue.description,
        autoAssignRules: formValue.autoAssignRules || undefined,
      };

      this.departmentService.createDepartment(request).subscribe({
        next: () => {
          this.submitting.set(false);
          this.router.navigate(['/departments']);
        },
        error: () => {
          this.error.set('No se pudo crear el departamento. Intente nuevamente.');
          this.submitting.set(false);
        },
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/departments']);
  }

  get nameControl() {
    return this.form.get('name');
  }

  get descriptionControl() {
    return this.form.get('description');
  }

  get codeControl() {
    return this.form.get('code');
  }

  get autoAssignRulesControl() {
    return this.form.get('autoAssignRules');
  }
}

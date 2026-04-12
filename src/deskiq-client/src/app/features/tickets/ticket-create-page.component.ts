
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { DepartmentService } from '../../core/services/department.service';
import { TicketService } from '../../core/services/ticket.service';
import { UserService } from '../../core/services/user.service';
import { CreateTicketRequest, Department, TicketPriority, TicketSource, User } from '../../core/models/ticket.models';
import { UserRole } from '../../core/models/auth.models';

@Component({
  selector: 'app-ticket-create-page',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './ticket-create-page.component.html',
})
export class TicketCreatePageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);
  private readonly departmentService = inject(DepartmentService);
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly departments = signal<Department[]>([]);
  readonly users = signal<User[]>([]);

  readonly form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', [Validators.required, Validators.minLength(10)]],
    priority: [TicketPriority.Medium, Validators.required],
    departmentId: ['', Validators.required],
    assignedToId: [null],
  });

  readonly canAssign = signal(false);

  constructor() {
    this.canAssign.set(this.hasAssignPrivileges());
  }

  ngOnInit(): void {
    this.loadDepartments();
  }

  hasAssignPrivileges(): boolean {
    const user = this.authService.user();
    if (!user) return false;
    return user.role === UserRole.Admin || user.role === UserRole.Supervisor;
  }

  loadDepartments(): void {
    this.loading.set(true);
    this.error.set(null);

    this.departmentService.getDepartments().subscribe({
      next: (departments) => {
        this.departments.set(departments);
        this.loading.set(false);

        // Load users if user has privileges
        if (this.canAssign()) {
          this.loadUsers();
        }
      },
      error: () => {
        this.error.set('No se pudieron cargar los departamentos.');
        this.loading.set(false);
      },
    });
  }

  loadUsers(): void {
    this.userService.getUsers(undefined, true).subscribe({
      next: (users) => {
        this.users.set(users);
      },
      error: () => {
        this.error.set('No se pudieron cargar los usuarios.');
      },
    });
  }

  onDepartmentChange(departmentId: string): void {
    if (this.canAssign()) {
      this.userService.getUsers(departmentId, true).subscribe({
        next: (users) => {
          this.users.set(users);
          // Reset assignedTo if it's not in the new user list
          const currentAssignedTo = this.form.get('assignedToId')?.value;
          if (currentAssignedTo && !users.find((u) => u.id === currentAssignedTo)) {
            this.form.get('assignedToId')?.setValue(null);
          }
        },
        error: () => {
          this.error.set('No se pudieron cargar los usuarios del departamento.');
        },
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const user = this.authService.user();
    if (!user) {
      this.error.set('Usuario no autenticado.');
      this.submitting.set(false);
      return;
    }

    const formValue = this.form.value;

    const request: CreateTicketRequest = {
      title: formValue.title,
      description: formValue.description,
      priority: formValue.priority,
      createdById: user.id,
      departmentId: formValue.departmentId,
      source: TicketSource.Web,
      assignedToId: formValue.assignedToId || undefined,
    };

    this.ticketService.createTicket(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/tickets']);
      },
      error: () => {
        this.error.set('No se pudo crear el ticket. Intente nuevamente.');
        this.submitting.set(false);
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/tickets']);
  }

  get titleControl() {
    return this.form.get('title');
  }

  get descriptionControl() {
    return this.form.get('description');
  }

  get priorityControl() {
    return this.form.get('priority');
  }

  get departmentIdControl() {
    return this.form.get('departmentId');
  }

  get assignedToIdControl() {
    return this.form.get('assignedToId');
  }
}

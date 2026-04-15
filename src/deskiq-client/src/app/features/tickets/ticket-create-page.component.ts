
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { DepartmentService } from '../../core/services/department.service';
import { TicketService } from '../../core/services/ticket.service';
import { UserService } from '../../core/services/user.service';
import { CreateTicketRequest, Department, TicketPriority, TicketSource, User } from '../../core/models/ticket.models';
import { UserRole } from '../../core/models/auth.models';
import { ButtonDirective } from 'ui-design-system';

interface FileWithProgress {
  file: File;
  progress: number;
  error: string | null;
}

@Component({
  selector: 'app-ticket-create-page',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonDirective],
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

  // File upload configuration
  readonly maxFileSize = 10 * 1024 * 1024; // 10MB
  readonly maxFiles = 10;
  readonly allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.pdf', '.doc', '.docx', '.txt', '.csv'];
  readonly files = signal<FileWithProgress[]>([]);
  readonly uploadProgress = signal(0);
  readonly dragOver = signal(false);

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
    return user.role === UserRole.Administrador || 
           user.role === UserRole.OperadorSupervisor || 
           user.role === UserRole.SupervisorGeneral;
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

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    
    const droppedFiles = event.dataTransfer?.files;
    if (droppedFiles) {
      this.handleFiles(Array.from(droppedFiles));
    }
  }

  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selectedFiles = input.files;
    if (selectedFiles) {
      this.handleFiles(Array.from(selectedFiles));
    }
    input.value = ''; // Reset input
  }

  handleFiles(newFiles: File[]): void {
    const currentFiles = this.files();
    
    if (currentFiles.length + newFiles.length > this.maxFiles) {
      this.error.set(`Máximo ${this.maxFiles} archivos permitidos por ticket`);
      return;
    }

    for (const file of newFiles) {
      // Validate file size
      if (file.size > this.maxFileSize) {
        this.error.set(`El archivo "${file.name}" excede el tamaño máximo de ${this.maxFileSize / (1024 * 1024)}MB`);
        continue;
      }

      // Validate file extension
      const extension = '.' + file.name.split('.').pop()?.toLowerCase();
      if (!this.allowedExtensions.includes(extension)) {
        this.error.set(`El archivo "${file.name}" tiene una extensión no permitida`);
        continue;
      }

      // Check for duplicates
      const isDuplicate = currentFiles.some(f => f.file.name === file.name && f.file.size === file.size);
      if (isDuplicate) {
        this.error.set(`El archivo "${file.name}" ya ha sido agregado`);
        continue;
      }

      this.files.update(files => [...files, { file, progress: 0, error: null }]);
    }

    // Clear error after 3 seconds if it's a file validation error
    setTimeout(() => this.error.set(null), 3000);
  }

  removeFile(index: number): void {
    this.files.update(files => files.filter((_, i) => i !== index));
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
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
    const currentFiles = this.files();

    // Use multipart/form-data if files are present
    if (currentFiles.length > 0) {
      const formData = new FormData();
      formData.append('Title', formValue.title);
      formData.append('Description', formValue.description);
      formData.append('Priority', formValue.priority.toString());
      formData.append('DepartmentId', formValue.departmentId);
      formData.append('Source', TicketSource.Web.toString());
      if (formValue.assignedToId) {
        formData.append('AssignedToId', formValue.assignedToId);
      }

      currentFiles.forEach(f => {
        formData.append('Attachments', f.file);
      });

      this.ticketService.createTicketWithAttachments(formData).subscribe({
        next: () => {
          this.submitting.set(false);
          this.router.navigate(['/tickets']);
        },
        error: () => {
          this.error.set('No se pudo crear el ticket. Intente nuevamente.');
          this.submitting.set(false);
        },
      });
    } else {
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

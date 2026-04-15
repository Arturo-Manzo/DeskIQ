import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subticket, Ticket, TicketPriority, TicketStatus, User, Department, getTicketPriorityLabel, getTicketStatusLabel } from '../../core/models/ticket.models';
import { TicketService } from '../../core/services/ticket.service';
import { UserService } from '../../core/services/user.service';
import { DepartmentService } from '../../core/services/department.service';
import { AuthService } from '../../core/auth/auth.service';
import { UserRole } from '../../core/models/auth.models';
import { ButtonDirective } from 'ui-design-system';

@Component({
  selector: 'app-ticket-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonDirective],
  templateUrl: './ticket-detail-page.component.html',
})
export class TicketDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly ticketService = inject(TicketService);
  private readonly userService = inject(UserService);
  private readonly departmentService = inject(DepartmentService);
  private readonly authService = inject(AuthService);

  readonly loading = signal(true);
  // State for attachment visualizer modal
  readonly attachmentPreviewModalOpen = signal(false);
  readonly attachmentPreviewAsset = signal<any | null>(null); // Ideally type Ticket['attachments'][0] | null
  readonly attachmentPreviewUrl = signal<string | null>(null);
  readonly attachmentPreviewError = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly ticket = signal<Ticket | null>(null);
  readonly isForbidden = signal(false);
  readonly isNotFound = signal(false);
  readonly isInvalidId = signal(false);
  readonly working = signal(false);
  readonly actionError = signal<string | null>(null);
  readonly actionSuccess = signal<string | null>(null);
  readonly comment = signal('');
  readonly replyModalOpen = signal(false);
  readonly replyToMessageId = signal<string | null>(null);
  readonly replyToMessageSummary = signal<string | null>(null);
  readonly replyContent = signal('');
  readonly selectedStatus = signal<TicketStatus | null>(null);
  readonly assignees = signal<User[]>([]);
  readonly selectedAssigneeId = signal<string>('');
  readonly subtickets = signal<Subticket[]>([]);
  readonly subticketModalOpen = signal(false);
  readonly subticketTitle = signal('');
  readonly subticketDescription = signal('');
  readonly subticketPriority = signal<TicketPriority>(TicketPriority.Medium);
  readonly subticketAssigneeId = signal('');
  readonly subticketBlocked = signal(false);
  readonly subticketBlockedReason = signal('');
  readonly uploadingFile = signal(false);
  readonly fileUploadError = signal<string | null>(null);
  readonly activities = signal<any[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly selectedDepartmentId = signal<string>('');
  readonly departmentChangeModalOpen = signal(false);
  readonly departmentChangeError = signal<string | null>(null);
  readonly editModalOpen = signal(false);
  readonly editTitle = signal('');
  readonly editDescription = signal('');
  readonly reassignModalOpen = signal(false);

  readonly statuses: Array<{ value: TicketStatus; label: string }> = [
    { value: TicketStatus.Open, label: getTicketStatusLabel(TicketStatus.Open) },
    { value: TicketStatus.InProgress, label: getTicketStatusLabel(TicketStatus.InProgress) },
    { value: TicketStatus.PendingCustomer, label: getTicketStatusLabel(TicketStatus.PendingCustomer) },
    { value: TicketStatus.Resolved, label: getTicketStatusLabel(TicketStatus.Resolved) },
    { value: TicketStatus.Closed, label: getTicketStatusLabel(TicketStatus.Closed) },
    { value: TicketStatus.Reopened, label: getTicketStatusLabel(TicketStatus.Reopened) },
  ];

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const ticketId = params.get('id');
        if (!ticketId || !this.isGuid(ticketId)) {
          this.ticket.set(null);
          this.isInvalidId.set(true);
          this.error.set('El identificador del ticket no es válido.');
          this.loading.set(false);
          return;
        }

        this.load(ticketId);
      });
  }

  load(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.isForbidden.set(false);
    this.isNotFound.set(false);
    this.isInvalidId.set(false);

    this.ticketService.getTicketById(id).subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.selectedStatus.set(ticket.status);
        this.selectedAssigneeId.set(ticket.assignedToId ?? '');
        this.subticketAssigneeId.set(ticket.assignedToId ?? '');
        this.loadAssignees(ticket.departmentId);
        this.loadSubtickets(ticket.id);
        this.loadActivities(ticket.id);
        this.loading.set(false);
      },
      error: (response: HttpErrorResponse) => {
        this.ticket.set(null);

        if (response.status === 403) {
          this.isForbidden.set(true);
          this.error.set('No tienes permisos para ver este ticket.');
        } else if (response.status === 404) {
          this.isNotFound.set(true);
          this.error.set('El ticket solicitado no existe o fue eliminado.');
        } else {
          this.error.set('No se pudo cargar el detalle del ticket. Intenta nuevamente.');
        }

        this.loading.set(false);
      },
    });
  }

  canEditTitleDescription(): boolean {
    const currentTicket = this.ticket();
    const currentUser = this.authService.user();

    if (!currentTicket || !currentUser) {
      return false;
    }

    // Cannot edit if ticket is resolved or closed
    if (currentTicket.status === TicketStatus.Resolved || currentTicket.status === TicketStatus.Closed) {
      return false;
    }

    // Creator can always edit their own ticket
    if (currentUser.id === currentTicket.createdById) {
      return true;
    }

    // OperadorSupervisor, ClienteSupervisor, SupervisorGeneral, Administrador can edit
    return currentUser.role === UserRole.OperadorSupervisor ||
           currentUser.role === UserRole.ClienteSupervisor ||
           currentUser.role === UserRole.SupervisorGeneral ||
           currentUser.role === UserRole.Administrador;
  }

  backToList(): void {
    this.router.navigate(['/tickets']);
  }

  retryLoad(): void {
    const ticketId = this.route.snapshot.paramMap.get('id');
    if (!ticketId || !this.isGuid(ticketId)) {
      this.isInvalidId.set(true);
      this.error.set('El identificador del ticket no es válido.');
      return;
    }

    this.load(ticketId);
  }

  onCommentChange(value: string): void {
    this.comment.set(value);
  }

  openReplyModal(messageId: string): void {
    const currentTicket = this.ticket();
    if (!currentTicket?.messages?.length) {
      return;
    }

    const message = currentTicket.messages.find((item) => item.id === messageId);
    if (!message) {
      return;
    }

    this.replyToMessageId.set(message.id);
    this.replyToMessageSummary.set(`${message.senderName || 'Usuario'}: ${message.content.slice(0, 90)}`);
    this.replyContent.set('');
    this.replyModalOpen.set(true);
  }

  closeReplyModal(): void {
    this.replyModalOpen.set(false);
    this.replyToMessageId.set(null);
    this.replyToMessageSummary.set(null);
    this.replyContent.set('');
  }

  onReplyContentChange(value: string): void {
    this.replyContent.set(value);
  }

  onAssigneeChange(value: string): void {
    this.selectedAssigneeId.set(value);
  }

  onStatusChange(value: string): void {
    const status = Number.parseInt(value, 10);
    if (Number.isNaN(status)) {
      return;
    }

    this.selectedStatus.set(status as TicketStatus);
  }

  onSubticketTitleChange(value: string): void {
    this.subticketTitle.set(value);
  }

  onSubticketDescriptionChange(value: string): void {
    this.subticketDescription.set(value);
  }

  onSubticketPriorityChange(value: string): void {
    const parsed = Number.parseInt(value, 10);
    if (Number.isNaN(parsed)) {
      return;
    }

    this.subticketPriority.set(parsed as TicketPriority);
  }

  onSubticketAssigneeChange(value: string): void {
    this.subticketAssigneeId.set(value);
  }

  onSubticketBlockedChange(value: boolean): void {
    this.subticketBlocked.set(value);
    if (!value) {
      this.subticketBlockedReason.set('');
    }
  }

  onSubticketBlockedReasonChange(value: string): void {
    this.subticketBlockedReason.set(value);
  }

  openSubticketDetail(subticketId: string): void {
    if (!subticketId) {
      return;
    }

    this.router.navigate(['/tickets', subticketId]);
  }

  isPrimaryTicketAssignmentLocked(status: TicketStatus | null | undefined): boolean {
    return status === TicketStatus.Resolved || status === TicketStatus.Closed;
  }

  openSubticketModal(): void {
    this.subticketModalOpen.set(true);
  }

  closeSubticketModal(): void {
    this.subticketModalOpen.set(false);
    this.subticketTitle.set('');
    this.subticketDescription.set('');
    this.subticketPriority.set(TicketPriority.Medium);
    this.subticketBlocked.set(false);
    this.subticketBlockedReason.set('');
  }

  assignTicket(): void {
    const currentTicket = this.ticket();
    const assigneeId = this.selectedAssigneeId();

    if (!currentTicket || !assigneeId) {
      return;
    }

    if (this.isPrimaryTicketAssignmentLocked(currentTicket.status)) {
      this.actionError.set('No se puede reasignar un ticket resuelto o cerrado.');
      return;
    }

    this.working.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);

    this.ticketService
      .updateTicket(currentTicket.id, {
        assignedToId: assigneeId,
      })
      .subscribe({
        next: () => {
          this.actionSuccess.set('Ticket asignado correctamente.');
          this.working.set(false);
          this.retryLoad();
        },
        error: (response: HttpErrorResponse) => {
          const message = response.error?.message || (typeof response.error === 'string' ? response.error : null);
          this.actionError.set(message || 'No se pudo asignar el ticket.');
          this.working.set(false);
        },
      });
  }

  updateTicketStatus(): void {
    const currentTicket = this.ticket();
    const nextStatus = this.selectedStatus();

    if (!currentTicket || nextStatus == null) {
      return;
    }

    const isClosingParent =
      !currentTicket.parentTicketId &&
      (nextStatus === TicketStatus.Resolved || nextStatus === TicketStatus.Closed);

    let closeOpenSubticketsWithParent = false;
    if (isClosingParent && this.hasOpenSubtickets()) {
      closeOpenSubticketsWithParent = window.confirm(
        'Este ticket principal tiene subtickets abiertos. ¿Deseas cerrarlos automáticamente también?',
      );

      if (!closeOpenSubticketsWithParent) {
        this.actionError.set('No se puede cerrar el ticket principal mientras existan subtickets abiertos.');
        return;
      }
    }

    this.working.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);

    this.ticketService
      .updateTicket(currentTicket.id, {
        status: nextStatus,
        closeOpenSubticketsWithParent,
      })
      .subscribe({
        next: () => {
          this.actionSuccess.set('Estado actualizado correctamente.');
          this.working.set(false);
          this.retryLoad();
        },
        error: (response: HttpErrorResponse) => {
          const message = response.error?.message || (typeof response.error === 'string' ? response.error : null);
          this.actionError.set(message || 'No se pudo actualizar el estado.');
          this.working.set(false);
        },
      });
  }

  hasOpenSubtickets(): boolean {
    return this.subtickets().some(
      (subticket) => subticket.status !== TicketStatus.Resolved && subticket.status !== TicketStatus.Closed,
    );
  }

  addComment(): void {
    const currentTicket = this.ticket();
    const content = this.comment().trim();

    if (!currentTicket || !content) {
      return;
    }

    this.working.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);

    this.ticketService
      .addMessage(currentTicket.id, {
        content,
        isInternal: false,
      })
      .subscribe({
        next: () => {
          this.comment.set('');
          this.actionSuccess.set('Comentario agregado.');
          this.working.set(false);
          this.retryLoad();
        },
        error: () => {
          this.actionError.set('No se pudo agregar el comentario.');
          this.working.set(false);
        },
      });
  }

  submitReply(): void {
    const currentTicket = this.ticket();
    const parentMessageId = this.replyToMessageId();
    const content = this.replyContent().trim();

    if (!currentTicket || !parentMessageId || !content) {
      return;
    }

    this.working.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);

    this.ticketService
      .addMessage(currentTicket.id, {
        content,
        isInternal: false,
        parentMessageId,
      })
      .subscribe({
        next: () => {
          this.actionSuccess.set('Respuesta agregada.');
          this.working.set(false);
          this.closeReplyModal();
          this.retryLoad();
        },
        error: () => {
          this.actionError.set('No se pudo publicar la respuesta.');
          this.working.set(false);
        },
      });
  }

  createSubticket(): void {
    const currentTicket = this.ticket();
    if (!currentTicket) {
      return;
    }

    const title = this.subticketTitle().trim();
    const description = this.subticketDescription().trim();

    if (!title || !description) {
      this.actionError.set('El subticket requiere título y descripción.');
      return;
    }

    if (this.subticketBlocked() && !this.subticketBlockedReason().trim()) {
      this.actionError.set('Debes indicar el motivo del bloqueo del subticket.');
      return;
    }

    this.working.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);

    this.ticketService
      .createSubticket(currentTicket.id, {
        title,
        description,
        priority: this.subticketPriority(),
        assignedToId: this.subticketAssigneeId() || undefined,
        isBlocked: this.subticketBlocked(),
        blockedReason: this.subticketBlocked() ? this.subticketBlockedReason().trim() : undefined,
      })
      .subscribe({
        next: () => {
          this.closeSubticketModal();
          this.actionSuccess.set('Subticket creado correctamente.');
          this.working.set(false);
          this.retryLoad();
        },
        error: (response: HttpErrorResponse) => {
          const message = response.error?.message || (typeof response.error === 'string' ? response.error : null);
          this.actionError.set(message || 'No se pudo crear el subticket.');
          this.working.set(false);
        },
      });
  }

  getMessageReplyContext(messageParentId?: string | null): string | null {
    if (!messageParentId) {
      return null;
    }

    const currentTicket = this.ticket();
    const parentMessage = currentTicket?.messages?.find((item) => item.id === messageParentId);

    if (!parentMessage) {
      return 'Respuesta';
    }

    return `Respuesta a ${parentMessage.senderName || 'usuario'}: ${parentMessage.content.slice(0, 70)}`;
  }

  getRootMessages(): NonNullable<Ticket['messages']> {
    const messages = this.ticket()?.messages ?? [];
    return messages.filter((message) => !message.parentMessageId);
  }

  getReplies(parentMessageId: string): NonNullable<Ticket['messages']> {
    const messages = this.ticket()?.messages ?? [];
    return messages.filter((message) => message.parentMessageId === parentMessageId);
  }

  private loadAssignees(departmentId: string): void {
    this.userService.getAssignees(departmentId).subscribe({
      next: (users) => {
        this.assignees.set(users);

        if (this.selectedAssigneeId() && !users.some((user) => user.id === this.selectedAssigneeId())) {
          this.selectedAssigneeId.set('');
        }
      },
      error: () => {
        this.assignees.set([]);
      },
    });
  }

  private loadSubtickets(ticketId: string): void {
    this.ticketService.getSubtickets(ticketId).subscribe({
      next: (subtickets) => {
        this.subtickets.set(subtickets);
      },
      error: () => {
        this.subtickets.set([]);
      },
    });
  }

  private loadActivities(ticketId: string): void {
    this.ticketService.getActivities(ticketId).subscribe({
      next: (activities) => {
        this.activities.set(activities);
      },
      error: () => {
        this.activities.set([]);
      },
    });
  }

  getActivityIcon(type: number): string {
    switch (type) {
      case 1: return '🎫'; // TicketCreated
      case 2: return '📊'; // StatusChanged
      case 3: return '⚡'; // PriorityChanged
      case 4: return '👤'; // Assigned
      case 5: return '🔄'; // Reassigned
      case 6: return '📋'; // SubticketCreated
      case 7: return '🔒'; // TicketBlocked
      case 8: return '🔓'; // TicketUnblocked
      case 9: return '✅'; // TicketResolved
      case 10: return '✅'; // TicketClosed
      case 11: return '🔄'; // TicketReopened
      case 12: return '💬'; // CommentAdded
      case 13: return '🏢'; // DepartmentChangeAttempt
      case 14: return '⚠️'; // DepartmentChangeSuccess
      case 15: return '✏️'; // TitleChanged
      case 16: return '📝'; // DescriptionChanged
      default: return '📌';
    }
  }

  getActivityDescription(activity: any): string {
    if (activity.oldValue && activity.newValue) {
      return `${activity.description}: ${activity.oldValue} → ${activity.newValue}`;
    }
    return activity.description || '';
  }

  getParentTicketStatusClass(status: number): { [key: string]: boolean } {
    if (status === 4 || status === 5) {
      return { 'bg-[var(--ui-success-bg)]': true, 'text-[var(--ui-success-text)]': true };
    } else if (status === 2) {
      return { 'bg-[var(--ui-warning-bg)]': true, 'text-[var(--ui-warning-text)]': true };
    } else {
      return { 'bg-[var(--ui-info-bg)]': true, 'text-[var(--ui-info-text)]': true };
    }
  }

  getPriorityClass(priority: number): { [key: string]: boolean } {
    if (priority >= 3) {
      return { 'text-[var(--ui-critical-text)]': true };
    } else {
      return { 'text-[var(--color-text)]': true };
    }
  }

  getStatusLabel(status: number): string {
    return getTicketStatusLabel(status);
  }

  getPriorityLabel(priority: number): string {
    return getTicketPriorityLabel(priority);
  }

  getSubticketStatusClass(status: number): { [key: string]: boolean } {
    if (status === TicketStatus.Resolved || status === TicketStatus.Closed) {
      return { 'border-[var(--ui-success-text)]': true };
    } else {
      return { 'border-[var(--ui-danger-text)]': true };
    }
  }

  getSubticketStatusTextColorClass(status: number): { [key: string]: boolean } {
    if (status === TicketStatus.Resolved || status === TicketStatus.Closed) {
      return { 'text-[var(--ui-success-text)]': true };
    } else {
      return { 'text-[var(--ui-danger-text)]': true };
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.uploadAttachment(file);
      input.value = ''; // Reset input
    }
  }

  uploadAttachment(file: File): void {
    const currentTicket = this.ticket();
    if (!currentTicket) return;

    // Validate file size (10MB max)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      this.fileUploadError.set('El archivo excede el tamaño máximo de 10MB');
      return;
    }

    // Validate file extension
    const allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.pdf', '.doc', '.docx', '.txt', '.csv'];
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!allowedExtensions.includes(fileExtension)) {
      this.fileUploadError.set(`Extensión de archivo no permitida: ${fileExtension}`);
      return;
    }

    this.uploadingFile.set(true);
    this.fileUploadError.set(null);

    this.ticketService.uploadAttachment(currentTicket.id, file).subscribe({
      next: () => {
        this.actionSuccess.set('Archivo adjuntado correctamente');
        this.uploadingFile.set(false);
        this.retryLoad();
      },
      error: () => {
        this.fileUploadError.set('No se pudo adjuntar el archivo');
        this.uploadingFile.set(false);
      },
    });
  }

  downloadAttachment(attachmentId: string, fileName: string): void {
    const currentTicket = this.ticket();
    if (!currentTicket) return;

    this.ticketService.downloadAttachment(currentTicket.id, attachmentId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.actionError.set('No se pudo descargar el archivo');
      },
    });
  }

  deleteAttachment(attachmentId: string): void {
    if (!confirm('¿Estás seguro de que deseas eliminar este archivo?')) {
      return;
    }

    const currentTicket = this.ticket();
    if (!currentTicket) return;

    this.working.set(true);
    this.actionError.set(null);

    this.ticketService.deleteAttachment(currentTicket.id, attachmentId).subscribe({
      next: () => {
        this.actionSuccess.set('Archivo eliminado correctamente');
        this.working.set(false);
        this.retryLoad();
      },
      error: () => {
        this.actionError.set('No se pudo eliminar el archivo');
        this.working.set(false);
      },
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }

  openAttachmentPreview(asset: any): void {
    if (!this.isAttachmentViewable(asset.contentType, asset.fileName)) {
      return;
    }

    const currentTicket = this.ticket();
    if (!currentTicket) return;

    this.attachmentPreviewAsset.set(asset);
    this.attachmentPreviewUrl.set(null);
    this.attachmentPreviewError.set(null);
    this.attachmentPreviewModalOpen.set(true);

    this.ticketService.downloadAttachment(currentTicket.id, asset.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        this.attachmentPreviewUrl.set(url);
      },
      error: (err) => {
        console.error('Download error:', err);
        this.attachmentPreviewError.set('El archivo no está disponible. Por favor, vuelve a subirlo.');
      },
    });
  }

  closeAttachmentPreview(): void {
    if (this.attachmentPreviewUrl()) {
      window.URL.revokeObjectURL(this.attachmentPreviewUrl()!);
    }
    this.attachmentPreviewModalOpen.set(false);
    this.attachmentPreviewAsset.set(null);
    this.attachmentPreviewUrl.set(null);
    this.attachmentPreviewError.set(null);
  }

  isAttachmentViewable(contentType: string | undefined, fileName: string | undefined): boolean {
    if (!contentType || !fileName) return false;

    const imageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml'];
    const pdfType = 'application/pdf';

    if (imageTypes.includes(contentType)) return true;
    if (contentType === pdfType) return true;

    const imageExtensions = /\.(png|jpe?g|gif|webp|svg)$/i;
    const pdfExtensions = /\.pdf$/i;

    if (imageExtensions.test(fileName)) return true;
    if (pdfExtensions.test(fileName)) return true;

    return false;
  }

  private isGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
  }

  openDepartmentChangeModal(): void {
    const currentTicket = this.ticket();
    if (!currentTicket) return;

    this.selectedDepartmentId.set('');
    this.departmentChangeModalOpen.set(true);
    this.departmentChangeError.set(null);

    this.departmentService.getDepartments().subscribe({
      next: (departments) => this.departments.set(departments),
      error: () => this.departments.set([])
    });
  }

  closeDepartmentChangeModal(): void {
    this.departmentChangeModalOpen.set(false);
    this.selectedDepartmentId.set('');
    this.departmentChangeError.set(null);
  }

  onDepartmentChange(value: string): void {
    this.selectedDepartmentId.set(value);
  }

  changeDepartment(): void {
    const currentTicket = this.ticket();
    const newDepartmentId = this.selectedDepartmentId();

    if (!currentTicket || !newDepartmentId) {
      return;
    }

    // All validation is handled by backend
    this.working.set(true);
    this.departmentChangeError.set(null);
    this.actionSuccess.set(null);

    this.ticketService.changeTicketDepartment(currentTicket.id, { newDepartmentId }).subscribe({
      next: () => {
        this.actionSuccess.set('Departamento cambiado correctamente.');
        this.working.set(false);
        this.closeDepartmentChangeModal();
        this.retryLoad();
      },
      error: (response: HttpErrorResponse) => {
        const message = response.error?.message || (typeof response.error === 'string' ? response.error : null);
        this.departmentChangeError.set(message || 'No se pudo cambiar el departamento.');
        this.working.set(false);
      },
    });
  }

  openEditModal(): void {
    const currentTicket = this.ticket();
    if (!currentTicket) return;

    this.editTitle.set(currentTicket.title);
    this.editDescription.set(currentTicket.description);
    this.editModalOpen.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);
  }

  closeEditModal(): void {
    this.editModalOpen.set(false);
    this.editTitle.set('');
    this.editDescription.set('');
    this.actionError.set(null);
  }

  saveTitleDescription(): void {
    const currentTicket = this.ticket();
    const title = this.editTitle().trim();
    const description = this.editDescription().trim();

    if (!currentTicket || !title || !description) {
      this.actionError.set('El título y la descripción son requeridos.');
      return;
    }

    if (title === currentTicket.title && description === currentTicket.description) {
      this.actionError.set('No hay cambios para guardar.');
      return;
    }

    if (!window.confirm('¿Estás seguro de que deseas guardar los cambios en el título y descripción?')) {
      return;
    }

    this.working.set(true);
    this.actionError.set(null);
    this.actionSuccess.set(null);

    this.ticketService.updateTicket(currentTicket.id, {
      title,
      description,
    }).subscribe({
      next: () => {
        this.actionSuccess.set('Título y descripción actualizados correctamente.');
        this.working.set(false);
        this.closeEditModal();
        this.retryLoad();
      },
      error: (response: HttpErrorResponse) => {
        const message = response.error?.message || (typeof response.error === 'string' ? response.error : null);
        this.actionError.set(message || 'No se pudo actualizar el título y descripción.');
        this.working.set(false);
      },
    });
  }
}

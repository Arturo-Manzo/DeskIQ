export enum TicketStatus {
  Open = 1,
  InProgress = 2,
  PendingCustomer = 3,
  Resolved = 4,
  Closed = 5,
  Reopened = 6,
}

export enum TicketPriority {
  Low = 1,
  Medium = 2,
  High = 3,
  Critical = 4,
}

export function getTicketStatusLabel(status: TicketStatus): string {
  switch (status) {
    case TicketStatus.Open:
      return 'Abierto';
    case TicketStatus.InProgress:
      return 'En Progreso';
    case TicketStatus.PendingCustomer:
      return 'Esperando Cliente';
    case TicketStatus.Resolved:
      return 'Resuelto';
    case TicketStatus.Closed:
      return 'Cerrado';
    case TicketStatus.Reopened:
      return 'Reabierto';
    default:
      return 'Desconocido';
  }
}

export function getTicketPriorityLabel(priority: TicketPriority): string {
  switch (priority) {
    case TicketPriority.Low:
      return 'Baja';
    case TicketPriority.Medium:
      return 'Media';
    case TicketPriority.High:
      return 'Alta';
    case TicketPriority.Critical:
      return 'Crítica';
    default:
      return 'Desconocido';
  }
}

export enum TicketSource {
  Web = 1,
  Email = 2,
  WhatsApp = 3,
  API = 4,
}

export enum ActivityType {
  TicketCreated = 1,
  StatusChanged = 2,
  PriorityChanged = 3,
  Assigned = 4,
  Reassigned = 5,
  SubticketCreated = 6,
  TicketBlocked = 7,
  TicketUnblocked = 8,
  TicketResolved = 9,
  TicketClosed = 10,
  TicketReopened = 11,
  CommentAdded = 12,
}

export interface Ticket {
  id: string;
  ticketId: string;
  title: string;
  description: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdById: string;
  assignedToId: string | null;
  departmentId: string;
  source?: TicketSource;
  externalId?: string | null;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
  parentTicketId?: string | null;
  isBlocked?: boolean;
  blockedReason?: string | null;
  department?: {
    id: string;
    name: string;
    code: string;
  } | null;
  createdBy?: {
    id: string;
    name: string;
    email: string;
  } | null;
  messages?: Array<{
    id: string;
    parentMessageId?: string | null;
    content: string;
    createdAt: string;
    senderId: string;
    isInternal: boolean;
    senderName?: string | null;
    senderEmail?: string | null;
  }>;
  attachments?: Array<{
    id: string;
    fileName: string;
    contentType: string;
    sizeBytes: number;
    uploadedAt: string;
    filePath?: string;
    uploadedBy?: {
      id: string;
      name: string;
      email: string;
    } | null;
  }>;
  assignedTo?: {
    id: string;
    name: string;
    email: string;
  } | null;
  subtickets?: Subticket[];
  parentTicket?: {
    id: string;
    ticketId: string;
    title: string;
    status: TicketStatus;
    priority: TicketPriority;
    assignedTo?: {
      id: string;
      name: string;
      email: string;
    } | null;
    resolvedAt: string | null;
  } | null;
}

export interface Subticket {
  id: string;
  ticketId: string;
  title: string;
  status: TicketStatus;
  priority: TicketPriority;
  assignedToId: string | null;
  assignedTo?: {
    id: string;
    name: string;
    email: string;
  } | null;
  createdAt: string;
  isBlocked: boolean;
  blockedReason?: string | null;
}

export interface TicketNotification {
  ticketId: string;
  title: string;
  status: TicketStatus;
  priority: TicketPriority;
  departmentId: string;
  assignedToId: string | null;
  assignedToName: string;
  createdAt: string;
  action: string;
  performedBy: string;
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
  createdById: string;
  departmentId: string;
  source: TicketSource;
  externalId?: string;
  assignedToId?: string;
}

export interface UpdateTicketRequest {
  title?: string;
  description?: string;
  status?: TicketStatus;
  priority?: TicketPriority;
  assignedToId?: string;
  isBlocked?: boolean;
  blockedReason?: string;
  closeOpenSubticketsWithParent?: boolean;
}

export interface AddTicketMessageRequest {
  content: string;
  isInternal: boolean;
  parentMessageId?: string;
}

export interface CreateSubticketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
  assignedToId?: string;
  isBlocked: boolean;
  blockedReason?: string;
}

export interface BlockTicketRequest {
  reason: string;
}

export interface UnblockTicketRequest {
  resolutionNotes?: string;
}

export interface User {
  id: string;
  name: string;
  email: string;
  departmentId: string;
  role: number;
  isActive: boolean;
}

export interface Department {
  id: string;
  name: string;
  code: string;
  description: string;
  autoAssignRules: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface TicketActivity {
  id: string;
  ticketId: string;
  type: ActivityType;
  description?: string | null;
  oldValue?: string | null;
  newValue?: string | null;
  performedBy: {
    id: string;
    name: string;
    email: string;
  };
  createdAt: string;
}

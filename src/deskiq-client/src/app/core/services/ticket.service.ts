import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import {
  AddTicketMessageRequest,
  BlockTicketRequest,
  CreateSubticketRequest,
  CreateTicketRequest,
  Subticket,
  Ticket,
  UnblockTicketRequest,
  UpdateTicketRequest,
  TicketActivity,
} from '../models/ticket.models';

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);

  private toArray<T>(value: any): T[] {
    if (Array.isArray(value)) {
      return value as T[];
    }

    if (value && Array.isArray(value.$values)) {
      return value.$values as T[];
    }

    return [];
  }

  private mapSubticket(ticket: any): Subticket {
    return {
      id: ticket.id,
      ticketId: ticket.ticketId,
      title: ticket.title,
      status: ticket.status,
      priority: ticket.priority,
      assignedToId: ticket.assignedToId,
      assignedTo: ticket.assignedTo
        ? {
            id: ticket.assignedTo.id,
            name: ticket.assignedTo.name,
            email: ticket.assignedTo.email,
          }
        : null,
      createdAt: ticket.createdAt,
      isBlocked: ticket.isBlocked ?? false,
      blockedReason: ticket.blockedReason ?? null,
    };
  }

  private mapTicket(ticket: any): Ticket {
    return {
      id: ticket.id,
      ticketId: ticket.ticketId,
      title: ticket.title,
      description: ticket.description,
      status: ticket.status,
      priority: ticket.priority,
      createdById: ticket.createdById,
      assignedToId: ticket.assignedToId,
      departmentId: ticket.departmentId,
      source: ticket.source,
      externalId: ticket.externalId,
      createdAt: ticket.createdAt,
      updatedAt: ticket.updatedAt,
      resolvedAt: ticket.resolvedAt ?? null,
      parentTicketId: ticket.parentTicketId ?? null,
      isBlocked: ticket.isBlocked ?? false,
      blockedReason: ticket.blockedReason ?? null,
      department: ticket.department
        ? {
            id: ticket.department.id,
            name: ticket.department.name,
            code: ticket.department.code,
          }
        : null,
      createdBy: ticket.createdBy
        ? {
            id: ticket.createdBy.id,
            name: ticket.createdBy.name,
            email: ticket.createdBy.email,
          }
        : null,
      assignedTo: ticket.assignedTo
        ? {
            id: ticket.assignedTo.id,
            name: ticket.assignedTo.name,
            email: ticket.assignedTo.email,
          }
        : null,
      parentTicket: ticket.parentTicket
        ? {
            id: ticket.parentTicket.id,
            ticketId: ticket.parentTicket.ticketId,
            title: ticket.parentTicket.title,
            status: ticket.parentTicket.status,
            priority: ticket.parentTicket.priority,
            assignedTo: ticket.parentTicket.assignedTo
              ? {
                  id: ticket.parentTicket.assignedTo.id,
                  name: ticket.parentTicket.assignedTo.name,
                  email: ticket.parentTicket.assignedTo.email,
                }
              : null,
            resolvedAt: ticket.parentTicket.resolvedAt ?? null,
          }
        : null,
      messages: this.toArray<any>(ticket.messages).map((message: any) => ({
            id: message.id,
        parentMessageId: message.parentMessageId ?? null,
            content: message.content,
            createdAt: message.createdAt,
            senderId: message.senderId,
            isInternal: message.isInternal,
        senderName: message.sender?.name ?? null,
        senderEmail: message.sender?.email ?? null,
          })),
      attachments: this.toArray<any>(ticket.attachments).map((attachment: any) => ({
            id: attachment.id,
            fileName: attachment.fileName,
            contentType: attachment.contentType,
            sizeBytes: attachment.fileSize,
            uploadedAt: attachment.uploadedAt,
            filePath: attachment.filePath,
            uploadedBy: attachment.uploadedBy
              ? {
                  id: attachment.uploadedBy.id,
                  name: attachment.uploadedBy.name,
                  email: attachment.uploadedBy.email,
                }
              : null,
          })),
      subtickets: this.toArray<any>(ticket.subtickets).map((subticket: any) => this.mapSubticket(subticket)),
    } as Ticket;
  }

  getTickets(page = 1, pageSize = 25): Observable<Ticket[]> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));

    return this.http.get<any>(`${API_BASE_URL}/api/tickets`, { params }).pipe(
      map(response => {
        // Handle ASP.NET Core JSON serialization format
        const tickets = response.$values || response;
        return this.toArray<any>(tickets).map((ticket: any) => this.mapTicket(ticket));
      })
    );
  }

  getTicketById(id: string): Observable<Ticket> {
    return this.http.get<any>(`${API_BASE_URL}/api/tickets/${id}`).pipe(
      map((ticket) => this.mapTicket(ticket))
    );
  }

  createTicket(request: CreateTicketRequest): Observable<Ticket> {
    return this.http.post<any>(`${API_BASE_URL}/api/tickets`, request).pipe(
      map((ticket) => this.mapTicket(ticket))
    );
  }

  updateTicket(id: string, request: UpdateTicketRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/tickets/${id}`, request);
  }

  addMessage(id: string, request: AddTicketMessageRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/api/tickets/${id}/messages`, request);
  }

  getSubtickets(id: string): Observable<Subticket[]> {
    return this.http.get<any>(`${API_BASE_URL}/api/tickets/${id}/subtickets`).pipe(
      map((response) => {
        const subtickets = response?.$values ?? response;
        return this.toArray<any>(subtickets).map((subticket: any) => this.mapSubticket(subticket));
      }),
    );
  }

  createSubticket(parentId: string, request: CreateSubticketRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/api/tickets/${parentId}/subtickets`, request);
  }

  blockTicket(id: string, request: BlockTicketRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/api/tickets/${id}/block`, request);
  }

  unblockTicket(id: string, request: UnblockTicketRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/api/tickets/${id}/unblock`, request);
  }

  uploadAttachment(ticketId: string, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${API_BASE_URL}/api/tickets/${ticketId}/attachments`, formData);
  }

  downloadAttachment(ticketId: string, attachmentId: string): Observable<Blob> {
    return this.http.get(`${API_BASE_URL}/api/tickets/${ticketId}/attachments/${attachmentId}`, {
      responseType: 'blob'
    });
  }

  deleteAttachment(ticketId: string, attachmentId: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/tickets/${ticketId}/attachments/${attachmentId}`);
  }

  getActivities(ticketId: string, page = 1, pageSize = 50): Observable<TicketActivity[]> {
    const params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));

    return this.http.get<any>(`${API_BASE_URL}/api/tickets/${ticketId}/activities`, { params }).pipe(
      map((response) => {
        const activities = response?.$values ?? response;
        return this.toArray<any>(activities).map((activity: any) => ({
          id: activity.id,
          ticketId: activity.ticketId,
          type: activity.type,
          description: activity.description,
          oldValue: activity.oldValue,
          newValue: activity.newValue,
          performedBy: activity.performedBy
            ? {
                id: activity.performedBy.id,
                name: activity.performedBy.name,
                email: activity.performedBy.email,
              }
            : { id: '', name: 'Unknown', email: '' },
          createdAt: activity.createdAt,
        }));
      })
    );
  }
}

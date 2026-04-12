import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Ticket } from '../../core/models/ticket.models';
import { TicketService } from '../../core/services/ticket.service';
import { getTicketStatusLabel, getTicketPriorityLabel } from '../../core/models/ticket.models';

@Component({
  selector: 'app-tickets-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './tickets-page.component.html',
})
export class TicketsPageComponent {
  private readonly ticketsApi = inject(TicketService);

  readonly loading = signal(true);
  readonly tickets = signal<Ticket[]>([]);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly filteredTickets = computed(() => {
    const query = this.search().trim().toLowerCase();
    if (!query) {
      return this.tickets();
    }

    return this.tickets().filter((ticket) => {
      const values = [
        ticket.title,
        ticket.ticketId,
        ticket.id,
        ticket.department?.name ?? '',
        this.getStatusLabel(ticket.status),
        this.getPriorityLabel(ticket.priority),
      ];

      return values.some((value) => value.toLowerCase().includes(query));
    });
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.ticketsApi.getTickets(1, 100).subscribe({
      next: (result) => {
        this.tickets.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar los tickets.');
        this.loading.set(false);
      },
    });
  }

  onSearchChange(value: string): void {
    this.search.set(value);
  }

  getStatusLabel(status: number): string {
    return getTicketStatusLabel(status);
  }

  getPriorityLabel(priority: number): string {
    return getTicketPriorityLabel(priority);
  }
}

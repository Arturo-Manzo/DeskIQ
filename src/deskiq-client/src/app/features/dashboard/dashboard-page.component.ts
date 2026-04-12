import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { TicketService } from '../../core/services/ticket.service';
import { TicketRealtimeService } from '../../core/services/ticket-realtime.service';
import { Ticket, TicketPriority, TicketStatus, getTicketPriorityLabel, getTicketStatusLabel } from '../../core/models/ticket.models';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-page.component.html',
})
export class DashboardPageComponent implements OnInit, OnDestroy {
  private readonly ticketsApi = inject(TicketService);
  private readonly realtime = inject(TicketRealtimeService);
  private readonly auth = inject(AuthService);

  readonly isLoading = signal(true);
  readonly error = signal<string | null>(null);
  readonly tickets = signal<Ticket[]>([]);

  readonly openCount = computed(
    () =>
      this.tickets().filter(
        (t) => t.status === TicketStatus.Open || t.status === TicketStatus.InProgress,
      ).length,
  );
  readonly criticalCount = computed(
    () => this.tickets().filter((t) => t.priority === TicketPriority.Critical).length,
  );
  readonly resolvedToday = computed(
    () =>
      this.tickets().filter((t) => {
        if (!t.resolvedAt) {
          return false;
        }
        return new Date(t.resolvedAt).toDateString() === new Date().toDateString();
      }).length,
  );

  ngOnInit(): void {
    this.loadTickets();
    const token = this.auth.token();
    if (token) {
      this.realtime.connect(token, () => this.loadTickets()).catch(() => {
        // Live updates are optional, polling fallback remains available via manual refresh.
      });
    }
  }

  ngOnDestroy(): void {
    this.realtime.disconnect().catch(() => {
      // No-op.
    });
  }

  loadTickets(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.ticketsApi.getTickets(1, 20).subscribe({
      next: (tickets) => {
        this.tickets.set(tickets);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('No fue posible cargar el dashboard en este momento.');
        this.isLoading.set(false);
      },
    });
  }

  getStatusLabel(status: number): string {
    return getTicketStatusLabel(status);
  }

  getPriorityLabel(priority: number): string {
    return getTicketPriorityLabel(priority);
  }
}

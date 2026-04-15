import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Ticket, TicketViewScope } from '../../core/models/ticket.models';
import { TicketService } from '../../core/services/ticket.service';
import { PermissionService } from '../../core/services/permission.service';
import { getTicketStatusLabel, getTicketPriorityLabel } from '../../core/models/ticket.models';
import { ButtonDirective } from 'ui-design-system';

@Component({
  selector: 'app-tickets-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonDirective],
  templateUrl: './tickets-page.component.html',
})
export class TicketsPageComponent {
  private readonly ticketsApi = inject(TicketService);
  private readonly permissions = inject(PermissionService);

  readonly loading = signal(true);
  readonly tickets = signal<Ticket[]>([]);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly currentScope = signal<TicketViewScope>(TicketViewScope.Departamento);
  readonly availableScopes = computed(() => {
    const role = this.permissions.getCurrentUserRole();
    switch (role) {
      case 1: // UserRole.Cliente
        return [TicketViewScope.MisTickets];
      case 2: // UserRole.ClienteSupervisor
      case 3: // UserRole.Operador
      case 4: // UserRole.OperadorSupervisor
        return [TicketViewScope.MisTickets, TicketViewScope.Departamento];
      case 5: // UserRole.SupervisorGeneral
      case 6: // UserRole.Auditor
      case 7: // UserRole.Administrador
        return [TicketViewScope.MisTickets, TicketViewScope.Departamento, TicketViewScope.Todos];
      default:
        return [TicketViewScope.Departamento];
    }
  });
  readonly canCreateTicket = computed(() => this.permissions.canCreateTicket());
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
    const defaultScope = this.getDefaultScopeForRole();
    this.currentScope.set(defaultScope);
    this.ticketsApi.getTickets(1, 100, this.currentScope()).subscribe({
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

  onScopeChange(scope: TicketViewScope): void {
    this.currentScope.set(scope);
    this.load();
  }

  private getDefaultScopeForRole(): TicketViewScope {
    const role = this.permissions.getCurrentUserRole();
    switch (role) {
      case 1: // UserRole.Cliente
        return TicketViewScope.MisTickets;
      case 2: // UserRole.ClienteSupervisor
      case 3: // UserRole.Operador
      case 4: // UserRole.OperadorSupervisor
        return TicketViewScope.Departamento;
      case 5: // UserRole.SupervisorGeneral
      case 6: // UserRole.Auditor
      case 7: // UserRole.Administrador
        return TicketViewScope.Todos;
      default:
        return TicketViewScope.Departamento;
    }
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

  getScopeLabel(scope: TicketViewScope): string {
    switch (scope) {
      case TicketViewScope.MisTickets:
        return 'Mis Tickets';
      case TicketViewScope.Departamento:
        return 'Departamento';
      case TicketViewScope.Todos:
        return 'Todos';
      default:
        return 'Departamento';
    }
  }
}

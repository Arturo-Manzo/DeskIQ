import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { API_BASE_URL } from '../config/api.config';
import { TicketNotification } from '../models/ticket.models';

@Injectable({ providedIn: 'root' })
export class TicketRealtimeService {
  private hubConnection: HubConnection | null = null;

  async connect(token: string, onTicketChanged: () => void): Promise<void> {
    if (this.hubConnection?.state === 'Connected') {
      return;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hub/tickets`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('TicketCreated', (_event: TicketNotification) => onTicketChanged());
    this.hubConnection.on('TicketUpdated', (_event: TicketNotification) => onTicketChanged());
    this.hubConnection.on('TicketAssigned', (_event: TicketNotification) => onTicketChanged());

    await this.hubConnection.start();
  }

  async disconnect(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
    }
  }
}

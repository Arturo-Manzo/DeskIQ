import { inject, Injectable } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { UserRole } from '../models/auth.models';

@Injectable({
  providedIn: 'root',
})
export class PermissionService {
  private readonly auth = inject(AuthService);

  private get userRole(): UserRole | undefined {
    return this.auth.user()?.role;
  }

  getCurrentUserRole(): UserRole | undefined {
    return this.userRole;
  }

  /**
   * Users with metrics access: ClienteSupervisor, OperadorSupervisor, SupervisorGeneral, Auditor, Administrador
   * Based on ROLE_MATRIX.md: View Metrics permission
   */
  canViewDashboard(): boolean {
    const role = this.userRole;
    if (!role) return false;
    
    return [
      UserRole.ClienteSupervisor,
      UserRole.OperadorSupervisor,
      UserRole.SupervisorGeneral,
      UserRole.Auditor,
      UserRole.Administrador,
    ].includes(role);
  }

  /**
   * Users who can create tickets: Cliente, Operador, OperadorSupervisor, SupervisorGeneral, Administrador
   * Based on ROLE_MATRIX.md: Create Tickets permission
   */
  canCreateTicket(): boolean {
    const role = this.userRole;
    if (!role) return false;
    
    return [
      UserRole.Cliente,
      UserRole.Operador,
      UserRole.OperadorSupervisor,
      UserRole.SupervisorGeneral,
      UserRole.Administrador,
    ].includes(role);
  }

  /**
   * Users who can view departments: SupervisorGeneral, Auditor, Administrador
   * Based on ROLE_MATRIX.md: View Departments permission
   */
  canViewDepartments(): boolean {
    const role = this.userRole;
    if (!role) return false;
    
    return [
      UserRole.SupervisorGeneral,
      UserRole.Auditor,
      UserRole.Administrador,
    ].includes(role);
  }

  /**
   * Users who can manage departments: Administrador only
   * Based on ROLE_MATRIX.md: Create/Edit/Delete Departments permissions
   */
  canManageDepartments(): boolean {
    return this.userRole === UserRole.Administrador;
  }

  /**
   * Users who can view users: SupervisorGeneral, Auditor, Administrador
   * Based on ROLE_MATRIX.md: View Users permission
   */
  canViewUsers(): boolean {
    const role = this.userRole;
    if (!role) return false;
    
    return [
      UserRole.SupervisorGeneral,
      UserRole.Auditor,
      UserRole.Administrador,
    ].includes(role);
  }

  /**
   * Users who can manage users: Administrador only
   * Based on ROLE_MATRIX.md: Create/Edit/Delete Users and Change User Roles permissions
   */
  canManageUsers(): boolean {
    return this.userRole === UserRole.Administrador;
  }
}

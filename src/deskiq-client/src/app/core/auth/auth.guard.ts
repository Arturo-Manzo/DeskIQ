import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { PermissionService } from '../services/permission.service';
import { UserRole } from '../models/auth.models';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.parseUrl('/login');
};

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.parseUrl('/login');
  }

  const user = auth.user();
  if (user?.role !== UserRole.Administrador) {
    return router.parseUrl('/dashboard');
  }

  return true;
};

export const metricsGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const permissions = inject(PermissionService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.parseUrl('/login');
  }

  if (!permissions.canViewDashboard()) {
    return router.parseUrl('/tickets');
  }

  return true;
};

export const departmentsGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const permissions = inject(PermissionService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.parseUrl('/login');
  }

  if (!permissions.canViewDepartments()) {
    return router.parseUrl('/dashboard');
  }

  return true;
};

export const ticketCreateGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const permissions = inject(PermissionService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.parseUrl('/login');
  }

  if (!permissions.canCreateTicket()) {
    return router.parseUrl('/tickets');
  }

  return true;
};

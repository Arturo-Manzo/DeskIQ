import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { User } from '../models/ticket.models';
import { UserRole } from '../models/auth.models';

export interface CreateUserRequest {
  name: string;
  email: string;
  password?: string;
  departmentId: string;
  role: UserRole;
  extId?: string;
  departmentPendingAssign?: boolean;
}

export interface UpdateUserRequest {
  name?: string;
  email?: string;
  password?: string;
  departmentId?: string;
  role?: UserRole;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  getUsers(departmentId?: string, isActive: boolean = true): Observable<User[]> {
    let params = new HttpParams();
    if (isActive) {
      params = params.set('isActive', 'true');
    }
    if (departmentId) {
      params = params.set('departmentId', departmentId);
    }

    return this.http.get<any>(`${API_BASE_URL}/api/users`, { params }).pipe(
      map((response) => {
        const users = response.$values || response;
        if (!Array.isArray(users)) {
          return [];
        }

        return users.map(
          (user: any) =>
            ({
              id: user.id,
              name: user.name,
              email: user.email,
              departmentId: user.departmentId,
              departmentName: user.departmentName,
              role: user.role,
              isActive: user.isActive,
            }) as User,
        );
      }),
    );
  }

  getAssignees(departmentId: string): Observable<User[]> {
    return this.http.get<any>(`${API_BASE_URL}/api/users/assignees?departmentId=${departmentId}`).pipe(
      map((response) => {
        const users = response.$values || response;
        if (!Array.isArray(users)) {
          return [];
        }

        return users.map(
          (user: any) =>
            ({
              id: user.id,
              name: user.name,
              email: user.email,
              departmentId: user.departmentId,
              departmentName: user.departmentName,
              role: user.role,
              isActive: user.isActive,
            }) as User,
        );
      }),
    );
  }

  createUser(request: CreateUserRequest): Observable<User> {
    return this.http.post<any>(`${API_BASE_URL}/api/users`, request).pipe(
      map((user) => ({
        id: user.id,
        name: user.name,
        email: user.email,
        departmentId: user.departmentId,
        departmentName: user.departmentName,
        role: user.role,
        isActive: user.isActive,
      }) as User)
    );
  }

  getUser(id: string): Observable<User> {
    return this.http.get<any>(`${API_BASE_URL}/api/users/${id}`).pipe(
      map((user) => ({
        id: user.id,
        name: user.name,
        email: user.email,
        departmentId: user.departmentId,
        departmentName: user.departmentName,
        role: user.role,
        isActive: user.isActive,
      }) as User)
    );
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/users/${id}`, request);
  }
}

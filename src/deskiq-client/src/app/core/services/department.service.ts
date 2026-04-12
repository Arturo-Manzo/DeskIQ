import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { Department } from '../models/ticket.models';

export interface CreateDepartmentRequest {
  name: string;
  code: string;
  description: string;
  autoAssignRules?: string;
}

export interface UpdateDepartmentRequest {
  name?: string;
  code?: string;
  description?: string;
  autoAssignRules?: string;
}

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private readonly http = inject(HttpClient);

  getDepartments(): Observable<Department[]> {
    return this.http.get<any>(`${API_BASE_URL}/api/departments`).pipe(
      map(response => {
        // Handle ASP.NET Core JSON serialization format
        const depts = response.$values || response;
        if (!Array.isArray(depts)) {
          return [];
        }
        return depts.map((dept: any) => ({
          id: dept.id,
          name: dept.name,
          code: dept.code,
          description: dept.description,
          autoAssignRules: dept.autoAssignRules,
          isActive: dept.isActive,
          createdAt: dept.createdAt,
          updatedAt: dept.updatedAt
        } as Department));
      })
    );
  }

  createDepartment(request: CreateDepartmentRequest): Observable<Department> {
    return this.http.post<any>(`${API_BASE_URL}/api/departments`, request).pipe(
      map(dept => ({
        id: dept.id,
        name: dept.name,
        code: dept.code,
        description: dept.description,
        autoAssignRules: dept.autoAssignRules,
        isActive: dept.isActive,
        createdAt: dept.createdAt,
        updatedAt: dept.updatedAt
      } as Department))
    );
  }

  getDepartment(id: string): Observable<Department> {
    return this.http.get<any>(`${API_BASE_URL}/api/departments/${id}`).pipe(
      map(dept => ({
        id: dept.id,
        name: dept.name,
        code: dept.code,
        description: dept.description,
        autoAssignRules: dept.autoAssignRules,
        isActive: dept.isActive,
        createdAt: dept.createdAt,
        updatedAt: dept.updatedAt
      } as Department))
    );
  }

  updateDepartment(id: string, request: UpdateDepartmentRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/api/departments/${id}`, request);
  }

  deleteDepartment(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/departments/${id}`);
  }

  suggestCode(name: string): Observable<string> {
    return this.http
      .get<{ code: string }>(`${API_BASE_URL}/api/departments/suggest-code`, {
        params: { name },
      })
      .pipe(map((response) => response.code));
  }

  checkCodeAvailability(code: string, excludeDepartmentId?: string): Observable<boolean> {
    const params: Record<string, string> = { code };
    if (excludeDepartmentId) {
      params['excludeDepartmentId'] = excludeDepartmentId;
    }

    return this.http
      .get<{ code: string; available: boolean }>(`${API_BASE_URL}/api/departments/code-availability`, {
        params,
      })
      .pipe(map((response) => response.available));
  }
}

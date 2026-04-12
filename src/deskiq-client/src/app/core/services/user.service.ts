import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { User } from '../models/ticket.models';

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
              role: user.role,
              isActive: user.isActive,
            }) as User,
        );
      }),
    );
  }
}

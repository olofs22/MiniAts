import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateOrganizationRequest,
  CreateUserRequest,
  Organization,
  UpdateOrganizationRequest,
  UserResponse,
} from '../../shared/models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  listOrganizations(): Promise<Organization[]> {
    return firstValueFrom(
      this.http.get<Organization[]>(`${environment.apiUrl}/api/admin/organizations`),
    );
  }

  getOrganization(id: string): Promise<Organization> {
    return firstValueFrom(
      this.http.get<Organization>(`${environment.apiUrl}/api/admin/organizations/${id}`),
    );
  }

  createOrganization(request: CreateOrganizationRequest): Promise<Organization> {
    return firstValueFrom(
      this.http.post<Organization>(`${environment.apiUrl}/api/admin/organizations`, request),
    );
  }

  updateOrganization(id: string, request: UpdateOrganizationRequest): Promise<Organization> {
    return firstValueFrom(
      this.http.put<Organization>(`${environment.apiUrl}/api/admin/organizations/${id}`, request),
    );
  }

  createUser(request: CreateUserRequest): Promise<UserResponse> {
    return firstValueFrom(
      this.http.post<UserResponse>(`${environment.apiUrl}/api/admin/users`, request),
    );
  }
}

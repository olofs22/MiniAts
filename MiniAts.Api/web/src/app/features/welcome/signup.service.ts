import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateSignupRequest } from '../../shared/models/signup-request.model';

@Injectable({ providedIn: 'root' })
export class SignupService {
  private readonly http = inject(HttpClient);

  submit(request: CreateSignupRequest): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${environment.apiUrl}/api/signup-requests`, request),
    );
  }
}

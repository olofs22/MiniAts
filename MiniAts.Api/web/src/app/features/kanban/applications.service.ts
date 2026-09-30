import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Application,
  CreateApplicationRequest,
  UpdateApplicationRequest,
} from '../../shared/models/application.model';

@Injectable({ providedIn: 'root' })
export class ApplicationsService {
  private readonly http = inject(HttpClient);

  listByJob(jobId: string): Promise<Application[]> {
    return firstValueFrom(
      this.http.get<Application[]>(`${environment.apiUrl}/api/applications`, {
        params: { jobId },
      }),
    );
  }

  listByCandidate(candidateId: string): Promise<Application[]> {
    return firstValueFrom(
      this.http.get<Application[]>(`${environment.apiUrl}/api/applications`, {
        params: { candidateId },
      }),
    );
  }

  create(request: CreateApplicationRequest): Promise<Application> {
    return firstValueFrom(
      this.http.post<Application>(`${environment.apiUrl}/api/applications`, request),
    );
  }

  updateStageAndPosition(id: string, request: UpdateApplicationRequest): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${environment.apiUrl}/api/applications/${id}`, request),
    );
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${environment.apiUrl}/api/applications/${id}`));
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateJobRequest, Job, UpdateJobRequest } from '../../shared/models/job.model';

@Injectable({ providedIn: 'root' })
export class JobsService {
  private readonly http = inject(HttpClient);

  list(): Promise<Job[]> {
    return firstValueFrom(this.http.get<Job[]>(`${environment.apiUrl}/api/jobs`));
  }

  get(id: string): Promise<Job> {
    return firstValueFrom(this.http.get<Job>(`${environment.apiUrl}/api/jobs/${id}`));
  }

  create(request: CreateJobRequest): Promise<Job> {
    return firstValueFrom(this.http.post<Job>(`${environment.apiUrl}/api/jobs`, request));
  }

  update(id: string, request: UpdateJobRequest): Promise<void> {
    return firstValueFrom(this.http.put<void>(`${environment.apiUrl}/api/jobs/${id}`, request));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${environment.apiUrl}/api/jobs/${id}`));
  }
}

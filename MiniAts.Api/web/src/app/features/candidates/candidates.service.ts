import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Candidate,
  CreateCandidateRequest,
  UpdateCandidateRequest,
} from '../../shared/models/candidate.model';

@Injectable({ providedIn: 'root' })
export class CandidatesService {
  private readonly http = inject(HttpClient);

  list(): Promise<Candidate[]> {
    return firstValueFrom(this.http.get<Candidate[]>(`${environment.apiUrl}/api/candidates`));
  }

  get(id: string): Promise<Candidate> {
    return firstValueFrom(this.http.get<Candidate>(`${environment.apiUrl}/api/candidates/${id}`));
  }

  create(request: CreateCandidateRequest): Promise<Candidate> {
    return firstValueFrom(
      this.http.post<Candidate>(`${environment.apiUrl}/api/candidates`, request),
    );
  }

  update(id: string, request: UpdateCandidateRequest): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${environment.apiUrl}/api/candidates/${id}`, request),
    );
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${environment.apiUrl}/api/candidates/${id}`));
  }
}

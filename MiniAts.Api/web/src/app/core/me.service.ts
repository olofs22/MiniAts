import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Me {
  userId: string;
  email: string;
  role: 'Admin' | 'Customer';
  orgId: string | null;
  orgName: string | null;
}

@Injectable({ providedIn: 'root' })
export class MeService {
  private readonly http = inject(HttpClient);

  readonly me = signal<Me | null>(null);

  async load(): Promise<Me> {
    const me = await firstValueFrom(this.http.get<Me>(`${environment.apiUrl}/api/me`));
    this.me.set(me);
    return me;
  }

  /** Clears the cached profile, e.g. on sign-out, so the next sign-in fetches fresh data. */
  clear(): void {
    this.me.set(null);
  }
}

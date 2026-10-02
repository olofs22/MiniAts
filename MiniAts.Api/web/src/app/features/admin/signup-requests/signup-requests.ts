import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { SignupRequest } from '../../../shared/models/signup-request.model';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-signup-requests',
  standalone: true,
  imports: [RouterLink, MatButtonModule, DatePipe],
  templateUrl: './signup-requests.html',
})
export class SignupRequests {
  private readonly adminService = inject(AdminService);

  readonly requests = signal<SignupRequest[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly confirmingRemoveId = signal<string | null>(null);

  constructor() {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.requests.set(await this.adminService.listSignupRequests());
    } catch {
      this.error.set('Could not load requests.');
    } finally {
      this.loading.set(false);
    }
  }

  replyLink(request: SignupRequest): string {
    const subject = encodeURIComponent(`Your Mini-ATS request for ${request.companyName}`);
    return `mailto:${request.email}?subject=${subject}`;
  }

  confirmRemove(id: string): void {
    this.confirmingRemoveId.set(id);
  }

  cancelRemove(): void {
    this.confirmingRemoveId.set(null);
  }

  async remove(id: string): Promise<void> {
    try {
      await this.adminService.deleteSignupRequest(id);
      this.requests.set(this.requests().filter((r) => r.id !== id));
    } catch {
      this.error.set('Could not remove request.');
    } finally {
      this.confirmingRemoveId.set(null);
    }
  }
}

import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Organization, UserResponse } from '../../../shared/models/admin.model';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-org-users',
  standalone: true,
  imports: [RouterLink, MatButtonModule, DatePipe],
  templateUrl: './org-users.html',
})
export class OrgUsers {
  private readonly adminService = inject(AdminService);

  readonly orgId = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  readonly organization = signal<Organization | null>(null);
  readonly users = signal<UserResponse[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly confirmingDeactivateId = signal<string | null>(null);

  constructor() {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [organization, users] = await Promise.all([
        this.adminService.getOrganization(this.orgId),
        this.adminService.listUsers(this.orgId),
      ]);
      this.organization.set(organization);
      this.users.set(users);
    } catch {
      this.error.set('Could not load users.');
    } finally {
      this.loading.set(false);
    }
  }

  confirmDeactivate(userId: string): void {
    this.confirmingDeactivateId.set(userId);
  }

  cancelDeactivate(): void {
    this.confirmingDeactivateId.set(null);
  }

  async deactivate(userId: string): Promise<void> {
    try {
      await this.adminService.deactivateUser(userId);
      this.users.set(this.users().filter((user) => user.userId !== userId));
    } catch {
      this.error.set('Could not deactivate user.');
    } finally {
      this.confirmingDeactivateId.set(null);
    }
  }
}

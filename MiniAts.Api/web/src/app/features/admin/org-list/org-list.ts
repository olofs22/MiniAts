import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Organization } from '../../../shared/models/admin.model';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-org-list',
  standalone: true,
  imports: [RouterLink, MatButtonModule],
  templateUrl: './org-list.html',
  styleUrl: './org-list.css',
})
export class OrgList {
  private readonly adminService = inject(AdminService);

  readonly organizations = signal<Organization[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly confirmingDeleteId = signal<string | null>(null);

  constructor() {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.organizations.set(await this.adminService.listOrganizations());
    } catch {
      this.error.set('Could not load organizations.');
    } finally {
      this.loading.set(false);
    }
  }

  confirmDelete(id: string): void {
    this.confirmingDeleteId.set(id);
  }

  cancelDelete(): void {
    this.confirmingDeleteId.set(null);
  }

  async deleteOrganization(id: string): Promise<void> {
    try {
      await this.adminService.deleteOrganization(id);
      this.organizations.set(this.organizations().filter((org) => org.id !== id));
    } catch {
      this.error.set('Could not delete organization.');
    } finally {
      this.confirmingDeleteId.set(null);
    }
  }
}

import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-org-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './org-form.html',
  styleUrl: './org-form.css',
})
export class OrgForm {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly orgId = this.route.snapshot.paramMap.get('id');
  readonly isEdit = this.orgId !== null;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
  });

  constructor() {
    const prefillName = this.route.snapshot.queryParamMap.get('name');
    if (!this.orgId && prefillName) {
      this.form.patchValue({ name: prefillName });
    }

    if (this.orgId) {
      this.loading.set(true);
      this.adminService
        .getOrganization(this.orgId)
        .then((org) => this.form.patchValue({ name: org.name }))
        .catch(() => this.error.set('Could not load organization.'))
        .finally(() => this.loading.set(false));
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    try {
      const { name } = this.form.getRawValue();
      if (this.orgId) {
        await this.adminService.updateOrganization(this.orgId, { name });
      } else {
        await this.adminService.createOrganization({ name });
      }
      await this.router.navigateByUrl('/admin');
    } catch {
      this.error.set('Could not save organization.');
    } finally {
      this.saving.set(false);
    }
  }
}

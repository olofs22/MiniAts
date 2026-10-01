import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Organization, ProfileRole } from '../../../shared/models/admin.model';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-user-invite-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './user-invite-form.html',
  styleUrl: './user-invite-form.css',
})
export class UserInviteForm {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly roles: ProfileRole[] = ['Admin', 'Customer'];

  readonly organizations = signal<Organization[]>([]);
  readonly loadingOrgs = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly partialFailure = signal(false);

  readonly form = this.fb.nonNullable.group({
    orgId: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    role: ['Customer' as ProfileRole, Validators.required],
  });

  constructor() {
    this.loadOrganizations();

    this.form.controls.role.valueChanges.subscribe((role) => {
      const orgIdControl = this.form.controls.orgId;
      if (role === 'Admin') {
        orgIdControl.clearValidators();
      } else {
        orgIdControl.setValidators(Validators.required);
      }
      orgIdControl.updateValueAndValidity();
    });
  }

  private async loadOrganizations(): Promise<void> {
    this.loadingOrgs.set(true);
    this.error.set(null);
    try {
      const organizations = await this.adminService.listOrganizations();
      this.organizations.set(organizations);

      const orgId = this.route.snapshot.queryParamMap.get('orgId');
      if (orgId && organizations.some((org) => org.id === orgId)) {
        this.form.patchValue({ orgId });
      }
    } catch {
      this.error.set('Could not load organizations.');
    } finally {
      this.loadingOrgs.set(false);
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    try {
      const { orgId, email, role } = this.form.getRawValue();
      await this.adminService.createUser({ orgId: orgId || null, email, role });
      this.partialFailure.set(false);
      await this.router.navigateByUrl('/admin');
    } catch (err) {
      this.partialFailure.set(false);

      if (err instanceof HttpErrorResponse) {
        const body: unknown = err.error;
        const message = typeof body === 'string' ? body : (body as { message?: string })?.message;

        if (err.status === 500) {
          this.partialFailure.set(true);
          this.error.set(
            message ?? 'The invite may have partially succeeded. Retry to resume it.',
          );
        } else if (message) {
          this.error.set(message);
        } else {
          this.error.set('Could not invite user.');
        }
      } else {
        this.error.set('Could not invite user.');
      }
    } finally {
      this.saving.set(false);
    }
  }
}

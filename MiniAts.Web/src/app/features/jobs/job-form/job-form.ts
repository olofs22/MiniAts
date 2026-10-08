import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { JobStatus } from '../../../shared/models/job.model';
import { JobsService } from '../jobs.service';

@Component({
  selector: 'app-job-form',
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
  templateUrl: './job-form.html',
  styleUrl: './job-form.css',
})
export class JobForm {
  private readonly fb = inject(FormBuilder);
  private readonly jobsService = inject(JobsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly statuses: JobStatus[] = ['Open', 'OnHold', 'Closed'];
  readonly statusLabels: Record<JobStatus, string> = {
    Open: 'Open',
    OnHold: 'On hold',
    Closed: 'Closed',
  };

  readonly jobId = this.route.snapshot.paramMap.get('id');
  readonly isEdit = this.jobId !== null;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    description: [''],
    status: ['Open' as JobStatus, Validators.required],
  });

  constructor() {
    if (this.jobId) {
      this.loading.set(true);
      this.jobsService
        .get(this.jobId)
        .then((job) => {
          this.form.patchValue({
            title: job.title,
            description: job.description ?? '',
            status: job.status,
          });
        })
        .catch(() => this.error.set('Could not load job.'))
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
      const { title, description, status } = this.form.getRawValue();
      if (this.jobId) {
        await this.jobsService.update(this.jobId, { title, description, status });
      } else {
        await this.jobsService.create({ title, description, status });
      }
      await this.router.navigateByUrl('/jobs');
    } catch {
      this.error.set('Could not save job.');
    } finally {
      this.saving.set(false);
    }
  }
}

import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CandidatesService } from '../candidates.service';

@Component({
  selector: 'app-candidate-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './candidate-form.html',
  styleUrl: './candidate-form.css',
})
export class CandidateForm {
  private readonly fb = inject(FormBuilder);
  private readonly candidatesService = inject(CandidatesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly candidateId = this.route.snapshot.paramMap.get('id');
  readonly isEdit = this.candidateId !== null;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', Validators.email],
    phone: [''],
    linkedInUrl: [''],
    notes: [''],
  });

  constructor() {
    if (this.candidateId) {
      this.loading.set(true);
      this.candidatesService
        .get(this.candidateId)
        .then((candidate) => {
          this.form.patchValue({
            name: candidate.name,
            email: candidate.email ?? '',
            phone: candidate.phone ?? '',
            linkedInUrl: candidate.linkedInUrl ?? '',
            notes: candidate.notes ?? '',
          });
        })
        .catch(() => this.error.set('Could not load candidate.'))
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
      const { name, email, phone, linkedInUrl, notes } = this.form.getRawValue();
      const request = { name, email, phone, linkedInUrl, notes };
      if (this.candidateId) {
        await this.candidatesService.update(this.candidateId, request);
      } else {
        await this.candidatesService.create(request);
      }
      await this.router.navigateByUrl('/candidates');
    } catch {
      this.error.set('Could not save candidate.');
    } finally {
      this.saving.set(false);
    }
  }
}

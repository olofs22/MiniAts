import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { CvJobMatch } from '../../../shared/models/candidate.model';
import { ApplicationsService } from '../../kanban/applications.service';
import { CandidatesService } from '../candidates.service';

const MAX_BYTES = 10 * 1024 * 1024;

@Component({
  selector: 'app-candidate-from-cv',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatRadioModule,
  ],
  templateUrl: './candidate-from-cv.html',
})
export class CandidateFromCv {
  private readonly fb = inject(FormBuilder);
  private readonly candidatesService = inject(CandidatesService);
  private readonly applicationsService = inject(ApplicationsService);
  private readonly router = inject(Router);

  readonly fileName = signal<string | null>(null);
  readonly analyzing = signal(false);
  readonly analyzed = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly matches = signal<CvJobMatch[]>([]);
  /** Job to add the candidate to on save; '' means none. */
  readonly selectedJobId = signal('');
  /** Set when the candidate was saved but adding them to the job failed. */
  readonly savedCandidateId = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', Validators.email],
    phone: [''],
    linkedInUrl: [''],
    notes: [''],
  });

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    this.error.set(null);
    if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
      this.error.set('Please choose a PDF file.');
      return;
    }
    if (file.size > MAX_BYTES) {
      this.error.set('The CV must be 10 MB or smaller.');
      return;
    }

    this.fileName.set(file.name);
    this.analyzing.set(true);
    try {
      const analysis = await this.candidatesService.analyzeCv(file);
      this.form.patchValue({
        name: analysis.name ?? '',
        email: analysis.email ?? '',
        phone: analysis.phone ?? '',
        linkedInUrl: analysis.linkedInUrl ?? '',
      });
      this.matches.set(analysis.matches);
      this.selectedJobId.set(analysis.matches[0]?.jobId ?? '');
      this.analyzed.set(true);
    } catch (err) {
      this.error.set(this.describeError(err));
    } finally {
      this.analyzing.set(false);
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      const candidate = await this.candidatesService.create(this.form.getRawValue());
      const jobId = this.selectedJobId();
      if (jobId) {
        try {
          await this.applicationsService.create({ candidateId: candidate.id, jobId });
        } catch {
          // The candidate exists now; block a second save so it can't be duplicated.
          this.savedCandidateId.set(candidate.id);
          this.error.set('Candidate saved, but could not add them to the job. Add them from their page.');
          return;
        }
      }
      await this.router.navigate(['/candidates', candidate.id]);
    } catch {
      this.error.set('Could not save candidate.');
    } finally {
      this.saving.set(false);
    }
  }

  private describeError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 503) return 'CV analysis is not set up on the server yet.';
      if (err.status === 400 && typeof err.error === 'string') return err.error;
      if (err.status === 502 && err.error?.detail) return err.error.detail;
    }
    return 'Could not analyse this CV. You can still fill in the details by hand.';
  }
}

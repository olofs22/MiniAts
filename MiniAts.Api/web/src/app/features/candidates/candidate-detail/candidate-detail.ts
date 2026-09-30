import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Candidate } from '../../../shared/models/candidate.model';
import { Application } from '../../../shared/models/application.model';
import { Job } from '../../../shared/models/job.model';
import { CandidatesService } from '../candidates.service';
import { ApplicationsService } from '../../kanban/applications.service';
import { JobsService } from '../../jobs/jobs.service';
import { JobPicker } from '../../jobs/job-picker/job-picker';

@Component({
  selector: 'app-candidate-detail',
  standalone: true,
  imports: [RouterLink, MatButtonModule, JobPicker],
  templateUrl: './candidate-detail.html',
  styleUrl: './candidate-detail.css',
})
export class CandidateDetail {
  private readonly candidatesService = inject(CandidatesService);
  private readonly applicationsService = inject(ApplicationsService);
  private readonly jobsService = inject(JobsService);
  private readonly route = inject(ActivatedRoute);

  private readonly candidateId = this.route.snapshot.paramMap.get('id')!;

  readonly candidate = signal<Candidate | null>(null);
  readonly applications = signal<Application[]>([]);
  readonly jobsById = signal<Map<string, Job>>(new Map());
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly appliedJobIds = computed(() => this.applications().map((application) => application.jobId));

  readonly rows = computed(() =>
    this.applications().map((application) => ({
      application,
      job: this.jobsById().get(application.jobId) ?? null,
    })),
  );

  constructor() {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [candidate, applications, jobs] = await Promise.all([
        this.candidatesService.get(this.candidateId),
        this.applicationsService.listByCandidate(this.candidateId),
        this.jobsService.list(),
      ]);
      this.candidate.set(candidate);
      this.applications.set(applications);
      this.jobsById.set(new Map(jobs.map((job) => [job.id, job])));
    } catch {
      this.error.set('Could not load candidate.');
    } finally {
      this.loading.set(false);
    }
  }

  async addToJob(jobId: string): Promise<void> {
    try {
      const application = await this.applicationsService.create({
        candidateId: this.candidateId,
        jobId,
      });
      this.applications.set([...this.applications(), application]);
    } catch {
      this.error.set('Could not add candidate to job.');
    }
  }
}

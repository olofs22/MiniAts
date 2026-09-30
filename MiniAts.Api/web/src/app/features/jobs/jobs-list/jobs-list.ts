import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Job } from '../../../shared/models/job.model';
import { JobsService } from '../jobs.service';

@Component({
  selector: 'app-jobs-list',
  standalone: true,
  imports: [RouterLink, MatButtonModule],
  templateUrl: './jobs-list.html',
  styleUrl: './jobs-list.css',
})
export class JobsList {
  private readonly jobsService = inject(JobsService);

  readonly jobs = signal<Job[]>([]);
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
      this.jobs.set(await this.jobsService.list());
    } catch {
      this.error.set('Could not load jobs.');
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

  async deleteJob(id: string): Promise<void> {
    try {
      await this.jobsService.delete(id);
      this.jobs.set(this.jobs().filter((job) => job.id !== id));
    } catch {
      this.error.set('Could not delete job.');
    } finally {
      this.confirmingDeleteId.set(null);
    }
  }
}

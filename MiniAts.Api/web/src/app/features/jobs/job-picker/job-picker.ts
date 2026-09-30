import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Job } from '../../../shared/models/job.model';
import { JobsService } from '../jobs.service';

@Component({
  selector: 'app-job-picker',
  standalone: true,
  imports: [FormsModule, MatAutocompleteModule, MatFormFieldModule, MatInputModule],
  templateUrl: './job-picker.html',
  styleUrl: './job-picker.css',
})
export class JobPicker {
  private readonly jobsService = inject(JobsService);

  readonly excludeJobIds = input<string[]>([]);
  readonly jobSelected = output<string>();

  readonly jobs = signal<Job[]>([]);
  readonly searchTerm = signal('');

  readonly options = computed(() => {
    const excluded = new Set(this.excludeJobIds());
    const term = this.searchTerm().trim().toLowerCase();
    return this.jobs()
      .filter((job) => !excluded.has(job.id))
      .filter((job) => !term || job.title.toLowerCase().includes(term));
  });

  constructor() {
    this.jobsService.list().then((jobs) => this.jobs.set(jobs));
  }

  select(job: Job): void {
    this.jobSelected.emit(job.id);
    this.searchTerm.set('');
  }
}

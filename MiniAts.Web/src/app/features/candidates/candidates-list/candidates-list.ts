import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Candidate } from '../../../shared/models/candidate.model';
import { CandidatesService } from '../candidates.service';

@Component({
  selector: 'app-candidates-list',
  standalone: true,
  imports: [RouterLink, FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './candidates-list.html',
  styleUrl: './candidates-list.css',
})
export class CandidatesList {
  private readonly candidatesService = inject(CandidatesService);

  readonly candidates = signal<Candidate[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly confirmingDeleteId = signal<string | null>(null);
  readonly searchTerm = signal('');

  readonly filteredCandidates = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      return this.candidates();
    }
    return this.candidates().filter(
      (candidate) =>
        candidate.name.toLowerCase().includes(term) ||
        (candidate.email ?? '').toLowerCase().includes(term),
    );
  });

  constructor() {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.candidates.set(await this.candidatesService.list());
    } catch {
      this.error.set('Could not load candidates.');
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

  async deleteCandidate(id: string): Promise<void> {
    try {
      await this.candidatesService.delete(id);
      this.candidates.set(this.candidates().filter((candidate) => candidate.id !== id));
    } catch {
      this.error.set('Could not delete candidate.');
    } finally {
      this.confirmingDeleteId.set(null);
    }
  }
}

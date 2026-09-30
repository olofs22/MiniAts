import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Candidate } from '../../../shared/models/candidate.model';
import { CandidatesService } from '../../candidates/candidates.service';

@Component({
  selector: 'app-candidate-picker',
  standalone: true,
  imports: [FormsModule, MatAutocompleteModule, MatFormFieldModule, MatInputModule],
  templateUrl: './candidate-picker.html',
  styleUrl: './candidate-picker.css',
})
export class CandidatePicker {
  private readonly candidatesService = inject(CandidatesService);

  readonly excludeCandidateIds = input<string[]>([]);
  readonly candidateSelected = output<string>();

  readonly candidates = signal<Candidate[]>([]);
  readonly searchTerm = signal('');

  readonly options = computed(() => {
    const excluded = new Set(this.excludeCandidateIds());
    const term = this.searchTerm().trim().toLowerCase();
    return this.candidates()
      .filter((candidate) => !excluded.has(candidate.id))
      .filter((candidate) => !term || candidate.name.toLowerCase().includes(term));
  });

  constructor() {
    this.candidatesService.list().then((candidates) => this.candidates.set(candidates));
  }

  select(candidate: Candidate): void {
    this.candidateSelected.emit(candidate.id);
    this.searchTerm.set('');
  }
}

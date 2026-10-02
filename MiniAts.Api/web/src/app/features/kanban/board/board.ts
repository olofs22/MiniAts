import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import {
  APPLICATION_STAGES,
  Application,
  ApplicationStage,
} from '../../../shared/models/application.model';
import { Candidate } from '../../../shared/models/candidate.model';
import { Job } from '../../../shared/models/job.model';
import { ApplicationsService } from '../applications.service';
import { CandidatesService } from '../../candidates/candidates.service';
import { JobsService } from '../../jobs/jobs.service';
import { CandidatePicker } from '../candidate-picker/candidate-picker';

// No rebalancing: halving a gap takes ~50 drops into the exact same slot before doubles
// collide, which a single job's column won't hit at MVP scale.
export function computeDropPosition(
  before: Application | null,
  after: Application | null,
): number {
  if (!before && !after) return 0;
  if (!before) return after!.position - 1;
  if (!after) return before.position + 1;
  return (before.position + after.position) / 2;
}

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [RouterLink, DragDropModule, MatButtonModule, CandidatePicker],
  templateUrl: './board.html',
  styleUrl: './board.css',
})
export class Board {
  private readonly applicationsService = inject(ApplicationsService);
  private readonly candidatesService = inject(CandidatesService);
  private readonly jobsService = inject(JobsService);
  private readonly route = inject(ActivatedRoute);

  private readonly jobId = this.route.snapshot.paramMap.get('jobId')!;

  readonly stages = APPLICATION_STAGES;

  readonly job = signal<Job | null>(null);
  readonly applications = signal<Application[]>([]);
  readonly candidatesById = signal<Map<string, Candidate>>(new Map());
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly confirmingRemoveId = signal<string | null>(null);

  readonly columns = computed(() => {
    const byStage = new Map<ApplicationStage, Application[]>();
    for (const stage of this.stages) {
      byStage.set(stage, []);
    }
    for (const application of this.applications()) {
      byStage.get(application.stage)!.push(application);
    }
    for (const list of byStage.values()) {
      list.sort((a, b) => a.position - b.position);
    }
    return byStage;
  });

  readonly candidateIdsOnBoard = computed(() =>
    this.applications().map((application) => application.candidateId),
  );

  constructor() {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [job, applications, candidates] = await Promise.all([
        this.jobsService.get(this.jobId),
        this.applicationsService.listByJob(this.jobId),
        this.candidatesService.list(),
      ]);
      this.job.set(job);
      this.applications.set(applications);
      this.candidatesById.set(new Map(candidates.map((candidate) => [candidate.id, candidate])));
    } catch {
      this.error.set('Could not load board.');
    } finally {
      this.loading.set(false);
    }
  }

  candidateName(candidateId: string): string {
    return this.candidatesById().get(candidateId)?.name ?? 'Unknown candidate';
  }

  candidate(candidateId: string): Candidate | undefined {
    return this.candidatesById().get(candidateId);
  }

  async onDrop(event: CdkDragDrop<Application[]>, targetStage: ApplicationStage): Promise<void> {
    const application = event.item.data as Application;
    const targetList = event.container.data ?? [];
    const siblings = targetList.filter((item) => item.id !== application.id);
    const before = siblings[event.currentIndex - 1] ?? null;
    const after = siblings[event.currentIndex] ?? null;
    const newPosition = computeDropPosition(before, after);

    if (application.stage === targetStage && application.position === newPosition) {
      return;
    }

    const previous = this.applications();
    this.applications.set(
      previous.map((item) =>
        item.id === application.id ? { ...item, stage: targetStage, position: newPosition } : item,
      ),
    );

    try {
      await this.applicationsService.updateStageAndPosition(application.id, {
        stage: targetStage,
        position: newPosition,
      });
    } catch {
      this.applications.set(previous);
      this.error.set('Could not move candidate. Please try again.');
    }
  }

  async addCandidate(candidateId: string): Promise<void> {
    try {
      const application = await this.applicationsService.create({ candidateId, jobId: this.jobId });
      this.applications.set([...this.applications(), application]);
    } catch {
      this.error.set('Could not add candidate to board.');
    }
  }

  confirmRemove(id: string): void {
    this.confirmingRemoveId.set(id);
  }

  cancelRemove(): void {
    this.confirmingRemoveId.set(null);
  }

  async removeApplication(id: string): Promise<void> {
    try {
      await this.applicationsService.delete(id);
      this.applications.set(this.applications().filter((application) => application.id !== id));
    } catch {
      this.error.set('Could not remove candidate from board.');
    } finally {
      this.confirmingRemoveId.set(null);
    }
  }
}

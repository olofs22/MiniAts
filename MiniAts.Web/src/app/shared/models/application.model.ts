export const APPLICATION_STAGES = [
  'New',
  'Screening',
  'Interview',
  'Offer',
  'Hired',
  'Rejected',
] as const;

export type ApplicationStage = (typeof APPLICATION_STAGES)[number];

export interface Application {
  id: string;
  orgId: string;
  candidateId: string;
  jobId: string;
  stage: ApplicationStage;
  position: number;
  createdAt: string;
}

export interface CreateApplicationRequest {
  candidateId: string;
  jobId: string;
}

export interface UpdateApplicationRequest {
  stage: ApplicationStage;
  position: number;
}
